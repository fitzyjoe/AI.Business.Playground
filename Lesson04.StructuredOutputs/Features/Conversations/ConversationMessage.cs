namespace Lesson04.StructuredOutputs.Features.Conversations;

public class ConversationMessage
{
	public required ConversationRole Role { get; init; }
	public required string Content { get; init; }
	public required DateTimeOffset CreatedAt { get; init; }
}