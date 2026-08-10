using Microsoft.Extensions.VectorData;

namespace Lesson07.RetrievalAugmentedGeneration.Infrastructure.Rag;

public sealed class KnowledgeChunk
{
	[VectorStoreKey]
	public required string Id { get; init; }

	[VectorStoreData]
	public required string Source { get; init; }

	[VectorStoreData]
	public required string Content { get; init; }

	public ReadOnlyMemory<float>? Embedding { get; set; }
}