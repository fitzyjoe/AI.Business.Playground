using Lesson01.Chat.Infrastructure.Ai.Providers;

namespace Lesson01.Chat.Infrastructure.Ai;

public sealed class AiProviderFactory(IServiceProvider serviceProvider) : IAiProviderFactory
{
    public IAiProvider GetProvider(string modelName)
    {
        return modelName.ToLowerInvariant() switch
        {
            "ollama" => serviceProvider.GetRequiredService<OllamaProvider>(),
            _ => throw new NotSupportedException($"AI Provider for model '{modelName}' is not supported.")
        };
    }
}
