namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public interface IAiProviderFactory
{
	IAiProvider GetProvider(string provider);
}