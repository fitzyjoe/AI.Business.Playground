# Lesson10.MonitoringAndAnomalyDetection

## Monitoring and Anomaly Investigation with an AI Agent

Lesson10 separates two responsibilities:

1. deterministic code detects that something is unusual;
2. an AI agent investigates what may explain it.

The LLM does not decide whether a metric is statistically anomalous. `RollingZScoreDetector` does that first. Only when anomaly candidates exist does the application invoke `AnomalyAnalysisAgent`.

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

The main boundary is:

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
- how structured output provides a predictable result contract;
- how the same agent architecture can run against Ollama or OpenAI;
- how chat/agent provider choice remains separate from the RAG embedding provider.

---

## Provider Story in Lesson10

By Lesson10 there are three distinct provider selections in the application:

```text
Conversation.Provider
    → provider for user-facing agent conversations

Monitoring.Provider
    → provider used by AnomalyAnalysisAgent

Rag.EmbeddingProvider
    → provider used to embed/search internal knowledge
```

They are related through common provider concepts, but they serve different workloads and do not need to have the same value.

For example:

```text
Conversation chat:   ollama
Monitoring agent:    openai
Knowledge embeddings: openai
```

is a valid configuration.

---

## Sample Monitoring Data

`MonitoringDataSource` contains 100 hourly observations for each sample metric:

```text
documents_processed
average_processing_minutes
error_rate_percent
```

The final observation is intentionally abnormal. The data source also includes recent operational events and deployment details so the agent has evidence it can choose to inspect.

---

## Phase 1 — Deterministic Detection

`MonitoringService` obtains a small recent window and asks `RollingZScoreDetector` to compare the latest observation with the previous baseline observations.

If the threshold is not exceeded:

```text
no anomaly candidates
    ↓
no LLM call
```

If candidates exist:

```text
AnomalyCandidate[]
    ↓
AnomalyAnalysisAgent
```

This keeps repetitive numerical detection cheap, predictable, testable, and reproducible.

---

## Phase 2 — Agent-Driven Investigation

The agent receives bounded tools:

```text
get_metric_history
get_recent_operational_events
get_deployment_details
```

A representative flow is:

```text
receive anomaly candidates
    ↓
request wider metric history
    ↓
notice several metrics changed together
    ↓
request recent operational events
    ↓
notice nearby deployment
    ↓
request deployment details
    ↓
return MonitoringAssessment
```

The prompt does not prescribe an exact tool order. The agent decides what evidence is useful.

---

## Bounded Agent Autonomy

The model proposes tool arguments, but application code constrains them.

For example, history windows are clamped to bounded ranges.

The principle is:

> **Agent autonomy operates inside boundaries established by application code.**

---

## Chat Providers

Both Ollama and OpenAI are registered `IAiProvider` implementations.

`AiProviderFactory` discovers them by `Name`, as introduced in Lesson09.

`MonitoringOptions.Provider` chooses which provider backs `AnomalyAnalysisAgent`.

Example:

```json
"Monitoring": {
  "Provider": "openai"
}
```

Changing that setting does not change deterministic anomaly detection. It only changes the model used for the investigation phase.

---

## RAG Embedding Provider

Lesson10 preserves the independent embedding-provider choice introduced in Lesson07:

```json
"Rag": {
  "EmbeddingProvider": "ollama",
  "EmbeddingModel": "embeddinggemma",
  "EmbeddingDimensions": 768,
  "TopResults": 3
}
```

or, for example:

```json
"Rag": {
  "EmbeddingProvider": "openai",
  "EmbeddingModel": "text-embedding-3-small",
  "EmbeddingDimensions": 768,
  "TopResults": 3
}
```

The vector store resolves the configured embedding generator independently from the chat/monitoring providers.

Changing embedding provider/model/dimensions means the index should be rebuilt. This lesson's in-memory knowledge index is regenerated at startup.

---

## Configuration

A representative configuration is:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "qwen3:8b"
  },
  "OpenAI": {
    "Model": "gpt-5.2"
  },
  "Rag": {
    "EmbeddingProvider": "ollama",
    "EmbeddingModel": "embeddinggemma",
    "EmbeddingDimensions": 768,
    "TopResults": 3
  },
  "Monitoring": {
    "Provider": "openai"
  }
}
```

Set the OpenAI key whenever an OpenAI chat or embedding workload will be used:

```bash
export OPENAI_AI_BUSINESS_PLAYGROUND="your-api-key"
```

---

## Project Structure

```text
Lesson10.MonitoringAndAnomalyDetection/
├── Features/
│   ├── Agents/
│   ├── Conversations/
│   ├── Knowledge/
│   │   ├── KnowledgeRetriever.cs
│   │   ├── KnowledgeTools.cs
│   │   └── RagOptions.cs
│   ├── Monitoring/
│   │   ├── AnomalyAnalysisAgent.cs
│   │   ├── MonitoringDataSource.cs
│   │   ├── MonitoringService.cs
│   │   ├── MonitoringTools.cs
│   │   ├── RollingZScoreDetector.cs
│   │   └── ...
│   └── PropertyReviews/
├── Infrastructure/
│   ├── Ai/
│   │   └── Providers/
│   │       ├── OllamaProvider.cs
│   │       └── OpenAiProvider.cs
│   ├── ErrorHandling/
│   └── Mcp/
├── Knowledge/
├── Program.cs
├── appsettings.json
└── README.md
```

---

## Structured Investigation Result

The agent returns a strongly typed `MonitoringAssessment`, containing fields such as:

```text
Severity
Summary
Correlations
RelevantEvents
PossibleCauses
RecommendedChecks
```

Structured output makes the investigation useful to application code rather than only as prose.

---

## Running Lesson10

```bash
dotnet run --project Lesson10.MonitoringAndAnomalyDetection
```

Use the lesson's monitoring endpoint to trigger the deterministic detector and, when anomalies are found, the agent investigation.

Console output from `MonitoringDataSource` helps distinguish deterministic data reads from later agent-selected tool calls.

---

## Evaluation Mindset

Do not evaluate the agent by requiring an exact tool sequence or exact wording.

Prefer outcome checks such as:

```text
Did deterministic detection find the planted anomaly?
Did the agent inspect evidence relevant to the anomaly?
Did it identify the nearby deployment as relevant when supported by evidence?
Did it avoid inventing events or metrics?
Did it return a valid MonitoringAssessment?
```

Different providers may follow different reasonable investigation paths.

---

## Deliberately Out of Scope

Lesson10 does not yet add the production controls introduced later, such as:

- authentication and authorization;
- global provider allowlists;
- bounded provider concurrency;
- provider-call timeouts;
- production telemetry;
- production evaluation suites;
- cost budgets;
- durable monitoring history.

---

## Lesson10 Acceptance Criteria

```text
✓ deterministic code detects anomaly candidates before any LLM call
✓ no LLM investigation occurs when no anomaly exists
✓ AnomalyAnalysisAgent can use bounded monitoring tools
✓ Monitoring.Provider can select Ollama or OpenAI
✓ user-facing conversations can independently select Ollama or OpenAI
✓ Rag.EmbeddingProvider can independently select Ollama or OpenAI
✓ structured MonitoringAssessment is returned
✓ provider choice does not alter the deterministic detection boundary
✓ tool autonomy remains bounded by application code
```

---

## What Lesson10 Is Really Teaching

> **Use deterministic code to detect that something is wrong, then use a bounded AI agent to investigate why—without coupling that workflow to one model provider or one embedding provider.**
