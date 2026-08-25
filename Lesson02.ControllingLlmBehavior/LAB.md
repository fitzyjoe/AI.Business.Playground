# Lesson02 Lab — Controlling LLM Behavior

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add one request-level control, `MaxTokens`, all the way through the application abstraction and both AI providers.

## Predict

1. Which project type should own the provider-neutral concept of a maximum output size?
2. Where should Ollama-specific and OpenAI-specific mappings live?
3. What should happen when `MaxTokens` is omitted?
4. Why is a token limit a control rather than a guarantee about exact response length?

## Run the Starter

Run Lesson02 and send the same prompt through Ollama and OpenAI. Observe that the starter still supports the other controls but intentionally leaves `MaxTokens` incomplete.

```bash
dotnet run --project Lesson02.ControllingLlmBehavior
```

## Build — Add `MaxTokens`

Implement `MaxTokens` end to end:

- expose it on the request contract;
- carry it through the application-level AI request;
- map it in `OllamaProvider`;
- map it in `OpenAiProvider`;
- preserve existing defaults when the caller omits it.

Do not introduce provider SDK types into the feature layer.

## Run — Compare Behavior

Call each provider with the same prompt and a small output limit, then repeat with a larger limit. Compare the returned text and duration.

Also verify that requests without `maxTokens` still work.

## Attack

Try:

- `maxTokens = 1`;
- a very large positive value;
- an unsupported provider;
- the same prompt with low and high temperature.

Record which behaviors are enforced by normal application validation and which remain probabilistic model behavior.

## Explain

1. Why should `MaxTokens` exist once at the application boundary and be translated separately by each provider?
2. Why doesn't a token limit guarantee a specific number of words?
3. What part of the application changes if a third provider is added?

## Lab Completion Criteria

```text
✓ MaxTokens is provider-neutral at the feature boundary
✓ Ollama maps the control
✓ OpenAI maps the control
✓ omitted MaxTokens preserves normal behavior
✓ existing provider selection and other controls still work
```
