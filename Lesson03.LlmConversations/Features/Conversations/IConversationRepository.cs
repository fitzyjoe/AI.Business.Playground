namespace Lesson03.LlmConversations.Features.Conversations;

public interface IConversationRepository
{
	Task<Conversation?> GetAsync(
		Guid conversationId,
		CancellationToken cancellationToken = default);
	
	Task SaveAsync(
		Conversation conversation,
		CancellationToken cancellationToken = default);
}