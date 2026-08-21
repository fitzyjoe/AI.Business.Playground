namespace Lesson11.ProductionAiPlatform.Features.Knowledge;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);