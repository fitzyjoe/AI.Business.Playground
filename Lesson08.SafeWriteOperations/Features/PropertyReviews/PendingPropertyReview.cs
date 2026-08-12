namespace Lesson08.SafeWriteOperations.Features.PropertyReviews;

public sealed class PendingPropertyReview
{
	public required Guid Id { get; init; }

	public required string ParcelNumber { get; init; }

	public required string Reason { get; init; }

	public required PropertyReviewPriority Priority { get; init; }

	public required PendingPropertyReviewStatus Status { get; set; }

	public required DateTimeOffset CreatedAt { get; init; }

	public DateTimeOffset? ApprovedAt { get; set; }

	public DateTimeOffset? RejectedAt { get; set; }

	public DateTimeOffset? ExecutedAt { get; set; }

	public Guid? PropertyReviewId { get; set; }
}