namespace Lesson09.Agents.Features.Agents;

public sealed record AgentMessageResult(
	string Text,
	string Model,
	TimeSpan Duration);