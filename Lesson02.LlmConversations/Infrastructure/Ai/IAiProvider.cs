using Lesson02.LlmConversations.Features.Models.Execute;

namespace Lesson02.LlmConversations.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiResponse> SendAsync(
		AiRequest aiRequest,
		CancellationToken cancellationToken = default);
}