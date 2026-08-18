namespace Lesson10.MonitoringAndAnomalyDetection.Infrastructure.Rag;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);