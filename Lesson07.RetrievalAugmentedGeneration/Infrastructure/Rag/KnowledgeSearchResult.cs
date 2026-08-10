namespace Lesson07.RetrievalAugmentedGeneration.Infrastructure.Rag;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);