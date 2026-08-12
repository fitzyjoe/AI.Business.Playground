namespace Lesson08.SafeWriteOperations.Features.PropertyReviews;

public interface IPropertyReviewRepository
{
	void Add(PropertyReview propertyReview);

	PropertyReview? Get(Guid id);

	PropertyReview? GetByPendingPropertyReviewId(Guid pendingPropertyReviewId);

	IReadOnlyCollection<PropertyReview> GetAll();
}