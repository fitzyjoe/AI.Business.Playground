using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lesson11.ProductionAiPlatform.Features.Conversations;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class MessageController(MessageHandler _messageHandler) : ControllerBase
{
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public Task<MessageResponse> Post(
		MessageRequest request,
		CancellationToken cancellationToken)
	{
		return _messageHandler.HandleAsync(request, cancellationToken);
	}
}
