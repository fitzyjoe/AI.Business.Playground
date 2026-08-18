using Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

namespace Lesson10.MonitoringAndAnomalyDetection.Infrastructure.Monitoring;

public sealed class MonitoringDataSource
{
    private readonly Dictionary<string, IReadOnlyList<MetricObservation>> _observations;
    private readonly IReadOnlyList<OperationalEvent> _events;
    private readonly IReadOnlyList<DeploymentDetails> _deployments;

    public MonitoringDataSource()
    {
        var now = DateTimeOffset.UtcNow;

        _observations = new Dictionary<string, IReadOnlyList<MetricObservation>>(StringComparer.OrdinalIgnoreCase)
        {
            ["documents_processed"] = BuildSeries("documents_processed", now, [1012, 995, 1008, 1021, 987, 1004, 1017, 992, 1009, 1024, 998, 1011, 412]),
            ["average_processing_minutes"] = BuildSeries("average_processing_minutes", now, [4.8, 5.1, 4.9, 5.0, 5.2, 4.7, 5.1, 4.9, 5.0, 4.8, 5.2, 4.9, 12.6]),
            ["error_rate_percent"] = BuildSeries("error_rate_percent", now, [1.1, 0.9, 1.0, 1.2, 0.8, 1.1, 0.9, 1.0, 1.1, 0.9, 1.0, 1.2, 7.8])
        };

        _events =
        [
            new OperationalEvent(
                now.AddMinutes(-20),
                "deployment",
                "Version 4.8 of the document-ingestion service was deployed."),

            new OperationalEvent(
                now.AddHours(-5),
                "batch-job",
                "The nightly customer export completed successfully."),

            new OperationalEvent(
                now.AddHours(-9),
                "maintenance",
                "Routine database index maintenance completed successfully.")
        ];
        
        _deployments =
        [
            new DeploymentDetails(
                "4.8",
                now.AddMinutes(-20),
                "document-ingestion",
                [
                    "Upgraded the document parser library.",
                    "Increased queue-processing concurrency from 12 to 48.",
                    "Changed the retry policy from 3 attempts to 1 attempt."
                ])
        ];
    }

    public IReadOnlyCollection<string> MetricNames => _observations.Keys;

    public IReadOnlyList<MetricObservation> GetMetricHistory(string metric, int points)
    {
        if (!_observations.TryGetValue(metric, out var observations))
        {
            return [];
        }

        return observations
            .TakeLast(points)
            .ToArray();
    }

    public IReadOnlyList<OperationalEvent> GetRecentEvents(int hours)
    {
        var cutoff = DateTimeOffset.UtcNow.AddHours(-hours);

        return _events
            .Where(item => item.Timestamp >= cutoff)
            .OrderByDescending(item => item.Timestamp)
            .ToArray();
    }
    
    public DeploymentDetails? GetDeploymentDetails(string version)
    {
        return _deployments.FirstOrDefault(
            deployment => string.Equals(
                deployment.Version,
                version,
                StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<MetricObservation> BuildSeries(string metric, DateTimeOffset now, IReadOnlyList<double> values)
    {
        return values
            .Select(
                (value, index) =>
                    new MetricObservation(
                        metric,
                        now.AddHours(index - values.Count + 1),
                        value))
            .ToArray();
    }
}