using System.ComponentModel;

namespace Lesson09.Agents.Features.PropertyReviews;

public sealed class PropertyReviewTools(PropertyReviewService _service)
{
	[Description("Creates a pending property review proposal that requires human approval. This does not approve or execute the property review.")]
	public PendingPropertyReview ProposePropertyReview(
		[Description("The parcel number for the property.")] string parcelNumber,
		[Description("The reason a property review is being requested.")] string reason,
		[Description("The priority of the review: Low, Normal, or High.")] PropertyReviewPriority priority)
	{
		return _service.Propose(new CreatePendingPropertyReviewRequest(parcelNumber, reason, priority));
	}
}