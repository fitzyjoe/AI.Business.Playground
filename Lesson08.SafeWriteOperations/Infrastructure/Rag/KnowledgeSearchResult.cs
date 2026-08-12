namespace Lesson08.SafeWriteOperations.Infrastructure.Rag;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);