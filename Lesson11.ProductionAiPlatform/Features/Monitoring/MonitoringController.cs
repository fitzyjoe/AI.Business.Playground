using Microsoft.AspNetCore.Mvc;

namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

[ApiController]
[Route("api/monitoring")]
public sealed class MonitoringController(MonitoringService _monitoringService) : ControllerBase
{
	[HttpGet("scan")]
	public async Task<ActionResult<MonitoringAssessment?>> ScanAsync(CancellationToken cancellationToken)
	{
		return Ok(await _monitoringService.ScanAsync(cancellationToken));
	}
}