using Microsoft.AspNetCore.Mvc;

namespace Lesson11.ProductionAiPlatform.Features.PropertyReviews;

[ApiController]
[Route("api/property-reviews")]
public sealed class PropertyReviewController(PropertyReviewService _service) : ControllerBase
{
	[HttpGet]
	public ActionResult<IReadOnlyCollection<PropertyReview>> GetAll()
	{
		return Ok(_service.GetReviews());
	}

	[HttpGet("{id:guid}")]
	public ActionResult<PropertyReview> Get(Guid id)
	{
		var propertyReview = _service.GetReview(id);
		return propertyReview is null ? NotFound() : Ok(propertyReview);
	}
}