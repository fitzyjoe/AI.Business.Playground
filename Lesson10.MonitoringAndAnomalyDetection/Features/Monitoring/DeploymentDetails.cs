namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed record DeploymentDetails(
	string Version,
	DateTimeOffset DeployedAt,
	string Service,
	string[] Changes);