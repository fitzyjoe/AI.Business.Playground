namespace Lesson06.ConsumingMcpServers.Infrastructure.Ai;

public sealed record AiChatResponse(
	string Text,
	string Model,
	TimeSpan Duration);