using Lesson01.Chat.Infrastructure.Ai;

namespace Lesson01.Chat.Features.Models.Execute;


public sealed class Handler(
	IAiProviderFactory aiProviderFactory)
{
	public async Task<AiResponse> Handle(
		AiRequest aiRequest,
		CancellationToken cancellationToken)
	{
		var aiProvider = aiProviderFactory.GetProvider(aiRequest.Model);

		AiResponse aiAiResponse =
			await aiProvider.SendAsync(
				aiRequest,
				cancellationToken);
		
		return aiAiResponse;
	}
}