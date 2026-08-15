namespace Lesson09.Agents.Infrastructure.Ai;

public sealed record AiChatResponse(
	string Text,
	string Model,
	TimeSpan Duration);