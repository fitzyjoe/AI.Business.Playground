namespace Lesson06.ConsumingMcpServers.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiChatResponse> SendAsync(
		AiChatRequest request,
		CancellationToken cancellationToken = default);
}