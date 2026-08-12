namespace Lesson08.SafeWriteOperations.Features.PropertyReviews;

public sealed class PropertyReviewService(
    IPendingPropertyReviewRepository _pendingRepository,
    IPropertyReviewRepository _propertyReviewRepository)
{
    private readonly Lock _executionLock = new();
                                                                                                                          
    public PendingPropertyReview Propose(CreatePendingPropertyReviewRequest request)
    {
        Validate(request);

        var pendingPropertyReview =
            new PendingPropertyReview
            {
                Id = Guid.NewGuid(),
                ParcelNumber = request.ParcelNumber.Trim(),
                Reason = request.Reason.Trim(),
                Priority = request.Priority,
                Status = PendingPropertyReviewStatus.PendingApproval,
                CreatedAt = DateTimeOffset.UtcNow
            };

        _pendingRepository.Add(pendingPropertyReview);

        return pendingPropertyReview;
    }

    public PendingPropertyReview? GetPending(Guid id)
    {
        return _pendingRepository.Get(id);
    }

    public IReadOnlyCollection<PendingPropertyReview> GetPending()
    {
        return _pendingRepository.GetAll();
    }

    public IReadOnlyCollection<PropertyReview> GetReviews()
    {
        return _propertyReviewRepository.GetAll();
    }

    public PropertyReview? GetReview(Guid id)
    {
        return _propertyReviewRepository.Get(id);
    }

    public PropertyReview Approve(Guid id)
    {
        lock (_executionLock)
        {
            var pendingPropertyReview = _pendingRepository.Get(id) ?? throw new KeyNotFoundException($"Pending property review '{id}' was not found.");

            // approving an already-executed proposal returns the review that was already created.
            if (pendingPropertyReview.Status == PendingPropertyReviewStatus.Executed)
            {
                return GetExecutedReview(pendingPropertyReview);
            }

            if (pendingPropertyReview.Status == PendingPropertyReviewStatus.Rejected)
            {
                throw new InvalidOperationException("A rejected property review cannot be approved.");
            }

            var existingReview = _propertyReviewRepository.GetByPendingPropertyReviewId(id);
            if (existingReview is not null)
            {
                MarkExecuted(pendingPropertyReview, existingReview.Id);
                return existingReview;
            }

            pendingPropertyReview.Status = PendingPropertyReviewStatus.Approved;

            pendingPropertyReview.ApprovedAt = DateTimeOffset.UtcNow;

            _pendingRepository.Update(pendingPropertyReview);

            var propertyReview =
                new PropertyReview
                {
                    Id = Guid.NewGuid(),
                    SourcePendingPropertyReviewId = pendingPropertyReview.Id,
                    ParcelNumber = pendingPropertyReview.ParcelNumber,
                    Reason = pendingPropertyReview.Reason,
                    Priority = pendingPropertyReview.Priority,
                    CreatedAt = DateTimeOffset.UtcNow
                };

            _propertyReviewRepository.Add(propertyReview);

            MarkExecuted(pendingPropertyReview, propertyReview.Id);

            return propertyReview;
        }
    }

    public PendingPropertyReview Reject(Guid id)
    {
        lock (_executionLock)
        {
            var pendingPropertyReview = _pendingRepository.Get(id) ?? throw new KeyNotFoundException($"Pending property review '{id}' was not found.");

            if (pendingPropertyReview.Status == PendingPropertyReviewStatus.Executed)
            {
                throw new InvalidOperationException("An executed property review cannot be rejected.");
            }

            // Making rejection idempotent is convenient.
            if (pendingPropertyReview.Status == PendingPropertyReviewStatus.Rejected)
            {
                return pendingPropertyReview;
            }

            pendingPropertyReview.Status = PendingPropertyReviewStatus.Rejected;

            pendingPropertyReview.RejectedAt = DateTimeOffset.UtcNow;

            _pendingRepository.Update(pendingPropertyReview);

            return pendingPropertyReview;
        }
    }

    private PropertyReview GetExecutedReview(PendingPropertyReview pendingPropertyReview)
    {
        if (pendingPropertyReview.PropertyReviewId is null)
        {
            throw new InvalidOperationException("The proposal is marked as executed but does not reference a property review.");
        }

        return _propertyReviewRepository.Get(pendingPropertyReview.PropertyReviewId.Value) ?? throw new InvalidOperationException("The executed property review could not be found.");
    }

    private void MarkExecuted(PendingPropertyReview pendingPropertyReview, Guid propertyReviewId)
    {
        pendingPropertyReview.Status = PendingPropertyReviewStatus.Executed;

        pendingPropertyReview.PropertyReviewId = propertyReviewId;

        pendingPropertyReview.ExecutedAt = DateTimeOffset.UtcNow;

        _pendingRepository.Update(pendingPropertyReview);
    }

    // TODO: validation in the future could use MCP to validate that the parcel number actually exists
    private static void Validate(CreatePendingPropertyReviewRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ParcelNumber))
        {
            throw new ArgumentException("Parcel number is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new ArgumentException("Reason is required.");
        }

        if (!Enum.IsDefined(request.Priority))
        {
            throw new ArgumentException("Priority is invalid.");
        }
    }
}