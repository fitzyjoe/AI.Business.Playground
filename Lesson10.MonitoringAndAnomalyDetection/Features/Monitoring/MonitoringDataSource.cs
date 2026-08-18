namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed class MonitoringDataSource
{
    private const int ObservationCount = 100;

    private static readonly double[] StableOffsets =
    [
        -1.0,
        -0.5,
        0.1,
        0.7,
        1.0,
        0.4,
        -0.8,
        0.2,
        0.9,
        -0.3,
        -0.6,
        0.5
    ];

    private readonly Dictionary<string, IReadOnlyList<MetricObservation>> _observations;
    private readonly IReadOnlyList<OperationalEvent> _events;
    private readonly IReadOnlyList<DeploymentDetails> _deployments;

    public MonitoringDataSource()
    {
        var now = DateTimeOffset.UtcNow;

        _observations = new Dictionary<string, IReadOnlyList<MetricObservation>>(StringComparer.OrdinalIgnoreCase)
        {
            ["documents_processed"] = BuildSeries("documents_processed", now, ObservationCount, 1005, 20, 412),
            ["average_processing_minutes"] = BuildSeries("average_processing_minutes", now, ObservationCount, 5.0, 0.3, 12.6),
            ["error_rate_percent"] = BuildSeries("error_rate_percent", now, ObservationCount, 1.0, 0.2, 7.8)
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
        Console.WriteLine($"*** GET METRIC HISTORY CALLED: {metric} {points} ***");

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
        Console.WriteLine($"*** GET RECENT EVENTS CALLED: {hours} ***");

        var cutoff = DateTimeOffset.UtcNow.AddHours(-hours);

        return _events
            .Where(item => item.Timestamp >= cutoff)
            .OrderByDescending(item => item.Timestamp)
            .ToArray();
    }

    public DeploymentDetails? GetDeploymentDetails(string version)
    {
        Console.WriteLine($"*** GET DEPLOYMENT DETAILS CALLED: {version} ***");

        return _deployments.FirstOrDefault(
            deployment => string.Equals(
                deployment.Version,
                version,
                StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<MetricObservation> BuildSeries(
        string metric,
        DateTimeOffset now,
        int count,
        double baseline,
        double variation,
        double anomalyValue)
    {
        if (count < 2)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "At least two observations are required.");
        }

        var values = Enumerable
            .Range(0, count - 1)
            .Select(index => baseline + StableOffsets[index % StableOffsets.Length] * variation)
            .Append(anomalyValue)
            .ToArray();

        return values
            .Select(
                (value, index) =>
                    new MetricObservation(
                        metric,
                        now.AddHours(index - values.Length + 1),
                        value))
            .ToArray();
    }
}
