var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithToolsFromAssembly()
    .WithHttpTransport();

builder.Services.AddCors();

var app = builder.Build();

app.MapMcp();

app.UseCors(policy => policy
    .WithOrigins("https://localhost:4680") 
    .AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials());

app.Run();