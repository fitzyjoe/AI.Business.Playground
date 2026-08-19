namespace Lesson11.ProductionAiPlatform.Features.PropertyReviews;

public sealed record CreatePendingPropertyReviewRequest(
	string ParcelNumber,
	string Reason,
	PropertyReviewPriority Priority);