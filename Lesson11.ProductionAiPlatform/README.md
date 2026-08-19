# Lesson10.MonitoringAndAnomalyDetection

## Monitoring and Anomaly Investigation with an AI Agent

Lesson10 separates two responsibilities:

1. **deterministic code detects that something is unusual**;
2. **an AI agent investigates what may explain it**.

The LLM does not decide whether a metric is statistically anomalous. `RollingZScoreDetector` does that first. Only when anomaly candidates exist does the application invoke `AnomalyAnalysisAgent`.

The agent then decides what additional evidence it needs and can autonomously call monitoring tools.

```text
operational metrics
        ↓
RollingZScoreDetector
        ↓
AnomalyCandidate[]
        ↓
AnomalyAnalysisAgent
        │
        ├── get_metric_history
        ├── get_recent_operational_events
        └── get_deployment_details
        ↓
MonitoringAssessment
```

The main design boundary is:

```text
detect                 → deterministic/statistical code
decide what to inspect → agent/LLM
retrieve evidence      → bounded application tools
correlate and explain  → LLM
act                     → human or controlled workflow
```

---

## Learning Goals

This lesson demonstrates:

- why detection and investigation are different jobs;
- why numerical anomaly detection usually belongs in deterministic code;
- how to invoke an LLM only when an anomaly deserves investigation;
- how an agent can choose its own evidence-gathering tools;
- how application code constrains agent-selected tool arguments;
- how one investigation can correlate multiple anomalous metrics;
- how structured output provides a predictable result contract;
- how the same agent architecture can run against different AI providers.

---

## Sample Monitoring Data

`MonitoringDataSource` contains **100 hourly observations for each metric**:

```text
documents_processed
average_processing_minutes
error_rate_percent
```

The first 99 observations represent stable operation. The final observation is intentionally abnormal:

```text
documents_processed          → 412
average_processing_minutes   → 12.6
error_rate_percent           → 7.8
```

The data source also contains operational events, including a deployment of version 4.8 twenty minutes before the anomalous observations.

Deployment details are available separately and include:

```text
Upgraded the document parser library
Increased queue-processing concurrency from 12 to 48
Changed the retry policy from 3 attempts to 1 attempt
```

The agent sees those details only if it chooses to call `get_deployment_details`.

---

## Phase 1 — Deterministic Detection

`MonitoringService` asks for 13 observations per metric:

```text
12 baseline observations
+ 1 current observation
= 13 observations
```

`RollingZScoreDetector` compares the latest value with the mean and standard deviation of the previous 12 values.

If the threshold is exceeded, it creates an `AnomalyCandidate` containing the metric, timestamp, current value, baseline mean, baseline standard deviation, and z-score.

If no candidates exist, the method returns without calling the LLM.

This keeps the repetitive numerical work cheap, predictable, testable, and reproducible.

---

## Phase 2 — Agent-Driven Investigation

When anomaly candidates exist, they are passed together to `AnomalyAnalysisAgent`.

The application does not automatically gather all supporting evidence first. The agent receives three capabilities:

```text
get_metric_history
get_recent_operational_events
get_deployment_details
```

The prompt does not prescribe a fixed workflow. The agent decides what evidence is useful.

A representative investigation might be:

```text
receive anomaly candidates
        ↓
request longer metric history
        ↓
notice several metrics changed together
        ↓
request recent operational events
        ↓
notice deployment 4.8 nearby
        ↓
request deployment details
        ↓
correlate evidence and form hypotheses
        ↓
return MonitoringAssessment
```

This demonstrates the agent loop:

```text
reason → retrieve → reason → retrieve → conclude
```

---

## Why Metric History Is Read Twice

Both the deterministic detector and the agent may request metric history, but they are answering different questions.

The detector asks:

> Is the latest observation unusual enough to investigate?

The agent asks:

> What historical evidence would help explain the anomaly?

The duplication is intentional because it keeps detection and investigation separate.

The sample now contains 100 observations, so an agent request for 48 or 100 points can return substantially more context than the detector's 13-point window.

---

## Tool Boundaries

The model chooses tool arguments, but application code limits them.

For example:

```csharp
points = Math.Clamp(points, 1, 168);
hours = Math.Clamp(hours, 1, 168);
```

The agent can decide that it wants a wider history window, but it cannot request an unbounded amount of data.

This is an important production principle:

> Agent autonomy operates inside boundaries established by application code.

---

## Tool-Call Diagnostics

`MonitoringDataSource` currently writes simple console messages whenever its methods are called.

For example:

```text
*** GET METRIC HISTORY CALLED: documents_processed 13 ***
*** GET RECENT EVENTS CALLED: 48 ***
*** GET METRIC HISTORY CALLED: documents_processed 48 ***
*** GET DEPLOYMENT DETAILS CALLED: 4.8 ***
```

