namespace Lesson01.Chat.Features.Models.Execute;

public sealed class AiRequest
{
	public required string Prompt { get; init; }

	public string? SystemPrompt { get; init; }

	public float Temperature { get; init; } = 0.2f;

	public string Model { get; init; } = "ollama";

	public int? MaxTokens { get; init; }
}