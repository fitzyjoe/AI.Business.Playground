using Lesson01.Chat.Features.Models.Execute;

namespace Lesson01.Chat.Infrastructure.Ai;

public interface IAiProvider
{
	Task<AiResponse> SendAsync(
		AiRequest aiRequest,
		CancellationToken cancellationToken = default);
}