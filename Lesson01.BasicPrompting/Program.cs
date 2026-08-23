using Lesson01.BasicPrompting.Features.Models.Execute;
using Lesson01.BasicPrompting.Infrastructure.Ai;
using Lesson01.BasicPrompting.Infrastructure.Ai.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OllamaOptions>(
	builder.Configuration.GetSection("Ollama"));

builder.Services.AddHttpClient();
builder.Services.AddTransient<Handler>();
builder.Services.AddSingleton<IAiProvider, OllamaProvider>();

var app = builder.Build();

app.MapExecutePrompt();

app.Run();