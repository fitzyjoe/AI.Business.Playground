namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

public sealed class RollingZScoreDetector
{
	public AnomalyCandidate? DetectLatest(IReadOnlyList<MetricObservation> observations, int baselinePoints, double threshold)
	{
		if (observations.Count < baselinePoints + 1)
		{
			return null;
		}

		var latest = observations[^1];

		var baseline = observations
			.Skip(observations.Count - baselinePoints - 1)
			.Take(baselinePoints)
			.Select(observation => observation.Value)
			.ToArray();

		var mean = baseline.Average();

		var variance = baseline
			.Select(value => Math.Pow(value - mean, 2))
			.Average();

		var standardDeviation = Math.Sqrt(variance);

		if (standardDeviation < 0.000001)
		{
			return null;
		}

		var zScore = (latest.Value - mean) / standardDeviation;

		if (Math.Abs(zScore) < threshold)
		{
			return null;
		}

		return new AnomalyCandidate(
			latest.Metric,
			latest.Timestamp,
			latest.Value,
			mean,
			standardDeviation,
			zScore);
	}
}