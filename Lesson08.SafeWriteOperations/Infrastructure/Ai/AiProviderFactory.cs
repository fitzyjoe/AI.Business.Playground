using Lesson08.SafeWriteOperations.Infrastructure.Ai.Providers;

namespace Lesson08.SafeWriteOperations.Infrastructure.Ai;

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
