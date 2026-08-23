namespace Lesson01.BasicPrompting.Features.Models.Execute;

public sealed record AiResponse(
	string Text,
	string Model,
	TimeSpan Duration);