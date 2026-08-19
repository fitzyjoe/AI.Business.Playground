using System.Text.Json.Serialization;
using CommunityToolkit.VectorData.InMemory;
using Lesson11.ProductionAiPlatform.Features.Agents;
using Lesson11.ProductionAiPlatform.Features.Conversations;
using Lesson11.ProductionAiPlatform.Features.Knowledge;
using Lesson11.ProductionAiPlatform.Features.Monitoring;
using Lesson11.ProductionAiPlatform.Features.PropertyReviews;
using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Lesson11.ProductionAiPlatform.Infrastructure.Ai.Providers;
using Lesson11.ProductionAiPlatform.Infrastructure.ErrorHandling;
using Lesson11.ProductionAiPlatform.Infrastructure.Mcp;
using Lesson11.ProductionAiPlatform.Infrastructure.Rag;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using OllamaSharp;

var builder = WebApplication.CreateBuilder(args);

// Ollama Options
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

// OpenAI
builder.Services
	.AddOptions<OpenAiOptions>()
	.Bind(builder.Configuration.GetSection("OpenAI"))
	.Validate(
		options => !string.IsNullOrWhiteSpace(options.Model),
		"An OpenAI default model is required.")
	.ValidateOnStart();

builder.Services
	.AddOptions<MonitoringOptions>()
	.Bind(builder.Configuration.GetSection("Monitoring"))
	.Validate(
		options => !string.IsNullOrWhiteSpace(options.Provider),
		"A monitoring AI provider is required.")
	.ValidateOnStart();

// RAG Options
builder.Services
	.AddOptions<RagOptions>()
	.Bind(builder.Configuration.GetSection("Rag"))
	.Validate(
		options => !string.IsNullOrWhiteSpace(options.EmbeddingModel),
		"EmbeddingModel is required.")
	.Validate(
		options => options.EmbeddingDimensions > 0,
		"EmbeddingDimensions must be greater than zero.")
	.Validate(
		options => options.TopResults > 0,
		"TopResults must be greater than zero.")
	.ValidateOnStart();

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
	});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiProvider, OllamaProvider>();
builder.Services.AddSingleton<IAiProvider, OpenAiProvider>();
builder.Services.AddSingleton<IAiProviderFactory, AiProviderFactory>();
builder.Services.AddTransient<MessageHandler>();
builder.Services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
builder.Services.AddSingleton<PropertyMcpClient>();

// RAG in Ollama
builder.Services.AddSingleton<VectorStore>(
	serviceProvider =>
	{
		var httpClient = serviceProvider
			.GetRequiredService<IHttpClientFactory>()
			.CreateClient();

		var ollamaOptions = serviceProvider
			.GetRequiredService<IOptions<OllamaOptions>>()
			.Value;

		var ragOptions = serviceProvider
			.GetRequiredService<IOptions<RagOptions>>()
			.Value;

		httpClient.BaseAddress = new Uri(ollamaOptions.Endpoint);
		
		IEmbeddingGenerator<string, Embedding<float>> generator = new OllamaApiClient(httpClient);
		
		var embeddingGenerator = generator
			.AsBuilder()
			.ConfigureOptions(options =>
			{
				options.ModelId = ragOptions.EmbeddingModel;
			})
			.Build();

		return new InMemoryVectorStore(
			new InMemoryVectorStoreOptions
			{
				EmbeddingGenerator = embeddingGenerator
			});
	});

builder.Services.AddSingleton<KnowledgeRetriever>();
builder.Services.AddSingleton<KnowledgeTools>();
builder.Services.AddSingleton<IPendingPropertyReviewRepository, InMemoryPendingPropertyReviewRepository>();
builder.Services.AddSingleton<IPropertyReviewRepository, InMemoryPropertyReviewRepository>();
builder.Services.AddSingleton<PropertyReviewService>();
builder.Services.AddSingleton<PropertyReviewTools>();
builder.Services.AddSingleton<PropertyReviewAgent>();
builder.Services.AddSingleton<MonitoringDataSource>();
builder.Services.AddSingleton<RollingZScoreDetector>();
builder.Services.AddSingleton<MonitoringTools>();
builder.Services.AddSingleton<AnomalyAnalysisAgent>();
builder.Services.AddSingleton<MonitoringService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConversationNotFoundExceptionHandler>();
builder.Services.AddExceptionHandler<UnsupportedAiProviderExceptionHandler>();

var app = builder.Build();

await app.Services
	.GetRequiredService<PropertyMcpClient>()
	.InitializeAsync();

await app.Services
	.GetRequiredService<KnowledgeRetriever>()
	.InitializeAsync();

app.UseExceptionHandler();
app.MapControllers();

app.Run();