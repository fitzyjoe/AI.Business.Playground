namespace Lesson09.Agents.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiChatResponse> SendAsync(
		AiChatRequest request,
		CancellationToken cancellationToken = default);
}