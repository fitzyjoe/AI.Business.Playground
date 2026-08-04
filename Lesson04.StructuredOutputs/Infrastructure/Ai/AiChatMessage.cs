namespace Lesson04.StructuredOutputs.Infrastructure.Ai;

public sealed record AiChatMessage
{
	public required AiMessageRole Role { get; init; }

	public required string Content { get; init; }
}