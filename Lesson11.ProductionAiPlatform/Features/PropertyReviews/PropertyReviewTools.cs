using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Lesson11.ProductionAiPlatform.Features.PropertyReviews;

public sealed class PropertyReviewTools(
	PropertyReviewService _service,
	IHttpContextAccessor _httpContextAccessor,
	IAuthorizationService _authorizationService)
{
	[Description(
		"Creates a pending property review proposal that requires human approval. " +
		"This does not approve or execute the property review. " +
		"The authenticated caller must satisfy the Reviewer authorization policy.")]
	public async Task<PropertyReviewProposalToolResult> ProposePropertyReviewAsync(
		[Description("The parcel number for the property.")]
		string parcelNumber,
		[Description("The reason a property review is being requested.")]
		string reason,
		[Description("The priority of the review: Low, Normal, or High.")]
		PropertyReviewPriority priority)
	{
		var user = _httpContextAccessor.HttpContext?.User;

		if (user?.Identity?.IsAuthenticated != true)
		{
			return new PropertyReviewProposalToolResult(
				false,
				"No authenticated caller is associated with this AI request.",
				null);
		}

		var authorization = await _authorizationService.AuthorizeAsync(
			user,
			resource: null,
			policyName: "Reviewer");

		if (!authorization.Succeeded)
		{
			return new PropertyReviewProposalToolResult(
				false,
				"The current caller is not authorized to create property-review proposals.",
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
