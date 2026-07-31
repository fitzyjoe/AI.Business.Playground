namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerName);
}
