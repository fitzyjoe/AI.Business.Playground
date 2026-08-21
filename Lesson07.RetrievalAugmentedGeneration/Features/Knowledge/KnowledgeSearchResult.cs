namespace Lesson07.RetrievalAugmentedGeneration.Features.Knowledge;

public sealed record KnowledgeSearchResult(
	string Source,
	string Content,
	double? Score);