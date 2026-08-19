using Lesson11.ProductionAiPlatform.Features.PropertyReviews;

namespace Lesson11.ProductionAiPlatform.Tests.Features.PropertyReviews;

public sealed class PropertyReviewServiceTests
{
	[Fact]
	public void ApproveIsIdempotent()
	{
		var service = CreateService();
		var pending = service.Propose(
			new CreatePendingPropertyReviewRequest(
				"0304-12-0042",
				"Client disputes the assessment.",
				PropertyReviewPriority.High));

		var first = service.Approve(pending.Id);
		var second = service.Approve(pending.Id);

		Assert.Equal(first.Id, second.Id);
		Assert.Single(service.GetReviews());
		Assert.Equal(PendingPropertyReviewStatus.Executed, service.GetPending(pending.Id)?.Status);
	}

	[Fact]
	public void RejectedProposalCannotBeApproved()
	{
		var service = CreateService();
		var pending = service.Propose(
			new CreatePendingPropertyReviewRequest(
				"0304-12-0042",
				"Client disputes the assessment.",
				PropertyReviewPriority.Normal));

		service.Reject(pending.Id);

		Assert.Throws<InvalidOperationException>(() => service.Approve(pending.Id));
		Assert.Empty(service.GetReviews());
	}

	[Fact]
	public void RejectIsIdempotent()
	{
		var service = CreateService();
		var pending = service.Propose(
			new CreatePendingPropertyReviewRequest(
				"0304-12-0042",
				"Client disputes the assessment.",
				PropertyReviewPriority.Low));

		var first = service.Reject(pending.Id);
		var second = service.Reject(pending.Id);

		Assert.Equal(first.Id, second.Id);
		Assert.Equal(PendingPropertyReviewStatus.Rejected, second.Status);
		Assert.Single(service.GetPending());
	}

	private static PropertyReviewService CreateService()
	{
		return new PropertyReviewService(
			new InMemoryPendingPropertyReviewRepository(),
			new InMemoryPropertyReviewRepository());
	}
}
