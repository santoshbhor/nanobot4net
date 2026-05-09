using Nanobot.Api.Services;
using Nanobot.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() 
    { 
        Title = "Nanobot API", 
        Version = "v1",
        Description = "OpenAI-compatible API for Nanobot AI Agent"
    });
});

// Add API service
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<ApiService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nanobot API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

Console.WriteLine("🐈 Nanobot API Server starting...");
Console.WriteLine("   API Docs: http://localhost:5000/swagger");
Console.WriteLine("   Health:   http://localhost:5000/health");
Console.WriteLine();
Console.WriteLine("Endpoints:");
Console.WriteLine("   POST /v1/chat/completions");
Console.WriteLine("   GET  /v1/models");
Console.WriteLine("   POST /v1/embeddings");

app.Run("http://0.0.0.0:5000");