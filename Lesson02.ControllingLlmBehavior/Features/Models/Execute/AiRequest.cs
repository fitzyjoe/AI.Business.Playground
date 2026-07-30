namespace Lesson02.ControllingLlmBehavior.Features.Models.Execute;

public sealed class AiRequest
{
	public required string Prompt { get; init; }

	public string? SystemPrompt { get; init; }

	public float Temperature { get; init; } = 0.2f;

	public string Provider { get; init; } = "ollama";
	
	public string? Model { get; init; }

	public int? MaxTokens { get; init; }
}