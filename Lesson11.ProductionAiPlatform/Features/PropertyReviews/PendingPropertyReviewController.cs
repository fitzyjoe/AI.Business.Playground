using Microsoft.AspNetCore.Mvc;

namespace Lesson11.ProductionAiPlatform.Features.PropertyReviews;

[ApiController]
[Route("api/pending-property-reviews")]
public sealed class PendingPropertyReviewController(PropertyReviewService _service) : ControllerBase
{
    // TODO: These pending property review requests are not idempotent right now for simplicity even though the approval is idempotent
    [HttpPost]
    public ActionResult<PendingPropertyReview> Create(CreatePendingPropertyReviewRequest request)
    {
        try
        {
            var pendingPropertyReview = _service.Propose(request);

            return CreatedAtAction(
                nameof(Get),
                new
                {
                    id = pendingPropertyReview.Id
                },
                pendingPropertyReview);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(
                new
                {
                    message = exception.Message
                });
        }
    }

    [HttpGet]
    public ActionResult<IReadOnlyCollection<PendingPropertyReview>> GetAll()
    {
        return Ok(_service.GetPending());
    }

    [HttpGet("{id:guid}")]
    public ActionResult<PendingPropertyReview> Get(Guid id)
    {
        var pendingPropertyReview = _service.GetPending(id);

        if (pendingPropertyReview is null)
        {
            return NotFound();
        }

        return Ok(pendingPropertyReview);
    }

    [HttpPost("{id:guid}/approve")]
    public ActionResult<PropertyReview> Approve(Guid id)
    {
        try
        {
            var propertyReview = _service.Approve(id);
            return Ok(propertyReview);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
    }

    [HttpPost("{id:guid}/reject")]
    public ActionResult<PendingPropertyReview> Reject(Guid id)
    {
        try
        {
            return Ok(_service.Reject(id));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(
                new
                {
                    message = exception.Message
                });
        }
    }
}