namespace Lesson06.ConsumingMcpServers.Infrastructure.Ai;

public interface IAiProviderFactory
{
    IAiProvider GetProvider(string providerName);
}
