namespace Lesson06.ConsumingMcpServers.Features.Conversations;

public sealed record MessageResponse(
	Guid ConversationId,
	string Content,
	string Model,
	TimeSpan Duration);