using CommunityToolkit.VectorData.InMemory;
using Lesson07.RetrievalAugmentedGeneration.Features.Conversations;
using Lesson07.RetrievalAugmentedGeneration.Features.Knowledge;
using Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai;
using Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai.Providers;
using Lesson07.RetrievalAugmentedGeneration.Infrastructure.ErrorHandling;
using Lesson07.RetrievalAugmentedGeneration.Infrastructure.Mcp;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using OllamaSharp;

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

builder.Services
	.AddOptions<OpenAiOptions>()
	.Bind(builder.Configuration.GetSection("OpenAI"))
	.Validate(
		options => !string.IsNullOrWhiteSpace(options.Model),
		"An OpenAI default model is required.")
	.ValidateOnStart();

builder.Services
	.AddOptions<RagOptions>()
	.Bind(builder.Configuration.GetSection("Rag"))
	.Validate(
		options =>
			string.Equals(options.EmbeddingProvider, "ollama", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(options.EmbeddingProvider, "openai", StringComparison.OrdinalIgnoreCase),
		"EmbeddingProvider must be 'ollama' or 'openai'.")
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

builder.Services.AddControllers();
builder.Services.AddHttpClient<OllamaProvider>(
	(serviceProvider, httpClient) =>
	{
		var options = serviceProvider
			.GetRequiredService<IOptions<OllamaOptions>>()
			.Value;

		httpClient.BaseAddress = new Uri(options.Endpoint);
	});

builder.Services.AddHttpClient(
	"OllamaEmbeddings",
	(serviceProvider, httpClient) =>
	{
		var options = serviceProvider
			.GetRequiredService<IOptions<OllamaOptions>>()
			.Value;

		httpClient.BaseAddress = new Uri(options.Endpoint);
	});

builder.Services.AddTransient<OpenAiProvider>();
builder.Services.AddTransient<IAiProviderFactory, AiProviderFactory>();
builder.Services.AddTransient<MessageHandler>();
builder.Services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
builder.Services.AddSingleton<PropertyMcpClient>();

builder.Services
	.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
		"ollama",
		(serviceProvider, _) =>
		{
			var httpClient = serviceProvider
				.GetRequiredService<IHttpClientFactory>()
				.CreateClient("OllamaEmbeddings");

			var ragOptions = serviceProvider
				.GetRequiredService<IOptions<RagOptions>>()
				.Value;

			IEmbeddingGenerator<string, Embedding<float>> generator = new OllamaApiClient(httpClient);

			return generator
				.AsBuilder()
				.ConfigureOptions(options =>
				{
					options.ModelId = ragOptions.EmbeddingModel;
				})
				.Build();
		});

builder.Services
	.AddKeyedSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
		"openai",
		(serviceProvider, _) =>
		{
			var ragOptions = serviceProvider
				.GetRequiredService<IOptions<RagOptions>>()
				.Value;

			var apiKey = Environment.GetEnvironmentVariable("OPENAI_AI_BUSINESS_PLAYGROUND")
			             ?? throw new InvalidOperationException(
				             "OPENAI_AI_BUSINESS_PLAYGROUND environment variable is required.");

			return new OpenAI.Embeddings.EmbeddingClient(
					ragOptions.EmbeddingModel,
					apiKey)
				.AsIEmbeddingGenerator(ragOptions.EmbeddingDimensions);
		});

builder.Services.AddSingleton<VectorStore>(
	serviceProvider =>
	{
		var ragOptions = serviceProvider
			.GetRequiredService<IOptions<RagOptions>>()
			.Value;

		var embeddingGenerator = serviceProvider
			.GetRequiredKeyedService<IEmbeddingGenerator<string, Embedding<float>>>(
				ragOptions.EmbeddingProvider);

		return new InMemoryVectorStore(
			new InMemoryVectorStoreOptions
			{
				EmbeddingGenerator = embeddingGenerator
			});
	});

builder.Services.AddSingleton<KnowledgeRetriever>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ConversationNotFoundExceptionHandler>();

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
