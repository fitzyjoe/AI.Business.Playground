namespace Lesson10.MonitoringAndAnomalyDetection.Features.Knowledge;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);