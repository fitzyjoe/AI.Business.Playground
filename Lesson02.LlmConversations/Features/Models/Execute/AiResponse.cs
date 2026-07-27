namespace Lesson01.Chat.Features.Models.Execute;

public sealed record AiResponse(
	string Text,
	string Model,
	TimeSpan Duration);