namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

public sealed record MetricObservation(
	string Metric,
	DateTimeOffset Timestamp,
	double Value);