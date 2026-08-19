using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Lesson11.ProductionAiPlatform.Features.Conversations;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class MessageController(
	MessageHandler _messageHandler,
	AiExecutionContextAccessor _executionContextAccessor) : ControllerBase
{
	[HttpPost]
	[ProducesResponseType(StatusCodes.Status200OK)]
	public async Task<ActionResult<MessageResponse>> Post(MessageRequest messageRequest, CancellationToken cancellationToken)
	{
		var capabilities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		
		if (messageRequest.AllowWriteProposal && User.IsInRole("Reviewer"))
		{
			capabilities.Add(AiCapabilities.ProposePropertyReview);
		}
		
		var executionContext = new AiExecutionContext(
			User.Identity?.Name ?? "unknown",
			HttpContext.TraceIdentifier,
			capabilities);
		
		using var scope = _executionContextAccessor.Push(executionContext);
		
		try
		{
			return await _messageHandler.HandleAsync(messageRequest, cancellationToken);
		}
		catch (AiPolicyViolationException exception)
		{
			return BadRequest(new { message = exception.Message });
		}
		catch (AiRequestTimeoutException exception)
		{
			return Problem(
				statusCode: StatusCodes.Status504GatewayTimeout,
				title: "AI request timed out",
				detail: exception.Message);
		}
	}
}