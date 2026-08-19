namespace Lesson11.ProductionAiPlatform.Features.Agents;

public sealed record AgentMessageResult(
	string Text,
	string Model,
	TimeSpan Duration);