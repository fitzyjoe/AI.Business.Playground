using System.Text.Json.Nodes;

namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

public sealed record AiChatRequest
{
	public required IReadOnlyList<AiChatMessage> Messages
	{
		get;
		init;
	}

	public string? Model { get; init; }

	public float? Temperature { get; init; }

	public int? MaxTokens { get; init; }
	
	public JsonNode? ResponseFormat { get; init; }

	public bool Stream { get; init; } = true;
}