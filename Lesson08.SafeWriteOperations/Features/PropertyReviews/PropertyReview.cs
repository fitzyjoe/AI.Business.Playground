namespace Lesson08.SafeWriteOperations.Features.PropertyReviews;

public sealed class PropertyReview
{
	public required Guid Id { get; init; }

	public required Guid SourcePendingPropertyReviewId { get; init; }

	public required string ParcelNumber { get; init; }

	public required string Reason { get; init; }

	public required PropertyReviewPriority Priority { get; init; }

	public required DateTimeOffset CreatedAt { get; init; }
}