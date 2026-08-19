namespace Lesson11.ProductionAiPlatform.Infrastructure.Ai;

public sealed class AiOptions
{
	public const string SectionName = "AiOptions";

	public string DefaultProvider { get; init; } = "openai";

	public string[] AllowedProviders { get; init; } = [];

	public int MaxInputCharacters { get; init; } = 8_000;

	public int DefaultMaxOutputTokens { get; init; } = 600;

	public int MaxOutputTokens { get; init; } = 1_200;

	public float MaxTemperature { get; init; } = 1.0f;

	public int AgentRequestTimeoutSeconds { get; init; } = 90;

	public int ProviderCallTimeoutSeconds { get; init; } = 45;

	public int MaxConcurrentCallsPerProvider { get; init; } = 4;
}