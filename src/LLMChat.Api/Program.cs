using LLMChat.Api.Data;
using LLMChat.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Register MVC Controllers
builder.Services.AddControllers();

// Register Swagger generation
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<VectorDbContext>(options =>
    options.UseSqlite("Data Source=vectors.db"));

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
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map Controller endpoints
app.MapControllers();

app.Run();