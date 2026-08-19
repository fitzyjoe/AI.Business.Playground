namespace Lesson11.ProductionAiPlatform.Infrastructure.Rag;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);