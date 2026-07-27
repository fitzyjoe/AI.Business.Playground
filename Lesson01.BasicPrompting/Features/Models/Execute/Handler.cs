using Lesson01.Chat.Infrastructure.Ai;

namespace Lesson01.Chat.Features.Models.Execute;


public sealed class Handler(
	IAiProvider aiProvider)
{
	public async Task<AiResponse> Handle(
		Request request,
		CancellationToken cancellationToken)
	{
		AiResponse aiAiResponse =
			await aiProvider.SendAsync(
				request,
				cancellationToken);
		
		return aiAiResponse;
	}
}