namespace Lesson10.MonitoringAndAnomalyDetection.Infrastructure.Ai;

public interface IAiProviderFactory
{
	IAiProvider GetProvider(string provider);
}