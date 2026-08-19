namespace Lesson11.ProductionAiPlatform.Features.PropertyReviews;

public sealed record PropertyReviewProposalToolResult(
	bool Authorized,
	string Message,
	PendingPropertyReview? Proposal);