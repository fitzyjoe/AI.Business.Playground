namespace Lesson10.MonitoringAndAnomalyDetection.Features.Knowledge;

public sealed class RagOptions
{
	public string EmbeddingProvider { get; init; } = "ollama";
	public required string EmbeddingModel { get; init; }
	public required int EmbeddingDimensions { get; init; }
	public int TopResults { get; init; } = 3;
}
