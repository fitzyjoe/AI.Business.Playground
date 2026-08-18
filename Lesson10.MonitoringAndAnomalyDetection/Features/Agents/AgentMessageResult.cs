namespace Lesson10.MonitoringAndAnomalyDetection.Features.Agents;

public sealed record AgentMessageResult(
	string Text,
	string Model,
	TimeSpan Duration);