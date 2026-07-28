namespace Lesson02.LlmConversations.Features.Models.Execute;

public sealed record AiResponse(
	string Text,
	string Model,
	TimeSpan Duration);