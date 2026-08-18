using System.Text.Json;

namespace Lesson10.MonitoringAndAnomalyDetection.Features.Conversations;

public class Conversation
{
	public Guid Id { get; init; } = Guid.NewGuid();
	public string? SystemPrompt { get; init; }
	public string Provider { get; init; } = "ollama";
	public string? Model { get; init; }
	public float? Temperature { get; init; }
	public int? MaxTokens { get; init; }
	public JsonElement? AgentSessionState { get; set; }
	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}