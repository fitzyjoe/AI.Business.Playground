namespace Lesson03.LlmConversations.Features.Conversations;

public sealed class ConversationNotFoundException(
	Guid _conversationId)
	: Exception($"Conversation '{_conversationId}' was not found.")
{
	public Guid ConversationId { get; } = _conversationId;
}