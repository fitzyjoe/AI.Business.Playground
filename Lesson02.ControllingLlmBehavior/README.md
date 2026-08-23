# Lesson02.ControllingLlmBehavior

## Controlling LLM Behavior

Lesson02 extends basic prompting by showing how an application can influence model behavior through explicit request-level controls rather than relying on the user prompt alone.

The application still sends a single prompt to an LLM, but the request can now also control:

```text
system prompt
temperature
provider
model
maximum output tokens
```

This lesson also introduces a second real AI provider. The same application-level request can be executed by either Ollama or OpenAI.

The key ideas are:

> **The application can separate what the user asks from how the model should behave while answering.**

and:

> **Feature code can select an AI provider without depending on provider-specific SDK types.**

---

## Learning Goals

By the end of Lesson02, you should understand:

- the difference between a user prompt and a system prompt;
- how temperature influences response variability;
- how maximum output tokens constrain response length;
- how an application can choose an AI provider independently from business logic;
- how a request can override a provider's configured default model;
- why provider-specific details belong behind an application abstraction;
- how the same application-level controls are translated into different provider APIs;
- why these controls influence model behavior but do not guarantee a particular response.

---

## Request Flow

```text
HTTP Client
    ↓
POST /api/prompt
    ↓
Endpoint
    ↓
Handler
    ↓
IAiProviderFactory
    ↓
IAiProvider
    ├── OllamaProvider → Ollama
    └── OpenAiProvider → OpenAI
```

The feature layer knows about application-level concepts such as `Prompt`, `SystemPrompt`, `Temperature`, `Provider`, `Model`, and `MaxTokens`.

Each provider is responsible for translating those concepts into its own API.

---

## Project Structure

```text
Lesson02.ControllingLlmBehavior/
├── Features/
│   └── Models/
│       └── Execute/
│           ├── AiRequest.cs
│           ├── AiResponse.cs
│           ├── Endpoint.cs
│           └── Handler.cs
├── Infrastructure/
│   └── Ai/
│       ├── AiProviderFactory.cs
│       ├── IAiProvider.cs
│       ├── IAiProviderFactory.cs
│       └── Providers/
│           ├── OllamaOptions.cs
│           ├── OllamaProvider.cs
│           ├── OpenAiOptions.cs
│           └── OpenAiProvider.cs
├── Lesson02.ControllingLlmBehavior.csproj
├── Program.cs
├── appsettings.json
└── README.md
```

---

## The HTTP API

Lesson02 exposes:

```http
POST /api/prompt
```

Example using Ollama:

```json
{
  "prompt": "Explain dependency injection in one paragraph.",
  "systemPrompt": "You are a concise technical instructor.",
  "temperature": 0.2,
  "provider": "ollama",
  "model": "gemma3:4b",
  "maxTokens": 200
}
```

Example using OpenAI:

```json
{
  "prompt": "Explain dependency injection in one paragraph.",
  "systemPrompt": "You are a concise technical instructor.",
  "temperature": 0.2,
  "provider": "openai",
  "maxTokens": 200
}
```

Only `prompt` is required. `Provider` defaults to `ollama`.

---

## Provider Selection

`Handler` asks `IAiProviderFactory` for the selected provider.

The factory supports:

```text
ollama
openai
```

Unknown provider names are rejected.

This is the first lesson where the provider abstraction has a concrete payoff:

```text
feature logic
    ↓
IAiProvider
    ↓
provider-specific implementation
```

The feature does not need to know whether the response came from OllamaSharp or the OpenAI SDK.

---

## Provider Configuration

`appsettings.json` contains defaults for both providers:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "gemma3:4b"
  },
  "OpenAI": {
    "Model": "gpt-5.2"
  }
}
```

To use OpenAI, set the API key in the environment:

```bash
export OPENAI_AI_BUSINESS_PLAYGROUND="your-api-key"
```

The OpenAI key is read when an OpenAI request is executed. Ollama-only use does not require the key.

---

## User Prompt vs. System Prompt

The user prompt is the task:

```text
Explain dependency injection.
```

The system prompt supplies higher-level behavioral guidance:

```text
You are a concise technical instructor.
```

Both providers receive those as distinct chat roles rather than concatenating them into one string.

---

## Temperature

`Temperature` defaults to `0.2`.

Lower values generally encourage more conservative, repeatable output. Higher values generally allow more variation.

The important architectural point is that `AiRequest` expresses temperature once while each provider maps it to its own SDK.

---

## Maximum Output Tokens

`MaxTokens` is optional and limits generated output.

The application-level concept is the same for both providers even though the underlying provider APIs differ.

A token limit is not an exact word or character limit.

---

## Model Selection

Each provider has a configured default model. A request may optionally override that model:

```text
request model if supplied
    ↓ otherwise
selected provider's configured default model
```

Model names are provider-specific. An Ollama model name should not be sent to OpenAI, and an OpenAI model name should not be sent to Ollama.

---

## Running the Lesson

```bash
dotnet run --project Lesson02.ControllingLlmBehavior
```

Examples below assume:

```text
http://localhost:5000
```

### Ollama

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain dependency injection in one paragraph.",
    "provider": "ollama"
  }'
```

### OpenAI

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain dependency injection in one paragraph.",
    "provider": "openai"
  }'
```

---

## Useful Experiments

### Compare Providers

Send the same prompt, system prompt, temperature, and output limit to both providers.

Compare:

```text
response quality
latency
writing style
instruction following
```

The goal is not to prove one provider is better. The goal is to see that the feature can remain stable while the implementation changes.

### Compare Temperatures

Run the same request several times at `0.0`, then at a higher value such as `1.0`.

### Model Override

Try another model supported by the selected provider without changing `appsettings.json`.

### Unsupported Provider

```json
{
  "prompt": "Hello",
  "provider": "unknown"
}
```

The factory should reject the request because only `ollama` and `openai` are supported.

---

## Control vs. Guarantee

Parameters such as system prompt, temperature, max tokens, and model influence model behavior.

They do not guarantee semantic correctness or exact formatting.

That distinction becomes important when later lessons introduce structured outputs, deterministic validation, tools, and authorization boundaries.

---

## Deliberately Out of Scope

Lesson02 does not add:

- multi-turn conversation state;
- persisted message history;
- structured JSON outputs;
- tool calling;
- MCP;
- RAG;
- agents;
- provider failover;
- authentication;
- production retry policies;
- streaming HTTP responses to the client.

---

## Lesson02 Acceptance Criteria

```text
✓ POST /api/prompt accepts a prompt
✓ an optional system prompt can influence model behavior
✓ temperature and maxTokens are passed through the provider abstraction
✓ provider selection goes through IAiProviderFactory
✓ both Ollama and OpenAI are supported
✓ unsupported providers are rejected
✓ each provider has a configured default model
✓ a request can override the selected provider's default model
✓ provider-specific SDK details stay inside Infrastructure/Ai
✓ the response includes generated text, model, and duration
```

---

## What Lesson02 Is Really Teaching

The lesson is about separating:

```text
what the user asks
```

from:

```text
how the application wants the LLM to behave
```

while also separating:

```text
what the feature needs
```

from:

```text
which AI provider happens to fulfill it
```
