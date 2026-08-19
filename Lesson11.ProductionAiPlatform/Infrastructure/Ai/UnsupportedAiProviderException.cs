namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public sealed class UnsupportedAiProviderException(string provider) : NotSupportedException($"AI provider '{provider}' is not supported.")
{
	public string Provider { get; } = provider;
}