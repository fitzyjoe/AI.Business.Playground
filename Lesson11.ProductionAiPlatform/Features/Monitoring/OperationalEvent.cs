namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

public sealed record OperationalEvent(
	DateTimeOffset Timestamp,
	string Type,
	string Description);