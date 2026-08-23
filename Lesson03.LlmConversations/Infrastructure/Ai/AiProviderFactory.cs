using Lesson03.LlmConversations.Infrastructure.Ai.Providers;

namespace Lesson03.LlmConversations.Infrastructure.Ai;

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
