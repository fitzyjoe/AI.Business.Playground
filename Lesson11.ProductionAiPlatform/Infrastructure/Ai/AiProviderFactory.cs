using Microsoft.Extensions.Options;

namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public sealed class AiProviderFactory(IServiceProvider _serviceProvider,
	IOptions<AiOptions> _options) : IAiProviderFactory
{
	public IAiProvider GetProvider(string provider)
	{
		if (!_options.Value.AllowedProviders.Contains(
			    provider,
			    StringComparer.OrdinalIgnoreCase))
		{
			throw new UnsupportedAiProviderException(provider);
		}
		
		return provider.ToLowerInvariant() switch
		{
			"ollama" => _serviceProvider.GetRequiredKeyedService<IAiProvider>("ollama"),
			"openai" => _serviceProvider.GetRequiredKeyedService<IAiProvider>("openai"),
			_ => throw new UnsupportedAiProviderException(provider)
		};
	}
}