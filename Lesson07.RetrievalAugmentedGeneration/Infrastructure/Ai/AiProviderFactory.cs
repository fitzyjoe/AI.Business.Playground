using Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai.Providers;

namespace Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai;

public sealed class AiProviderFactory(OllamaProvider ollamaProvider, OpenAiProvider openAiProvider) : IAiProviderFactory
{
	public IAiProvider GetProvider(string providerName)
	{
		return providerName.ToLowerInvariant() switch
		{
			"ollama" => ollamaProvider,
			"openai" => openAiProvider,
			_ => throw new NotSupportedException($"AI Provider '{providerName}' is not supported.")
		};
	}
}
