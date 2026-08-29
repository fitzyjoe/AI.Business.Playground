# Lesson10 Lab — Monitoring and Anomaly Detection

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add a new monitored metric and anomaly scenario so deterministic detection triggers an AI investigation only when the planted anomaly is present.

## Predict

1. Why should a z-score detector run before an LLM?
2. What evidence should the agent be allowed to retrieve after an anomaly is detected?
3. What should happen when no anomaly candidate exists?

## Run

Run the existing monitoring scan and trace the two phases: deterministic detection followed by bounded agent investigation.

Inspect the sample metric observations and events in `Features/Monitoring/*.json` to see how baseline and anomalous records are structured.

## Build — Add a New Anomaly Scenario

Add a metric named `failed_document_percent`:
1. Create `Features/Monitoring/failed_document_percent.json` with a realistic historical baseline and a final observation that can be made anomalous.
2. Register `"failed_document_percent.json"` in the `MetricFileNames` array in `MonitoringDataSource.cs`.
3. Add any supporting operational event in `operations_events.json` or deployment changes in `deployment_details.json` needed for a meaningful investigation.

Preserve the existing separation between:

```text
detection -> deterministic code
investigation -> agent
```

The detector, not the LLM, must decide whether the latest value crosses the anomaly threshold. When you modify JSON data files, rebuild or run the project so the updated files are copied to the build output.

## Run — Compare Normal and Anomalous Cases

Exercise two versions of the data:

1. latest value inside the expected range;
2. latest value clearly anomalous.

Verify that the normal case produces no AI investigation and the anomalous case produces a structured `MonitoringAssessment` grounded in available evidence.

## Attack

- Plant an anomaly with no nearby operational event.
- Plant a deployment event without a metric anomaly.
- Make several metrics move together.
- Ask whether the agent invents evidence that is not exposed by its tools.

## Explain

1. Why is anomaly detection cheaper and more testable as deterministic code?
2. What value does the agent add after detection?
3. Why are tool arguments bounded even when the agent chooses them?

## Lab Completion Criteria

```text
✓ new metric JSON file is created and registered in MetricFileNames
✓ deterministic detector identifies the planted anomaly
✓ no LLM call is needed for the normal case
✓ anomalous case invokes bounded investigation
✓ structured assessment uses available evidence without inventing events
```
