namespace Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerName);
}
