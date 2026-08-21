using Microsoft.Extensions.VectorData;

namespace Lesson08.SafeWriteOperations.Features.Knowledge;

public sealed class KnowledgeChunk
{
	public required string Id { get; init; }

	public required string Source { get; init; }

	public required string Content { get; init; }

	public string Embedding => Content;
}