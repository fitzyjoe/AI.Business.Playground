namespace Lesson03.LlmConversations.Features.Conversations;

public class Conversation
{
	public Guid Id { get; init; } = Guid.NewGuid();
	public string SystemPrompt { get; init; } = "As an AI assistant";
	public string Provider { get; init; } = "ollama";
	public string? Model { get; init; }
	public float? Temperature { get; init; }
	public int? MaxTokens { get; init; }
	public List<ConversationMessage> Messages { get; init; } = [];
	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}