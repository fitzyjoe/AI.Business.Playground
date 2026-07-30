using System.Collections.Concurrent;

namespace Lesson03.LlmConversations.Infrastructure.Conversations;

using Lesson03.LlmConversations.Features.Conversations;

public sealed class InMemoryConversationRepository : IConversationRepository
{
	private readonly ConcurrentDictionary<Guid, Conversation> _conversations = new();
	
	public Task<Conversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default)
	{
		_conversations.TryGetValue(conversationId, out var conversation);
		return Task.FromResult(conversation);
	}

	public Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		_conversations[conversation.Id] = conversation;
		return Task.CompletedTask;
	}
}