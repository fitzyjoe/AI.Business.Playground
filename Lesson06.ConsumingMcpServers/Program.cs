using Lesson06.ConsumingMcpServers.Features.Conversations;
using Lesson06.ConsumingMcpServers.Infrastructure.Ai;
using Lesson06.ConsumingMcpServers.Infrastructure.Ai.Providers;
using Lesson06.ConsumingMcpServers.Infrastructure.Conversations;
using Lesson06.ConsumingMcpServers.Infrastructure.ErrorHandling;
using Lesson06.ConsumingMcpServers.Infrastructure.Mcp;
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
builder.Services.AddSingleton<PropertyMcpClient>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConversationNotFoundExceptionHandler>();

var app = builder.Build();

await app.Services
	.GetRequiredService<PropertyMcpClient>()
	.InitializeAsync();

app.UseExceptionHandler();
app.MapControllers();

app.Run();