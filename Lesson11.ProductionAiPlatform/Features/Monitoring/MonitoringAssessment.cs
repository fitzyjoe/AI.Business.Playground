namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

public sealed record MonitoringAssessment(
	string Severity,
	string Summary,
	string[] Correlations,
	RelevantOperationalEvent[] RelevantEvents,
	string[] PossibleCauses,
	string[] RecommendedChecks);