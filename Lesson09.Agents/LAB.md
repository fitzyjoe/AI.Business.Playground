# Lesson09 Lab — Agents

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add a new bounded agent tool to `PropertyReviewAgent` and observe when the agent chooses to use it.

## Predict

1. What does `AgentSession` own that the application previously managed manually?
2. Which state still belongs to the application rather than the Agent Framework?
3. Why should a new agent tool be narrowly scoped rather than a generic "do anything" function?

## Run

Exercise the existing agent with an MCP-only question, a knowledge-only question, and a proposal request. Observe that the agent chooses among available capabilities.

## Build — Add a Bounded Tool

Add a tool called `summarize_property_review_status` that accepts a parcel number and returns a concise application-generated summary of any pending or executed property-review records for that parcel.

Requirements:

- expose a narrow, well-described tool contract;
- use existing application repositories/services rather than bypassing them;
- return bounded structured or concise data;
- make the tool available to `PropertyReviewAgent`;
- do not give the tool approval or execution authority.

## Run — Observe Agent Selection

Ask:

- a question that clearly requires the new status tool;
- a property-data question that should use MCP instead;
- a procedure question that should use knowledge retrieval instead;
- a combined question where the agent may reasonably use more than one tool.

Do not require an exact tool order; evaluate whether the outcome is appropriately grounded.

## Attack

Ask the agent to use the new status tool to approve, reject, or create a review. Verify that the tool's narrow contract prevents capability expansion.

## Explain

1. What makes this an agent rather than just a chat client with manually orchestrated calls?
2. Why does the application still own business authority even when the agent chooses tools?
3. Why should evaluation focus on outcomes rather than one exact tool sequence?

## Lab Completion Criteria

```text
✓ new bounded tool is available to PropertyReviewAgent
✓ agent chooses it for appropriate status questions
✓ unrelated questions continue to use more appropriate capabilities
✓ tool cannot approve or execute reviews
✓ AgentSession and application-owned business state remain distinct
```
