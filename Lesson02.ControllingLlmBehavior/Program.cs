using Lesson02.ControllingLlmBehavior.Features.Models.Execute;
using Lesson02.ControllingLlmBehavior.Infrastructure.Ai;
using Lesson02.ControllingLlmBehavior.Infrastructure.Ai.Providers;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddOptions<OllamaOptions>()
	.Bind(builder.Configuration.GetSection("Ollama"))
	.Validate(
		options => Uri.TryCreate(
			options.Endpoint,
			UriKind.Absolute,
			out _),
		"Ollama endpoint must be an absolute URI.")
	.Validate(
		options => !string.IsNullOrWhiteSpace(options.Model),
		"An Ollama default model is required.")
	.ValidateOnStart();

builder.Services.AddHttpClient<OllamaProvider>(
	(serviceProvider, httpClient) =>
	{
		var options = serviceProvider
			.GetRequiredService<IOptions<OllamaOptions>>()
			.Value;

		httpClient.BaseAddress = new Uri(options.Endpoint);
	});

builder.Services.AddTransient<IAiProviderFactory, AiProviderFactory>();
builder.Services.AddTransient<Handler>();

var app = builder.Build();

app.MapExecutePrompt();

app.Run();