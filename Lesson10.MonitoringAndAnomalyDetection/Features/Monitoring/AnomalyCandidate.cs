namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed record AnomalyCandidate(
	string Metric,
	DateTimeOffset Timestamp,
	double Value,
	double BaselineMean,
	double BaselineStandardDeviation,
	double ZScore);