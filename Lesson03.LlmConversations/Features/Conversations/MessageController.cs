using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Lesson03.LlmConversations.Features.Conversations;

[ApiController]
[Route("api/[controller]")]
public class MessageController(MessageHandler _messageHandler) : ControllerBase
{
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status201Created)]
	public async Task<ActionResult<MessageResponse>> Post(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		return await _messageHandler.HandleAsync(messageRequest, cancellationToken);
	}
}