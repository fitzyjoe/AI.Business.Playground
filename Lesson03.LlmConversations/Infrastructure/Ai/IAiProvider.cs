using Lesson03.LlmConversations.Features.Conversations;

namespace Lesson03.LlmConversations.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiChatResponse> SendAsync(
		AiChatRequest request,
		CancellationToken cancellationToken = default);
}