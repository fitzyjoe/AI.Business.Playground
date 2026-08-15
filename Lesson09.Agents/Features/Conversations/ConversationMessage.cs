namespace Lesson09.Agents.Features.Conversations;

public class ConversationMessage
{
	public required ConversationRole Role { get; init; }
	public required string Content { get; init; }
	public required DateTimeOffset CreatedAt { get; init; }
}