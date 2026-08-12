namespace Lesson08.SafeWriteOperations.Infrastructure.Rag;

public sealed class RagOptions
{
	public required string EmbeddingModel { get; init; }
	public required int EmbeddingDimensions { get; init; }
	public int TopResults { get; init; } = 3;
}