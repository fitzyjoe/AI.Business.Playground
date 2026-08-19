namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public sealed class AiRequestTimeoutException(string message) : Exception(message);