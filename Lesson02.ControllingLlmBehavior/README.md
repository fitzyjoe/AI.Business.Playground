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

The key idea is:

> **The application can separate what the user asks from how the model should behave while answering.**

---

## Learning Goals

By the end of Lesson02, you should understand:

- the difference between a user prompt and a system prompt;
- how temperature influences response variability;
- how maximum output tokens constrain response length;
- how an application can choose an AI provider independently from business logic;
- how a request can override the configured default model;
- why provider-specific details should stay behind an application abstraction;
- how request-level controls are translated into provider-specific options;
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
    ↓
OllamaProvider
    ↓
Ollama
```

The feature layer knows about application-level concepts such as `Prompt`, `SystemPrompt`, `Temperature`, `Provider`, `Model`, and `MaxTokens`.

The Ollama provider is responsible for translating those concepts into an Ollama chat request.

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
│           └── OllamaProvider.cs
├── Lesson02.ControllingLlmBehavior.csproj
├── Program.cs
├── appsettings.json
└── README.md
```

---

## The HTTP API

Lesson02 exposes one endpoint:

```http
POST /api/prompt
```

A request can include:

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

Only `prompt` is required by the C# type.

The remaining fields allow the caller to influence how the request is executed.

---

## AiRequest

The request model contains the controls introduced in this lesson:

```csharp
public sealed class AiRequest
{
    public required string Prompt { get; init; }

    public string? SystemPrompt { get; init; }

    public float Temperature { get; init; } = 0.2f;

    public string Provider { get; init; } = "ollama";

    public string? Model { get; init; }

    public int? MaxTokens { get; init; }
}
```

This is deliberately simple. The feature does not expose Ollama-specific request types to the API layer.

---

## User Prompt vs. System Prompt

The user prompt is the task or question:

```text
Explain dependency injection.
```

The system prompt provides higher-level behavioral guidance:

```text
You are a concise technical instructor.
```

`OllamaProvider` converts these into separate chat messages.

If `SystemPrompt` is present:

```text
System message
    ↓
User message
```

If it is absent:

```text
User message only
```

This separation lets the application influence tone, role, format, or constraints without mixing those instructions into the user's content.

---

## Temperature

`Temperature` defaults to:

```text
0.2
```

The provider maps it to Ollama's request options:

```csharp
var options = new RequestOptions
{
    Temperature = aiRequest.Temperature
};
```

Lower temperature generally encourages more repeatable, conservative responses.

Higher temperature generally allows more variation.

Temperature does not make a model deterministic in an absolute sense. It is one control over sampling behavior.

### Example experiment

Run the same request several times with:

```json
"temperature": 0.0
```

Then repeat with:

```json
"temperature": 1.2
```

Compare how much the wording and content vary.

---

## Maximum Output Tokens

`MaxTokens` is optional.

When supplied, `OllamaProvider` maps it to:

```csharp
options.NumPredict = aiRequest.MaxTokens.Value;
```

This constrains how many tokens the model may generate.

For example:

```json
{
  "prompt": "Explain REST APIs in detail.",
  "maxTokens": 50
}
```

will usually produce a much shorter response than the same request without a token limit.

A token limit is not the same thing as an exact character count or word count.

---

## Provider Selection

The request includes:

```json
"provider": "ollama"
```

`Handler` does not construct or call `OllamaProvider` directly.

Instead:

```text
Handler
    ↓
IAiProviderFactory.GetProvider(...)
    ↓
IAiProvider
```

The current factory supports:

```text
ollama
```

and throws `NotSupportedException` for unknown providers.

This is an important architectural separation:

```text
feature logic
    ≠
provider-specific implementation
```

The API and handler can work with application-level AI abstractions while provider-specific code remains under `Infrastructure/Ai`.

---

## Model Selection

`appsettings.json` configures the default Ollama model:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "gemma3:4b"
  }
}
```

A request may override that model:

```json
{
  "prompt": "Explain polymorphism.",
  "model": "another-installed-model"
}
```

Inside `OllamaProvider`:

```csharp
var model = aiRequest.Model ?? _options.Model;
```

So the resolution rule is:

```text
request model if supplied
    ↓ otherwise
