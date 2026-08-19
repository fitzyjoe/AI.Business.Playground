using System.Text.Json;
using Lesson11.ProductionAiPlatform.Infrastructure.Ai;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace Lesson11.ProductionAiPlatform.Features.Monitoring;

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
        IOptions<MonitoringOptions> options,
        MonitoringTools monitoringTools,
        ILoggerFactory loggerFactory)
    {
        var provider = aiProviderFactory.GetProvider(options.Value.Provider);
        _model = provider.DefaultModel;

        AITool[] tools =
        [
            AIFunctionFactory.Create(monitoringTools.GetMetricHistory, name: "get_metric_history"),
            AIFunctionFactory.Create(monitoringTools.GetRecentOperationalEvents, name: "get_recent_operational_events"),
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
        IReadOnlyList<AnomalyCandidate> anomalies,
        CancellationToken cancellationToken = default)
    {
        var prompt =
            $"""
             Investigate these statistically detected anomalies.

             Anomalies:
             {JsonSerializer.Serialize(anomalies)}

             Determine whether the anomalies appear related and identify plausible explanations.

             Use your available tools to gather whatever additional evidence is useful.

             Consider:
             - whether affected metrics changed at approximately the same time;
             - whether their recent history suggests a shared event;
             - whether deployments, incidents, batch jobs, or maintenance occurred nearby;
             - whether details of a relevant deployment provide a plausible mechanism.

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
