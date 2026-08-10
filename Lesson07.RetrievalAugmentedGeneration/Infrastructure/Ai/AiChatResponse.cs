namespace Lesson07.RetrievalAugmentedGeneration.Infrastructure.Ai;

public sealed record AiChatResponse(
	string Text,
	string Model,
	TimeSpan Duration);