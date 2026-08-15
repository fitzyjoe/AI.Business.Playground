namespace Lesson09.Agents.Infrastructure.Ai;

public interface IAiProviderFactory
{
	IAiProvider GetProvider(string provider);
}