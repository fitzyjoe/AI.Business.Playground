using Lesson04.StructuredOutputs.Infrastructure.Ai.Providers;

namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

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
