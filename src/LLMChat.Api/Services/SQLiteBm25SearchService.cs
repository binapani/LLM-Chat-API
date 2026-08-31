using System.Data;
using System.Diagnostics;
using System.Text.RegularExpressions;
using LLMChat.Api.Data;
using LLMChat.Api.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LLMChat.Api.Services;

public class SQLiteBm25SearchService : IBm25SearchService
{
    private const string FtsTableName = "DocumentChunksFts";
    private readonly VectorDbContext _dbContext;
    private readonly ILogger<SQLiteBm25SearchService> _logger;

    public SQLiteBm25SearchService(
        VectorDbContext dbContext,
        ILogger<SQLiteBm25SearchService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var connection = await GetOpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            CREATE VIRTUAL TABLE IF NOT EXISTS {FtsTableName}
            USING fts5(
                ChunkId UNINDEXED,
                DocumentId UNINDEXED,
                Source UNINDEXED,
                Content
            );";

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task BackfillAsync(CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);

        var connection = await GetOpenConnectionAsync(cancellationToken);

        await using (var clearCommand = connection.CreateCommand())
        {
            clearCommand.CommandText = $"DELETE FROM {FtsTableName};";
            await clearCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insertCommand = connection.CreateCommand())
        {
            insertCommand.CommandText = $@"
                INSERT INTO {FtsTableName}(ChunkId, DocumentId, Source, Content)
                SELECT ChunkId, DocumentId, Source, Content
                FROM DocumentVectors
                ORDER BY DocumentId, ChunkId;";

            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        _logger.LogInformation(
            "BM25 FTS5 index backfilled from existing DocumentVectors rows.");
    }

    public async Task IndexChunkAsync(
        DocumentVector document,
        CancellationToken cancellationToken = default)
    {
        if (document is null)
        {
            throw new ArgumentNullException(nameof(document));
        }

        var connection = await GetOpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            INSERT INTO {FtsTableName}(ChunkId, DocumentId, Source, Content)
            VALUES (@ChunkId, @DocumentId, @Source, @Content);";

        command.Parameters.Add(new SqliteParameter("@ChunkId", document.ChunkId));
        command.Parameters.Add(new SqliteParameter("@DocumentId", document.DocumentId));
        command.Parameters.Add(new SqliteParameter("@Source", document.Source));
        command.Parameters.Add(new SqliteParameter("@Content", document.Content));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReindexDocumentAsync(
        string documentId,
        IReadOnlyList<DocumentVector> documents,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException("Document ID is required.", nameof(documentId));
        }

        if (documents == null)
        {
            throw new ArgumentNullException(nameof(documents));
        }

        await InitializeAsync(cancellationToken);

        var connection = await GetOpenConnectionAsync(cancellationToken);

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.CommandText = $"DELETE FROM {FtsTableName} WHERE DocumentId = @DocumentId;";
            deleteCommand.Parameters.Add(new SqliteParameter("@DocumentId", documentId));
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        if (documents.Count == 0)
        {
            return;
        }

        await using var insertCommand = connection.CreateCommand();
        insertCommand.CommandText = $@"
            INSERT INTO {FtsTableName}(ChunkId, DocumentId, Source, Content)
            VALUES (@ChunkId, @DocumentId, @Source, @Content);";

        foreach (var document in documents)
        {
            insertCommand.Parameters.Clear();
            insertCommand.Parameters.Add(new SqliteParameter("@ChunkId", document.ChunkId));
            insertCommand.Parameters.Add(new SqliteParameter("@DocumentId", document.DocumentId));
            insertCommand.Parameters.Add(new SqliteParameter("@Source", document.Source));
            insertCommand.Parameters.Add(new SqliteParameter("@Content", document.Content));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<Bm25SearchResult>> SearchAsync(
        string query,
        int topK,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<Bm25SearchResult>();
        }

        if (topK <= 0)
        {
            return Array.Empty<Bm25SearchResult>();
        }

        await InitializeAsync(cancellationToken);

        var normalizedQuery = NormalizeFtsQuery(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return Array.Empty<Bm25SearchResult>();
        }

        var stopwatch = Stopwatch.StartNew();

        var connection = await GetOpenConnectionAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $@"
            SELECT
                DocumentId,
                ChunkId,
                Source,
                Content,
                bm25({FtsTableName}) AS Score
            FROM {FtsTableName}
            WHERE {FtsTableName} MATCH @Query
            ORDER BY bm25({FtsTableName}) DESC
            LIMIT @TopK;";

        command.Parameters.Add(new SqliteParameter("@Query", normalizedQuery));
        command.Parameters.Add(new SqliteParameter("@TopK", topK));

        var results = new List<Bm25SearchResult>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new Bm25SearchResult
            {
                DocumentId = reader.GetString(0),
                ChunkId = reader.GetInt32(1),
                Source = reader.GetString(2),
                Content = reader.GetString(3),
                Score = reader.GetDouble(4)
            });
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "BM25 query={Query} topK={TopK} resultCount={ResultCount} durationMs={DurationMs}",
            query,
            topK,
            results.Count,
            stopwatch.ElapsedMilliseconds);

        return results;
    }

    private async Task<SqliteConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = (SqliteConnection)_dbContext.Database.GetDbConnection();

        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }

    private static string NormalizeFtsQuery(string query)
    {
        var normalized = Regex.Replace(query.Trim(), @"\s+", " ");

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        normalized = normalized.Replace('-', ' ');
        normalized = Regex.Replace(normalized, @"\s+", " ");

        if (normalized.Contains('"'))
        {
            return normalized;
        }

        var tokens = normalized
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(token => !string.IsNullOrWhiteSpace(token));

        return string.Join(" ", tokens);
    }
}
