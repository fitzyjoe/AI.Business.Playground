using Lesson09.Agents.Infrastructure.Rag;
using Microsoft.AspNetCore.Mvc;

namespace Lesson09.Agents.Features.Knowledge;

/* This is a temporary endpoint to test out knowledge retrieval */
[ApiController]
[Route("api/[controller]")]
public sealed class KnowledgeController(
	KnowledgeRetriever _knowledgeRetriever)
	: ControllerBase
{
	[HttpGet("search")]
	public async Task<IActionResult> SearchAsync(
		[FromQuery] string query,
		CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return BadRequest("query is required.");
		}
		
		var results =
			await _knowledgeRetriever.SearchAsync(
				query,
				cancellationToken);

		return Ok(results);
	}
}