using System.ComponentModel;

namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed class MonitoringTools(MonitoringDataSource _dataSource)
{
	[Description("Gets recent observations for an operational metric so its current value can be compared with recent history.")]
	public IReadOnlyList<MetricObservation> GetMetricHistory(
		[Description("The metric name.")] string metric,
		[Description("The number of recent observations to return.")] int points = 24)
	{
		points = Math.Clamp(points, 1, 168);
		Console.WriteLine($"*** GET METRIC HISTORY CALLED: {metric} {points} ***");
		return _dataSource.GetMetricHistory(metric, points);
	}

	[Description("Gets recent operational events such as deployments, incidents, and batch jobs that may help explain an anomaly.")]
	public IReadOnlyList<OperationalEvent> GetRecentOperationalEvents(
		[Description("How many hours of operational events to return.")] int hours = 24)
	{
		hours = Math.Clamp(hours, 1, 168);
		Console.WriteLine($"*** GET RECENT EVENTS CALLED: {hours} ***");
		return _dataSource.GetRecentEvents(hours);
	}
	
	[Description(
		"Gets the changes included in a deployment. Use this when a deployment may be related to an anomaly and " +
		"the deployment contents could help evaluate that hypothesis.")]
	public DeploymentDetails? GetDeploymentDetails(
		[Description("The deployed version, such as '4.8'.")] string version)
	{
		Console.WriteLine($"*** GET DEPLOYMENT DETAILS CALLED: {version} ***");
		return _dataSource.GetDeploymentDetails(version);
	}
}
