namespace Lesson11.ProductionAiPlatform.Features.Conversations;

public sealed record MessageResponse(
	Guid ConversationId,
	string Content,
	string Model,
	TimeSpan Duration);