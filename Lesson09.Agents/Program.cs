using System.Text.Json.Serialization;
using CommunityToolkit.VectorData.InMemory;
using Lesson09.Agents.Features.Agents;
using Lesson09.Agents.Features.Conversations;
using Lesson09.Agents.Features.Knowledge;
using Lesson09.Agents.Features.PropertyReviews;
using Lesson09.Agents.Infrastructure.Ai.Providers;
using Lesson09.Agents.Infrastructure.Conversations;
using Lesson09.Agents.Infrastructure.ErrorHandling;
using Lesson09.Agents.Infrastructure.Mcp;
using Lesson09.Agents.Infrastructure.PropertyReviews;
using Lesson09.Agents.Infrastructure.Rag;
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

builder.Services.AddHttpClient(
	"OllamaAgent",
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

builder.Services.AddTransient<MessageHandler>();
builder.Services.AddSingleton<IConversationRepository, InMemoryConversationRepository>();
builder.Services.AddSingleton<PropertyMcpClient>();

builder.Services.AddSingleton<IEmbeddingGenerator<string, Embedding<float>>>(
	serviceProvider =>
	{
		var httpClient = serviceProvider
			.GetRequiredService<IHttpClientFactory>()
			.CreateClient("OllamaEmbeddings");

		var ragOptions = serviceProvider
			.GetRequiredService<IOptions<RagOptions>>()
			.Value;

		IEmbeddingGenerator<string, Embedding<float>> generator =
			new OllamaApiClient(httpClient);

		return generator
			.AsBuilder()
			.ConfigureOptions(options =>
			{
				options.ModelId = ragOptions.EmbeddingModel;
			})
			.Build();
	});

builder.Services.AddSingleton<VectorStore>(
	serviceProvider =>
	{
		var embeddingGenerator = serviceProvider.GetRequiredService<
			IEmbeddingGenerator<string, Embedding<float>>>();

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