using System.Collections.Concurrent;

namespace Lesson11.ProductionAiPlatform.Features.Conversations;

public sealed class InMemoryConversationRepository : IConversationRepository
{
	private readonly ConcurrentDictionary<Guid, Conversation> _conversations = new();
	
	public Task<Conversation?> GetAsync(Guid conversationId, string ownerId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		
		if (!_conversations.TryGetValue(conversationId, out var conversation))
		{
			return Task.FromResult<Conversation?>(null);
		}
		
		if (!string.Equals(conversation.OwnerId, ownerId, StringComparison.Ordinal))
		{
			return Task.FromResult<Conversation?>(null);
		}
		
		return Task.FromResult<Conversation?>(conversation);
	}

	public Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		_conversations[conversation.Id] = conversation;
		return Task.CompletedTask;
	}
}