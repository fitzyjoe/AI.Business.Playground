namespace Lesson08.SafeWriteOperations.Features.Knowledge;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);