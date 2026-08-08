using Lesson06.ConsumingMcpServers.Features.Conversations;

namespace Lesson06.ConsumingMcpServers.Infrastructure.Ai;

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