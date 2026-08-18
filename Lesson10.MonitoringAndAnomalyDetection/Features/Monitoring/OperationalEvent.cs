namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed record OperationalEvent(
	DateTimeOffset Timestamp,
	string Type,
	string Description);