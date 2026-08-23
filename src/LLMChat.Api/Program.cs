using LLMChat.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Register MVC Controllers
builder.Services.AddControllers();

// Register Swagger generation
builder.Services.AddSwaggerGen();

// Register our LLM service
builder.Services.AddScoped<ILLMService, LLMService>();

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