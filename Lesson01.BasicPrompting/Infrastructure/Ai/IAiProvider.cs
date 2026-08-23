using Lesson01.BasicPrompting.Features.Models.Execute;

namespace Lesson01.BasicPrompting.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiResponse> SendAsync(AiRequest aiRequest, CancellationToken cancellationToken = default);
}