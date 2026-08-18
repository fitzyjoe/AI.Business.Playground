using Microsoft.Extensions.AI;

namespace Lesson10.MonitoringAndAnomalyDetection.Infrastructure.Ai;

public interface IAiProvider
{
	string Name { get; }
	string DefaultModel { get; }
	IChatClient ChatClient { get; }
}