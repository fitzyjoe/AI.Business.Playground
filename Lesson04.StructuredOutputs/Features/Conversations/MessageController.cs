using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Lesson04.StructuredOutputs.Features.Conversations;

[ApiController]
[Route("api/[controller]")]
public class MessageController(MessageHandler _messageHandler) : ControllerBase
{
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<MessageResponse>> Post(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		return await _messageHandler.HandleAsync(messageRequest, cancellationToken);
	}
}