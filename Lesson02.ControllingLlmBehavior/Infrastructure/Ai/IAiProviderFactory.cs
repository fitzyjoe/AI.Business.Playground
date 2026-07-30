namespace Lesson02.ControllingLlmBehavior.Infrastructure.Ai;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerName);
}