The 13-point metric-history calls are made directly by `MonitoringService` for deterministic detection. They are not agent tool calls.

Later wider history requests, event lookups, and deployment-detail requests may be agent-selected calls.

---

## Structured Output

The agent returns a strongly typed result:

```csharp
public sealed record MonitoringAssessment(
    string Severity,
    string Summary,
    string[] Correlations,
    RelevantOperationalEvent[] RelevantEvents,
    string[] PossibleCauses,
    string[] RecommendedChecks);
```

The investigation path is flexible, while the application result shape remains predictable.

The instructions also tell the model to distinguish observations from hypotheses and to treat temporal proximity as correlation rather than proof of causation.

---

## Provider Abstraction

Lesson10 includes both `OllamaProvider` and `OpenAiProvider` behind the existing `IAiProvider` abstraction.

```text
Monitoring.Provider
        ↓
IAiProviderFactory
        ↓
IAiProvider
        ↓
IChatClient
        ↓
ChatClientAgent
```

The monitoring provider is selected in `appsettings.json`:

```json
"Monitoring": {
  "Provider": "openai"
}
```

The agent itself contains no OpenAI-specific branching logic.

### Why OpenAI is the monitoring default

During development, the current Ollama/Qwen configuration did not reliably emit tool calls when tool calling and strongly typed structured output were requested together. Without the structured-output requirement, it did call the tool.

With OpenAI, the same agent architecture successfully performed autonomous tool calls while returning the typed `MonitoringAssessment`.

This is a useful example of why a provider abstraction matters: application architecture can remain stable even when provider/model capabilities differ.

---

## OpenAI Configuration

The model is configured in `appsettings.json`:

```json
"OpenAI": {
  "Model": "gpt-5.2"
}
```

The API key is not stored in the repository. Set it as an environment variable:

```bash
export OPENAI_AI_BUSINESS_PLAYGROUND="your-api-key"
```

On macOS this can be placed in `~/.zshrc` and loaded with:

```bash
source ~/.zshrc
```

---

## Existing Lesson Capabilities

Lesson10 remains a snapshot of the application and preserves earlier capabilities such as conversations, Agent Framework sessions, property MCP tools, RAG, and safe property-review proposals.

The inherited RAG implementation still uses Ollama embeddings, so Ollama remains part of application startup even when monitoring uses OpenAI.

---

## Running the Lesson

Build the Lesson05 MCP server first:

```bash
dotnet build Lesson05.McpFundamentals/Lesson05.McpFundamentals.csproj
```

Make sure Ollama is running, set the OpenAI key, and then run Lesson10 from the repository root:

```bash
ASPNETCORE_URLS=http://localhost:5000 \
dotnet run --project Lesson10.MonitoringAndAnomalyDetection
```

Run a monitoring scan:

```bash
curl -s \
  http://localhost:5000/api/monitoring/scan \
  | jq .
```

The deterministic detector should identify the intentionally abnormal final observations and invoke the anomaly-analysis agent.

The exact wording and tool-call sequence may vary because the model chooses how to investigate.

---

## What to Observe

There are two distinct behaviors to watch.

### Deterministic behavior

```text
metric history
    ↓
RollingZScoreDetector
    ↓
AnomalyCandidate
```

The application always performs the short history reads required for detection.

### Agent behavior

Once candidates exist, the model can independently decide to:

- inspect longer history windows;
- retrieve recent operational events;
- ignore irrelevant events;
- inspect a temporally relevant deployment;
- correlate changes across multiple metrics;
- recommend human diagnostic checks.

The model is given capabilities and investigative guidance, not a hard-coded workflow.

---

## Simplifications

This is a teaching sample rather than a production monitoring platform.

It uses:

- in-memory synthetic telemetry;
- only three metrics;
- hourly observations;
- a simple rolling z-score detector;
- synthetic operational events and deployment details;
- read-only agent tools;
- model-selected investigation paths that can vary between runs.

A production system could replace `MonitoringDataSource` with real telemetry, logging, deployment, and incident systems while preserving the same boundary between deterministic detection and agent-driven investigation.

`RollingZScoreDetector` is also deliberately simple. Real systems may use seasonal baselines, robust statistics, change-point detection, forecasting, or service-specific thresholds.

---

## Key Takeaway

Lesson10 demonstrates a practical division of responsibility:

```text
Deterministic code
    identifies what deserves attention

Agent
    decides what evidence it needs

Tools
    provide bounded access to evidence

LLM
    correlates observations and forms hypotheses

Structured output
    returns a predictable application contract
```

The important idea is not that an LLM can recognize a large number. It is that deterministic monitoring can identify an unusual condition and then hand that condition to an agent that autonomously investigates the surrounding evidence before producing a useful structured assessment.
