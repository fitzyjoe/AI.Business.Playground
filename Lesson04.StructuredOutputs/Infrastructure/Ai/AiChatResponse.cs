namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

public sealed record AiChatResponse(
	string Text,
	string Model,
	TimeSpan Duration);