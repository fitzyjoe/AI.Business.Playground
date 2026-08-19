namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

public sealed record RelevantOperationalEvent(
	DateTimeOffset Timestamp,
	string Type,
	string Description,
	string Relevance);