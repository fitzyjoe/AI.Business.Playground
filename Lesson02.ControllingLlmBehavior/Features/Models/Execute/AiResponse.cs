namespace Lesson02.ControllingLlmBehavior.Features.Models.Execute;

public sealed record AiResponse(
	string Text,
	string Model,
	TimeSpan Duration);