configured default model
```

This keeps a sensible default while still allowing per-request experimentation.

---

## Provider Configuration

`Program.cs` binds the `Ollama` configuration section to `OllamaOptions` and validates it at startup.

The application verifies that:

```text
Endpoint is an absolute URI
Model is not blank
```

The typed `HttpClient` uses the configured endpoint as its base address.

This means configuration errors are detected when the application starts rather than during the first prompt request.

---

## Endpoint and Handler Responsibilities

`Endpoint` is responsible for HTTP concerns:

```text
route
request binding
HTTP response
endpoint metadata
```

`Handler` is deliberately small:

```text
receive AiRequest
    ↓
select provider
    ↓
send request
    ↓
return AiResponse
```

The handler does not know how Ollama expresses temperature, token limits, or chat messages.

That responsibility belongs to the provider.

---

## OllamaProvider Responsibilities

`OllamaProvider` translates the application request into Ollama-specific structures.

It handles:

```text
model resolution
system-message creation
user-message creation
temperature
maximum output tokens
stream consumption
response timing
```

Even though Ollama streams its response internally, the API waits for the complete result and returns one `AiResponse`.

---

## Response

The API returns an `AiResponse` containing the generated text plus execution metadata.

Conceptually:

```json
{
  "text": "...model response...",
  "model": "gemma3:4b",
  "duration": "00:00:01.234..."
}
```

Including the model and elapsed duration makes experimentation easier because the caller can see which model produced the answer and how long it took.

---

## Exercise 1 — Basic Request

Start the application and send:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain dependency injection in one paragraph."
  }'
```

This uses:

```text
provider = ollama
temperature = 0.2
model = configured default
maxTokens = unlimited by this request
systemPrompt = none
```

---

## Exercise 2 — System Prompt

Compare:

```json
{
  "prompt": "Explain dependency injection."
}
```

with:

```json
{
  "prompt": "Explain dependency injection.",
  "systemPrompt": "Explain concepts as if teaching an experienced Java developer who is learning C#."
}
```

Observe how the higher-level instruction changes the style and framing of the response.

---

## Exercise 3 — Temperature

Try the same prompt several times with:

```json
"temperature": 0.0
```

Then repeat with:

```json
"temperature": 1.0
```

Look for differences in:

```text
word choice
examples
structure
creativity
consistency
```

---

## Exercise 4 — MaxTokens

Send:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Give me a detailed explanation of event-driven architecture.",
    "maxTokens": 40
  }'
```

Then repeat without `maxTokens` and compare the result.

---

## Exercise 5 — Model Override

If multiple Ollama models are installed, send the same prompt to two different models by changing:

```json
"model": "..."
```

Compare:

```text
response quality
latency
writing style
instruction following
```

The default model in `appsettings.json` does not need to change for each experiment.

---

## Exercise 6 — Unsupported Provider

Try:

```json
{
  "prompt": "Hello",
  "provider": "unknown"
}
```

The provider factory should reject it because only `ollama` is currently supported.

This demonstrates that provider choice is an application-controlled abstraction rather than an arbitrary string passed directly to an external service.

---

## Important Distinction: Control vs. Guarantee

Parameters such as:

```text
system prompt
temperature
max tokens
model
```

influence model behavior.

They do not provide absolute guarantees about semantic correctness.

For example:

```text
System prompt:
"Always answer in exactly three bullet points."
```

is an instruction to the model, not a deterministic schema validator.

That distinction becomes important whenever an application needs machine-enforced output structure or business rules.

---

## Architecture Takeaway

Lesson02 introduces a pattern that will remain useful throughout AI application development:

```text
Application request
    ↓
application-level AI abstraction
    ↓
provider-specific translation
    ↓
LLM service
```

The feature code expresses what it wants from the AI.

The provider implementation decides how those concepts map to the external model API.

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

The lesson stays focused on request-level model controls and provider abstraction.

---

## Lesson02 Acceptance Criteria

Lesson02 is complete when:

```text
✓ POST /api/prompt accepts a prompt
✓ an optional system prompt can influence model behavior
✓ temperature is passed to the provider
✓ maxTokens maps to the provider's output-token limit
✓ provider selection goes through IAiProviderFactory
✓ unsupported providers are rejected
✓ the configured default model is used when no model override is supplied
✓ a request can override the default model
✓ provider-specific Ollama details stay inside Infrastructure/Ai
✓ the response includes generated text, model, and duration
```

---

## What Lesson02 Is Really Teaching

The lesson is not merely about adding more JSON fields to a prompt endpoint.

It is about separating:

```text
what the user asks
```

from:

```text
how the application wants the LLM to behave while answering
```

and keeping provider-specific mechanics behind a stable application abstraction.
