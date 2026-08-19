namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

public sealed record DeploymentDetails(
	string Version,
	DateTimeOffset DeployedAt,
	string Service,
	string[] Changes);