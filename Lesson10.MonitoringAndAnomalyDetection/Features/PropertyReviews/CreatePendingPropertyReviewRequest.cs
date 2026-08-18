namespace Lesson10.MonitoringAndAnomalyDetection.Features.PropertyReviews;

public sealed record CreatePendingPropertyReviewRequest(
	string ParcelNumber,
	string Reason,
	PropertyReviewPriority Priority);