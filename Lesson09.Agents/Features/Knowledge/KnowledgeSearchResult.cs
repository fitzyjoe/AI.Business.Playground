namespace Lesson09.Agents.Features.Knowledge;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);