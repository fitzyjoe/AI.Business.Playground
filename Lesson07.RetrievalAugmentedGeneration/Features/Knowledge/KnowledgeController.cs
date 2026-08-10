using Lesson07.RetrievalAugmentedGeneration.Infrastructure.Rag;
using Microsoft.AspNetCore.Mvc;

namespace Lesson07.RetrievalAugmentedGeneration.Features.Knowledge;

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
		var results =
			await _knowledgeRetriever.SearchAsync(
				query,
				cancellationToken);

		return Ok(results);
	}
}