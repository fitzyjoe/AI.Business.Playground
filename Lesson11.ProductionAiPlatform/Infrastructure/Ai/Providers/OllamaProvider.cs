using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OllamaSharp;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai.Providers;

public sealed class OllamaProvider : IAiProvider, IDisposable
{
	private readonly OllamaOptions _options;

	public OllamaProvider(
		IHttpClientFactory httpClientFactory,
		IOptions<OllamaOptions> options)
	{
		_options = options.Value;

		var httpClient = httpClientFactory.CreateClient();
		httpClient.BaseAddress = new Uri(_options.Endpoint);

		ChatClient = new OllamaApiClient(httpClient);
	}

	public string Name => "ollama";
	public string DefaultModel => _options.Model;
	public IChatClient ChatClient { get; }

	public void Dispose()
	{
		ChatClient.Dispose();
	}
}