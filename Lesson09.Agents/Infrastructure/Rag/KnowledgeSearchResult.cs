namespace Lesson09.Agents.Infrastructure.Rag;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);