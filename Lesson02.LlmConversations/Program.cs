using Lesson01.Chat.Features.Models.Execute;
using Lesson01.Chat.Infrastructure.Ai;
using Lesson01.Chat.Infrastructure.Ai.Providers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<OllamaOptions>(
	builder.Configuration.GetSection("Ollama"));

builder.Services.AddHttpClient();
builder.Services.AddTransient<Handler>();
builder.Services.AddSingleton<OllamaProvider>();
builder.Services.AddSingleton<IAiProviderFactory, AiProviderFactory>();

var app = builder.Build();

app.MapExecutePrompt();

app.Run();