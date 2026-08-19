namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

public sealed class MonitoringService(
	MonitoringDataSource _dataSource,
	RollingZScoreDetector _detector,
	AnomalyAnalysisAgent _anomalyAnalysisAgent)
{
	private const int BaselinePoints = 12;
	private const double ZScoreThreshold = 3.0;

	public async Task<MonitoringAssessment?> ScanAsync(CancellationToken cancellationToken = default)
	{
		var candidates = new List<AnomalyCandidate>();

		foreach (var metric in _dataSource.MetricNames)
		{
			var observations = _dataSource.GetMetricHistory(
				metric,
				BaselinePoints + 1);

			var candidate = _detector.DetectLatest(
				observations,
				BaselinePoints,
				ZScoreThreshold);

			if (candidate is not null)
			{
				candidates.Add(candidate);
			}
		}

		if (candidates.Count == 0)
		{
			return null;
		}

		return await _anomalyAnalysisAgent.AnalyzeAsync(
			candidates,
			cancellationToken);
	}
}