using System.Text.Json;

namespace Lesson11.ProductionAiPlatform.Features.Conversations;

public class Conversation
{
	public Guid Id { get; init; } = Guid.NewGuid();

	public required string OwnerId { get; init; }

	public string Provider { get; init; } = "openai";

	public float? Temperature { get; init; }

	public int? MaxTokens { get; init; }

	public JsonElement? AgentSessionState { get; set; }

	public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

	public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}