namespace Lesson07.RetrievalAugmentedGeneration.Features.Knowledge;

public sealed class RagOptions
{
	public required string EmbeddingModel { get; init; }
	public required int EmbeddingDimensions { get; init; }
	public int TopResults { get; init; } = 3;
}