namespace Lesson10.MonitoringAndAnomalyDetection.Features.Conversations;

public sealed record MessageResponse(
	Guid ConversationId,
	string Content,
	string Model,
	TimeSpan Duration);