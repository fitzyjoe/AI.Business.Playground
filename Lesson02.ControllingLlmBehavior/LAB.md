# Lesson02 Lab — Controlling LLM Behavior

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add one request-level control, `MaxTokens`, all the way through the application abstraction and both AI providers.

## Predict

1. Which layer should own the provider-neutral concept of a maximum output size, and which layer should translate it into provider-specific settings?
2. Where should Ollama-specific and OpenAI-specific mappings live?
3. What should happen when `MaxTokens` is omitted?
4. Why is a token limit a control rather than a guarantee about exact response length?

## Run the Starter

Run Lesson02:

```bash
dotnet run --project Lesson02.ControllingLlmBehavior
```

Send the same basic prompt through Ollama and OpenAI using the examples in [README.md](README.md). Observe that the starter still supports the other controls but intentionally leaves `MaxTokens` incomplete.

## Build — Add `MaxTokens`

Implement `MaxTokens` end to end:

- expose it on the request contract;
- carry it through the application-level AI request;
- map it in `OllamaProvider`;
- map it in `OpenAiProvider`;
- preserve existing defaults when the caller omits it.

Do not introduce provider SDK types into the feature layer.

## Run — Compare Providers

Send the same prompt, system prompt, temperature, and output limit to both providers.

Ollama:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain dependency injection in one paragraph.",
    "systemPrompt": "You are a concise technical instructor.",
    "temperature": 0.2,
    "provider": "ollama",
    "maxTokens": 120
  }'
```

OpenAI:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain dependency injection in one paragraph.",
    "systemPrompt": "You are a concise technical instructor.",
    "temperature": 0.2,
    "provider": "openai",
    "maxTokens": 120
  }'
```

Compare:

```text
response quality
latency
writing style
instruction following
response length
```

The goal is not to prove one provider is better. The goal is to see that the feature can remain stable while the provider implementation changes.

## Run — Compare Output Limits

Repeat a request first with a small output limit and then with a larger one. Also verify that requests without `maxTokens` still work.

For example:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain the major differences between REST, GraphQL, and gRPC.",
    "provider": "ollama",
    "maxTokens": 40
  }'
```

Then rerun with a larger value such as `400`.

## Run — Compare Temperatures

Run the same creative request several times at `0.0`, then at a higher value such as `1.0`.

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Give me three names for a software consulting company.",
    "provider": "ollama",
    "temperature": 0.0
  }'
```

Then change only `temperature` to `1.0` and compare the responses.

## Run — Model Override

Try another model supported by the selected provider without changing `appsettings.json`:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain dependency injection in one paragraph.",
    "provider": "ollama",
    "model": "qwen3:8b"
  }'
```

Use a model that is actually installed for the selected provider.

## Attack — Unsupported Provider

```bash
curl -i \
  -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Hello",
    "provider": "unknown"
  }'
```

The factory should reject the request because only `ollama` and `openai` are supported.

Also try:

- `maxTokens = 1`;
- a very large positive `maxTokens` value;
- the same prompt with low and high temperature.

Record which behaviors are deterministic application behavior and which remain probabilistic model behavior.

## Explain

1. Why should `MaxTokens` exist once at the application boundary and be translated separately by each provider?
2. Why doesn't a token limit guarantee a specific number of words?
3. What part of the application changes if a third provider is added?
4. Why can two providers produce different results while still honoring the same application-level request contract?

## Lab Completion Criteria

```text
✓ MaxTokens is provider-neutral at the feature boundary
✓ Ollama maps the control
✓ OpenAI maps the control
✓ omitted MaxTokens preserves normal behavior
✓ provider comparison remains possible through one application contract
✓ temperature and model override experiments still work
✓ unsupported providers are rejected
```
