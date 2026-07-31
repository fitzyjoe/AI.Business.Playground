namespace Lesson04.StructuredOutputs.Features.Conversations;

public sealed record MessageResponse(
	Guid ConversationId,
	string Content,
	string Model,
	TimeSpan Duration);