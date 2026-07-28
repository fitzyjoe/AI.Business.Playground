namespace Lesson02.LlmConversations.Infrastructure.Ai;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerName);
}
