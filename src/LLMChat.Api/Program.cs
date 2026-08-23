using LLMChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register MVC Controllers
builder.Services.AddControllers();

// Register Swagger generation
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient<ILLMService, LLMService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
});

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