namespace Lesson11.ProductionAiPlatform.Features.Conversations;

public interface IConversationRepository
{
	Task<Conversation?> GetAsync(
		Guid conversationId,
		string ownerId,
		CancellationToken cancellationToken = default);
	
	Task SaveAsync(
		Conversation conversation,
		CancellationToken cancellationToken = default);
}