using Lesson04.StructuredOutputs.Features.Conversations;

namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

public sealed record AiChatRequest
{
	public required IReadOnlyList<ConversationMessage> Messages
	{
		get;
		init;
	}

	public string? Model { get; init; }

	public float? Temperature { get; init; }

	public int? MaxTokens { get; init; }
}