using System.Text.Json;

namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed class MonitoringDataSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly string[] MetricFileNames =
    [
        "documents_processed.json",
        "average_processing_minutes.json",
        "error_rate_percent.json"
    ];

    private readonly Dictionary<string, IReadOnlyList<MetricObservation>> _observations;
    private readonly IReadOnlyList<OperationalEvent> _events;
    private readonly IReadOnlyList<DeploymentDetails> _deployments;

    public MonitoringDataSource()
    {
        var basePath = ResolveDataDirectory();

        var observations = new Dictionary<string, IReadOnlyList<MetricObservation>>(StringComparer.OrdinalIgnoreCase);

        foreach (var metricFileName in MetricFileNames)
        {
            var filePath = Path.Combine(basePath, metricFileName);
            if (File.Exists(filePath))
            {
                using var stream = File.OpenRead(filePath);
                var items = JsonSerializer.Deserialize<List<MetricObservation>>(stream, JsonOptions) ?? [];
                if (items.Count > 0)
                {
                    var metricName = items[0].Metric;
                    observations[metricName] = items.OrderBy(i => i.Timestamp).ToArray();
                }
            }
        }

        _observations = observations;

        var eventsPath = Path.Combine(basePath, "operations_events.json");
        if (File.Exists(eventsPath))
        {
            using var stream = File.OpenRead(eventsPath);
            _events = JsonSerializer.Deserialize<List<OperationalEvent>>(stream, JsonOptions)?
                .OrderByDescending(e => e.Timestamp)
                .ToArray() ?? [];
        }
        else
        {
            _events = [];
        }

        var deploymentsPath = Path.Combine(basePath, "deployment_details.json");
        if (File.Exists(deploymentsPath))
        {
            using var stream = File.OpenRead(deploymentsPath);
            _deployments = JsonSerializer.Deserialize<List<DeploymentDetails>>(stream, JsonOptions) ?? [];
        }
        else
        {
            _deployments = [];
        }
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
        if (_events.Count == 0)
        {
            return [];
        }

        var referenceTime = _events.Max(item => item.Timestamp);
        var cutoff = referenceTime.AddHours(-hours);

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

    private static string ResolveDataDirectory()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Features", "Monitoring"),
            Path.Combine(Directory.GetCurrentDirectory(), "Lesson10.MonitoringAndAnomalyDetection", "Features", "Monitoring"),
            Path.Combine(Directory.GetCurrentDirectory(), "Features", "Monitoring"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Features", "Monitoring")
        };

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "operations_events.json")))
            {
                return candidate;
            }
        }

        return Path.Combine(AppContext.BaseDirectory, "Features", "Monitoring");
    }
}
