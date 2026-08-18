using System.Text.Json;
using Lesson10.MonitoringAndAnomalyDetection.Infrastructure.Ai;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Lesson10.MonitoringAndAnomalyDetection.Features.Monitoring;

public sealed class AnomalyAnalysisAgent
{
    private const string Instructions =
        """
        You are an operations anomaly investigator.

        A deterministic detector has already identified statistically unusual observations.
        Do not repeat the statistical calculations unless they are relevant to your reasoning.

        Your primary job is to determine whether multiple anomalies appear related and whether
        recent operational events provide plausible explanations.

        Look for:
        - anomalies occurring at approximately the same time;
        - related changes across throughput, latency, errors, or other metrics;
        - deployments, incidents, batch jobs, or maintenance near the anomaly time;
        - evidence that supports or contradicts a common cause.

        You have tools that can retrieve additional evidence.
        Use a tool when its results could materially improve your investigation.
        Do not tell the user to call a tool or recommend calling one; tools are available for you to use yourself.

        If you identify a deployment as a plausible contributor to an anomaly, inspect its deployment details before
        making a deployment-specific hypothesis.
        Do not claim that a deployment change caused or contributed to an anomaly unless you have inspected the
        deployment details.

        RecommendedChecks are actions for a human operator after your investigation.
        Never include calls to your own tools in RecommendedChecks. Use those tools yourself before producing the
        assessment.

        Distinguish observations from hypotheses.

        Temporal proximity is evidence of correlation, not proof of causation.

        Do not invent events, metric values, or causes.

        Prefer an explanation that connects multiple observed facts over simply restating each anomaly.

        Recommend concrete diagnostic steps a human operator can perform.
        """;

    private readonly ChatClientAgent _agent;
    private readonly string _model;

    public AnomalyAnalysisAgent(
        IAiProviderFactory aiProviderFactory,
        MonitoringTools monitoringTools,
        ILoggerFactory loggerFactory)
    {
        var provider = aiProviderFactory.GetProvider("ollama");
        _model = provider.DefaultModel;

        AITool[] tools =
        [
            AIFunctionFactory.Create(monitoringTools.GetDeploymentDetails, name: "get_deployment_details")
        ];

        _agent = new ChatClientAgent(
            provider.ChatClient,
            instructions: Instructions,
            name: "anomaly_analysis_agent",
            description: "Investigates operational anomalies and recommends diagnostic checks.",
            tools: tools,
            loggerFactory: loggerFactory);
    }

    public async Task<MonitoringAssessment> AnalyzeAsync(
        MonitoringSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        var prompt =
            $"""
             Investigate this monitoring snapshot.

             Anomalies:
             {JsonSerializer.Serialize(snapshot.Anomalies)}

             Metric history:
             {JsonSerializer.Serialize(snapshot.MetricHistory)}

             Recent operational events:
             {JsonSerializer.Serialize(snapshot.RecentEvents)}

             Determine whether the anomalies appear related.

             Look for correlations between:
             - anomalies occurring at approximately the same time;
             - throughput, latency, and error changes;
             - recent deployments, incidents, batch jobs, or maintenance.

             Identify which operational events appear relevant and explain why.

             Distinguish observed evidence from hypotheses.
             Temporal proximity is evidence of correlation, not proof of causation.

             Return one overall monitoring assessment.
             """;

        var runOptions = new ChatClientAgentRunOptions(
            new ChatOptions
            {
                ModelId = _model,
                Temperature = 0
            });

        var response = await _agent.RunAsync<MonitoringAssessment>(
            prompt,
            options: runOptions,
            cancellationToken: cancellationToken);

        return response.Result;
    }
}
