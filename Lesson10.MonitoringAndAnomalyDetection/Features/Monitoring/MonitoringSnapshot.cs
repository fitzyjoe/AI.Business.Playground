namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed record MonitoringSnapshot(
	IReadOnlyList<AnomalyCandidate> Anomalies,
	IReadOnlyDictionary<string, IReadOnlyList<MetricObservation>> MetricHistory,
	IReadOnlyList<OperationalEvent> RecentEvents);