using Lesson02.ControllingLlmBehavior.Features.Models.Execute;

namespace Lesson02.ControllingLlmBehavior.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiResponse> SendAsync(
		AiRequest aiRequest,
		CancellationToken cancellationToken = default);
}