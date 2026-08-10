using Lesson07.RetrievalAugmentedGeneration.Features.Conversations;

namespace Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai;

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