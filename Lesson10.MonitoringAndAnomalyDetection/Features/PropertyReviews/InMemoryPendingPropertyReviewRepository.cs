using System.Collections.Concurrent;

namespace Lesson10.MonitoringAndAnomalyDetection.Features.PropertyReviews;

public sealed class InMemoryPendingPropertyReviewRepository : IPendingPropertyReviewRepository
{
	private readonly ConcurrentDictionary<Guid, PendingPropertyReview> _pendingPropertyReviews = new();

	public void Add(PendingPropertyReview pendingPropertyReview)
	{
		if (!_pendingPropertyReviews.TryAdd(pendingPropertyReview.Id, pendingPropertyReview))
		{
			throw new InvalidOperationException($"Pending property review '{pendingPropertyReview.Id}' already exists.");
		}
	}

	public PendingPropertyReview? Get(Guid id)
	{
		_pendingPropertyReviews.TryGetValue(id, out var pendingPropertyReview);

		return pendingPropertyReview;
	}

	public IReadOnlyCollection<PendingPropertyReview> GetAll()
	{
		return _pendingPropertyReviews.Values
			.OrderByDescending(review => review.CreatedAt)
			.ToArray();
	}

	public void Update(PendingPropertyReview pendingPropertyReview)
	{
		if (!_pendingPropertyReviews.ContainsKey(pendingPropertyReview.Id))
		{
			throw new KeyNotFoundException($"Pending property review '{pendingPropertyReview.Id}' was not found.");
		}

		_pendingPropertyReviews[pendingPropertyReview.Id] = pendingPropertyReview;
	}
}