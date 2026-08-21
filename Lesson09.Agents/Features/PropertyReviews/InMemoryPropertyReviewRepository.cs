using System.Collections.Concurrent;

namespace Lesson09.Agents.Features.PropertyReviews;

public sealed class InMemoryPropertyReviewRepository : IPropertyReviewRepository
{
	private readonly ConcurrentDictionary<Guid, PropertyReview> _propertyReviews = new();

	private readonly ConcurrentDictionary<Guid, Guid> _reviewIdsByPendingPropertyReviewId = new();

	public void Add(PropertyReview propertyReview)
	{
		if (!_reviewIdsByPendingPropertyReviewId.TryAdd(propertyReview.SourcePendingPropertyReviewId, propertyReview.Id))
		{
			throw new InvalidOperationException($"A property review has already been created for pending review '{propertyReview.SourcePendingPropertyReviewId}'.");
		}

		if (!_propertyReviews.TryAdd(propertyReview.Id, propertyReview))
		{
			_reviewIdsByPendingPropertyReviewId.TryRemove(propertyReview.SourcePendingPropertyReviewId, out _);

			throw new InvalidOperationException($"Property review '{propertyReview.Id}' already exists.");
		}
	}

	public PropertyReview? Get(Guid id)
	{
		_propertyReviews.TryGetValue(id, out var propertyReview);

		return propertyReview;
	}

	public PropertyReview? GetByPendingPropertyReviewId(Guid pendingPropertyReviewId)
	{
		if (!_reviewIdsByPendingPropertyReviewId.TryGetValue(pendingPropertyReviewId, out var propertyReviewId))
		{
			return null;
		}

		return Get(propertyReviewId);
	}

	public IReadOnlyCollection<PropertyReview> GetAll()
	{
		return _propertyReviews.Values
			.OrderByDescending(review => review.CreatedAt)
			.ToArray();
	}
}