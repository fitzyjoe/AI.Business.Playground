namespace Lesson08.SafeWriteOperations.Features.PropertyReviews;

public sealed record CreatePendingPropertyReviewRequest(
	string ParcelNumber,
	string Reason,
	PropertyReviewPriority Priority);