namespace Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiChatResponse> SendAsync(
		AiChatRequest request,
		CancellationToken cancellationToken = default);
}