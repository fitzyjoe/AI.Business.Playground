using Lesson02.LlmConversations.Infrastructure.Ai;

namespace Lesson02.LlmConversations.Features.Models.Execute;

public sealed class Handler(
	IAiProviderFactory aiProviderFactory)
{
	public async Task<AiResponse> HandleAsync(
		AiRequest aiRequest,
		CancellationToken cancellationToken)
	{
		var aiProvider = aiProviderFactory.GetProvider(aiRequest.Provider);

		return await aiProvider.SendAsync(
				aiRequest,
				cancellationToken);
	}
}