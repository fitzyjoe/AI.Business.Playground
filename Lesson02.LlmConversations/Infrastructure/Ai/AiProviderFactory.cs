using Lesson02.LlmConversations.Infrastructure.Ai.Providers;

namespace Lesson02.LlmConversations.Infrastructure.Ai;

public sealed class AiProviderFactory(OllamaProvider ollamaProvider) : IAiProviderFactory
{
    public IAiProvider GetProvider(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "ollama" => ollamaProvider,
            _ => throw new NotSupportedException($"AI Provider '{providerName}' is not supported.")
        };
    }
}
