using System.ComponentModel;
using Lesson11.ProductionAiPlatform.Infrastructure.Ai;

namespace Lesson11.ProductionAiPlatform.Features.PropertyReviews;

public sealed class PropertyReviewTools(
	PropertyReviewService _service)
{
	[Description(
		"Creates a pending property review proposal that requires human approval. " +
		"This does not approve or execute the property review. " +
		"The current request must have been granted the property-review proposal capability.")]
	public PropertyReviewProposalToolResult ProposePropertyReview(
		[Description("The parcel number for the property.")]
		string parcelNumber,
		[Description("The reason a property review is being requested.")]
		string reason,
		[Description("The priority of the review: Low, Normal, or High.")]
		PropertyReviewPriority priority)
	{
		var executionContext = _executionContextAccessor.Current;

		if (executionContext is null || !executionContext.HasCapability(AiCapabilities.ProposePropertyReview))
		{
			return new PropertyReviewProposalToolResult(
				false,
				"The current request is not authorized to create a property-review proposal.",
				null);
		}

		var proposal = _service.Propose(
			new CreatePendingPropertyReviewRequest(
				parcelNumber,
				reason,
				priority));

		return new PropertyReviewProposalToolResult(
			true,
			"A pending proposal was created. Human approval is still required before execution.",
			proposal);
	}
}