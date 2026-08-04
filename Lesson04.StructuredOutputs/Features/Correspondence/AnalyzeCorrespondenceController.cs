using Microsoft.AspNetCore.Mvc;

namespace Lesson04.StructuredOutputs.Features.Correspondence;

[ApiController]
[Route("api/correspondence")]
public sealed class AnalyzeCorrespondenceController(AnalyzeCorrespondenceHandler handler) : ControllerBase
{
	[HttpPost("analyze")]
	[ProducesResponseType<AnalyzeCorrespondenceResponse>(StatusCodes.Status200OK)]
	[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<AnalyzeCorrespondenceResponse>> AnalyzeAsync(AnalyzeCorrespondenceRequest request,
		CancellationToken cancellationToken)
	{
		var response = await handler.HandleAsync(request, cancellationToken);
		return Ok(response);
	}
}