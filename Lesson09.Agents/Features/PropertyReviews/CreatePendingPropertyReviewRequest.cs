namespace Lesson09.Agents.Features.PropertyReviews;

public sealed record CreatePendingPropertyReviewRequest(
	string ParcelNumber,
	string Reason,
	PropertyReviewPriority Priority);