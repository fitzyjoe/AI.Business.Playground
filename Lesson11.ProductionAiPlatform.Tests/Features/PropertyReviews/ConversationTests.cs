using Lesson11.ProductionAiPlatform.Features.Conversations;

namespace Lesson11.ProductionAiPlatform.Tests.Features.PropertyReviews;

public sealed class ConversationTests
{
	[Fact]
	public async Task GetAsyncDoesNotReturnConversationOwnedByAnotherUser()
	{
		var repository = new InMemoryConversationRepository();

		var conversation = new Conversation
		{
			OwnerId = "reader-user"
		};

		await repository.SaveAsync(conversation, TestContext.Current.CancellationToken);

		var result = await repository.GetAsync(conversation.Id, "reviewer-user", TestContext.Current.CancellationToken);

		Assert.Null(result);
	}
}