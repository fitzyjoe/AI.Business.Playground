namespace Lesson07.RetrievalAugmentedGeneration.Features.Conversations;

public sealed record MessageResponse(
	Guid ConversationId,
	string Content,
	string Model,
	TimeSpan Duration);