using Lesson04.StructuredOutputs.Features.Conversations;
using Lesson04.StructuredOutputs.Infrastructure.Ai;
using Lesson04.StructuredOutputs.Infrastructure.Ai.Providers;
using Lesson04.StructuredOutputs.Infrastructure.Conversations;
using Lesson04.StructuredOutputs.Infrastructure.ErrorHandling;
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

builder.Services.AddControllers();
builder.Services.AddHttpClient<OllamaProvider>(
	(serviceProvider, httpClient) =>
	{
		var options = serviceProvider
			.GetRequiredService<IOptions<OllamaOptions>>()
			.Value;

		httpClient.BaseAddress = new Uri(options.Endpoint);
	});

builder.Services.AddTransient<IAiProviderFactory, AiProviderFactory>();
builder.Services.AddTransient<MessageHandler>();
builder.Services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConversationNotFoundExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();
app.MapControllers();

app.Run();