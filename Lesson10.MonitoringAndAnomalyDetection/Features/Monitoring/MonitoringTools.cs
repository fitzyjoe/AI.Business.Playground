using System.ComponentModel;
using Lesson10.MonitoringAndAnomalyDetection.Infrastructure.Monitoring;

namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed class MonitoringTools(MonitoringDataSource _dataSource)
{
	[Description("Gets recent observations for an operational metric so its current value can be compared with recent history.")]
	public IReadOnlyList<MetricObservation> GetMetricHistory(
		[Description("The metric name.")] string metric,
		[Description("The number of recent observations to return.")] int points = 24)
	{
		return _dataSource.GetMetricHistory(metric, points);
	}

	[Description("Gets recent operational events such as deployments, incidents, and batch jobs that may help explain an anomaly.")]
	public IReadOnlyList<OperationalEvent> GetRecentOperationalEvents(
		[Description("How many hours of operational events to return.")] int hours = 24)
	{
		return _dataSource.GetRecentEvents(hours);
	}
	
	[Description("Gets details about a deployment, including the service deployed and the changes included in that version.")]
	public DeploymentDetails? GetDeploymentDetails(
		[Description("The deployed version, such as '4.8'.")] string version)
	{
		return _dataSource.GetDeploymentDetails(version);
	}
}