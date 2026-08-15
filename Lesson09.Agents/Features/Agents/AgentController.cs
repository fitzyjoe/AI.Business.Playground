using Microsoft.AspNetCore.Mvc;

namespace Lesson09.Agents.Features.Agents;

[ApiController]
[Route("api/agent")]
public sealed class AgentController(PropertyReviewAgent _agent) : ControllerBase
{
	[HttpPost("run")]
	[ProducesResponseType<RunAgentResponse>(StatusCodes.Status200OK)]
	public async Task<ActionResult<RunAgentResponse>> RunAsync(RunAgentRequest request, CancellationToken cancellationToken)
	{
		var text = await _agent.RunAsync(request.Objective, cancellationToken);
		return Ok(new RunAgentResponse(text));
	}
}