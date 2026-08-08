using Lesson06.ConsumingMcpServers.Infrastructure.Ai.Providers;

namespace Lesson06.ConsumingMcpServers.Infrastructure.Ai;

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
