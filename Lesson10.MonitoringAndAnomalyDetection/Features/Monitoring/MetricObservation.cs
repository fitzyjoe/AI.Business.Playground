namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed record MetricObservation(
	string Metric,
	DateTimeOffset Timestamp,
	double Value);