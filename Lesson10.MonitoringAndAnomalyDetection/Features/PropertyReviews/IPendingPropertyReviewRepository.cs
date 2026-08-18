namespace Lesson10.MonitoringAndAnomalyDetection.Features.PropertyReviews;

public interface IPendingPropertyReviewRepository
{
	void Add(PendingPropertyReview pendingPropertyReview);

	PendingPropertyReview? Get(Guid id);

	IReadOnlyCollection<PendingPropertyReview> GetAll();

	void Update(PendingPropertyReview pendingPropertyReview);
}