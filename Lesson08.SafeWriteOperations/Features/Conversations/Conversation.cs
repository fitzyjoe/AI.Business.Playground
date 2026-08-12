namespace Lesson08.SafeWriteOperations.Features.Conversations;

public class Conversation
{
	public Guid Id { get; init; } = Guid.NewGuid();
	public required string SystemPrompt { get; init; }
	public string Provider { get; init; } = "ollama";
	public string? Model { get; init; }
	public float? Temperature { get; init; }
	public int? MaxTokens { get; init; }
	public List<ConversationMessage> Messages { get; init; } = [];
	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}