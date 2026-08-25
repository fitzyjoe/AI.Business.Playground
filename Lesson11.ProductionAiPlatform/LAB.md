# Lesson11 Lab — Production AI Platform

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add one production control and prove it twice: once with a deterministic software test and once with a live AI evaluation.

## Predict

1. Which controls in Lesson11 must remain deterministic even though an LLM is involved?
2. Why is tool selection not authorization?
3. Why do deterministic tests and AI evaluations answer different questions?
4. What can authorization prevent during prompt injection, and what can it not prevent for an already-authorized Reviewer?

## Run

Run the deterministic test project first:

```bash
dotnet test Lesson11.ProductionAiPlatform.Tests/Lesson11.ProductionAiPlatform.Tests.csproj
```

Then exercise the Reader and Reviewer identities against the running Lesson11 application. Confirm the existing provider policy, request limits, proposal authorization, and separate approval boundary.

## Build — Add `MaxConversationTurns`

Add a production control named `MaxConversationTurns` to `AiOptions`.

Requirements:

- configure a sensible default in `appsettings.json`;
- validate the configured value at startup;
- enforce the limit in application code before another agent turn is executed;
- derive the current turn count from application-owned conversation/session state rather than trusting a caller-supplied value;
- return a clear application-level policy failure when the limit is reached;
- do not implement the limit as a system-prompt instruction.

## Test — Deterministic Proof

Add deterministic tests proving at minimum:

- valid configuration is accepted;
- non-positive configuration is rejected;
- a conversation below the limit can continue;
- a conversation at the limit is blocked without calling the model.

The test should prove an application invariant, not generated wording.

## Evaluate — Live AI Proof

Add a live AI evaluation that starts a conversation, continues it until the configured limit is reached, and verifies that one additional turn is refused by application policy.

Keep live model tests guarded by:

```text
RUN_AI_EVALUATIONS=true
```

The evaluation should assert the observable outcome, not an exact model response.

## Attack

Try to bypass the limit by:

- asking the model to ignore it;
- claiming the user is authorized for unlimited turns;
- placing similar instructions in retrieved knowledge;
- switching provider on a new conversation.

The limit should remain application-owned regardless of prompt content or provider choice.

## Explain

1. Why is `MaxConversationTurns` stronger as application policy than as a system prompt?
2. Why should the model never be asked to decide whether its own resource limit applies?
3. What does the deterministic test prove that the AI evaluation does not?
4. What does the AI evaluation prove that the deterministic test does not?
5. How does this control reinforce the lesson's central principle?

## Lab Completion Criteria

```text
✓ MaxConversationTurns is configured and startup-validated
✓ application enforces it before an extra agent turn
✓ deterministic tests cover the invariant
✓ live AI evaluation covers end-to-end behavior
✓ prompt content cannot bypass the control
✓ provider choice does not bypass the control
```

> **The LLM can decide what it wants to do. The application decides what it is allowed to do.**
