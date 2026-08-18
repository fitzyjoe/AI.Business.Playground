namespace Lesson10.MonitoringAndAnomalyDetection.Features.PropertyReviews;

public interface IPropertyReviewRepository
{
	void Add(PropertyReview propertyReview);

	PropertyReview? Get(Guid id);

	PropertyReview? GetByPendingPropertyReviewId(Guid pendingPropertyReviewId);

	IReadOnlyCollection<PropertyReview> GetAll();
}