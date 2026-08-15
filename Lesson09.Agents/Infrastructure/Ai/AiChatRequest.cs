using Lesson09.Agents.Features.Conversations;

namespace Lesson09.Agents.Infrastructure.Ai;

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