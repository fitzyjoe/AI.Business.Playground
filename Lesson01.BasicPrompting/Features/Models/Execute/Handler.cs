using Lesson01.BasicPrompting.Infrastructure.Ai;

namespace Lesson01.BasicPrompting.Features.Models.Execute;

public sealed class Handler(IAiProvider aiProvider)
{
	public async Task<AiResponse> Handle(AiRequest aiRequest, CancellationToken cancellationToken)
	{
		var aiAiResponse = await aiProvider.SendAsync(aiRequest, cancellationToken);
		return aiAiResponse;
	}
}