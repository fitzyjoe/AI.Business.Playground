using System.Collections.Concurrent;

namespace Lesson04.StructuredOutputs.Infrastructure.Conversations;

using Lesson04.StructuredOutputs.Features.Conversations;

public sealed class InMemoryConversationRepository : IConversationRepository
{
	private readonly ConcurrentDictionary<Guid, Conversation> _conversations = new();
	
	public Task<Conversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		_conversations.TryGetValue(conversationId, out var conversation);
		return Task.FromResult(conversation);
	}

	public Task SaveAsync(Conversation conversation, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		_conversations[conversation.Id] = conversation;
		return Task.CompletedTask;
	}
}