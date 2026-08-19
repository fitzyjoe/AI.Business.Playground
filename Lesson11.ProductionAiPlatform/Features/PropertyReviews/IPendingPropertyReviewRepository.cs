namespace Lesson11.ProductionAiPlatform.Features.PropertyReviews;

public interface IPendingPropertyReviewRepository
{
	void Add(PendingPropertyReview pendingPropertyReview);

	PendingPropertyReview? Get(Guid id);

	IReadOnlyCollection<PendingPropertyReview> GetAll();

	void Update(PendingPropertyReview pendingPropertyReview);
}