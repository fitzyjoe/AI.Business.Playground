namespace Lesson03.LlmConversations.Infrastructure.Ai;

public sealed record AiChatResponse(
	string Text,
	string Model,
	TimeSpan Duration);