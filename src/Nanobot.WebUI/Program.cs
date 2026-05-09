using Microsoft.AspNetCore.Http.Connections;
using Nanobot.WebUI.Hubs;
using Nanobot.WebUI.Services;
using Nanobot.Core.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddSignalR();

// Add configuration
builder.Services.AddSingleton<ConfigurationService>();
builder.Services.AddSingleton<AgentService>();

// Configure CORS for development
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors();

app.MapRazorPages();
app.MapControllers();
app.MapHub<ChatHub>("/chathub", options =>
{
    options.Transports = HttpTransportType.WebSockets;
});

Console.WriteLine("🐈 Nanobot WebUI starting...");
Console.WriteLine("   Open http://localhost:5000 in your browser");

app.Run("http://0.0.0.0:5000");