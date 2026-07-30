using Lesson03.LlmConversations.Features.Conversations;

namespace Lesson03.LlmConversations.Infrastructure.Ai;

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