namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed record AnomalyAssessment(
	string Metric,
	string Severity,
	string Summary,
	string[] Evidence,
	string[] PossibleCauses,
	string[] RecommendedChecks);