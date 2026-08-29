using LLMChat.Api.Data;
using LLMChat.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

const string sqliteConnectionString = "Data Source=vectors.db";
var sqliteDataSource = new SqliteConnectionStringBuilder(sqliteConnectionString).DataSource;
var absoluteSqliteDatabasePath = Path.GetFullPath(
    sqliteDataSource,
    Directory.GetCurrentDirectory());

Console.WriteLine($"Process working directory: {Directory.GetCurrentDirectory()}");
Console.WriteLine($"Application base directory: {AppContext.BaseDirectory}");
Console.WriteLine($"SQLite connection string: {sqliteConnectionString}");
Console.WriteLine($"Absolute SQLite database path: {absoluteSqliteDatabasePath}");

// Register MVC Controllers
builder.Services.AddControllers();

// Register Swagger generation
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<VectorDbContext>(options =>
    options.UseSqlite(sqliteConnectionString));

builder.Services.AddHttpClient<ILLMService, LLMService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddHttpClient<IEmbeddingService, EmbeddingService>();
builder.Services.AddScoped<IVectorStore, SQLiteVectorStore>();
builder.Services.AddScoped<IDocumentIngestionService, DocumentIngestionService>();
builder.Services.AddSingleton<IDocumentChunker, DocumentChunker>();
builder.Services.AddScoped<IRAGService, RAGService>();
builder.Services.AddScoped<IRelevanceService, RelevanceService>();
builder.Services.AddScoped<IReranker, HybridReranker>();
builder.Services.AddScoped<IRAGEvaluationService, RAGEvaluationService>();
builder.Services.AddScoped<PlainTextDocumentExtractor>();
builder.Services.AddScoped<PdfDocumentTextExtractor>();
builder.Services.AddScoped<DocxDocumentTextExtractor>();
builder.Services.AddScoped<IDocumentTextExtractorResolver, DocumentTextExtractorResolver>();
builder.Services.AddScoped<IDocumentRepository, SQLiteDocumentRepository>();
builder.Services.AddScoped<ISearchKnowledgeBaseTool, SearchKnowledgeBaseTool>();
builder.Services.AddScoped<ISearchKnowledgeBaseTool, SearchKnowledgeBaseTool>();
builder.Services.AddHttpClient<OllamaAgentService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddScoped<ICalculatorTool, CalculatorTool>();
var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<VectorDbContext>();
    var connection = dbContext.Database.GetDbConnection();

    Console.WriteLine("=== SQLITE RUNTIME DIAGNOSTIC ===");
    Console.WriteLine($"Connection string: {connection.ConnectionString}");
    Console.WriteLine($"Data source: {connection.DataSource}");
    Console.WriteLine(
    $"Absolute EF database path: {Path.GetFullPath(connection.DataSource, Directory.GetCurrentDirectory())}");
    Console.WriteLine($"Base directory: {AppContext.BaseDirectory}");
    Console.WriteLine($"Current directory: {Directory.GetCurrentDirectory()}");

    await connection.OpenAsync();

    using var command = connection.CreateCommand();
    command.CommandText =
        "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";

    using var reader = await command.ExecuteReaderAsync();

    while (await reader.ReadAsync())
    {
        Console.WriteLine($"TABLE: {reader.GetString(0)}");
    }

    Console.WriteLine("=================================");
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map Controller endpoints
app.MapControllers();

app.Run();