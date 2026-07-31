namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiChatResponse> SendAsync(
		AiChatRequest request,
		CancellationToken cancellationToken = default);
}