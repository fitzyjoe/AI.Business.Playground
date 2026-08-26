# Lesson01.BasicPrompting

## Basic Prompting with a Local LLM

Lesson01 establishes the smallest useful end-to-end AI application in the course.

The application accepts a prompt over HTTP, sends that prompt to a locally running Ollama model, waits for the model response, and returns the generated text together with basic response metadata.

```text
HTTP client
    ↓
POST /api/prompt
    ↓
Endpoint
    ↓
Handler
    ↓
IAiProvider
    ↓
OllamaProvider
    ↓
Ollama
    ↓
local LLM
```

The goal is not to introduce advanced prompting techniques. The goal is to create a clean, understandable vertical slice that connects application code to an LLM without coupling the feature directly to the Ollama SDK.

---

## Learning Goals

By the end of Lesson01, you should understand:

- how to expose a simple AI capability through an ASP.NET Core endpoint;
- how a user prompt becomes an LLM request;
- why provider-specific AI code should be isolated from feature code;
- how dependency injection connects `Handler` to `IAiProvider`;
- how application configuration supplies the Ollama endpoint and default model;
- how streaming provider responses can be accumulated into one API response;
- how cancellation flows from the HTTP request through to the model call;
- how to return useful metadata such as the model name and request duration;
- how a small vertical slice can serve as the foundation for more advanced AI behavior without over-engineering the first example.

---

## Prerequisites

This lesson assumes:

- .NET 10 is installed;
- Ollama is installed and running locally with one or more models like gemma3:4b;
- the model configured in `appsettings.json` is available in Ollama;
- It might be helpful but not required to have commands `jq` and `sed` installed;

The current configuration uses:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "gemma3:4b"
  }
}
```

You can verify your locally installed models with:

```bash
ollama list
```

If the configured model is missing, pull it before running the lesson:

```bash
ollama pull gemma3:4b
```

It may be helpful to understand how to modify the curl command or pipe the output to other commands.  First consider a base example:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me a table of radio frequencies that are considered AM or FM. Please include a column for frequency band in mhz and a second column showing the range name. I only want the two column table without other supporting text."}'
  
  {"text":"| Frequency (MHz) | Range Name        |\n|-----------------|--------------------|\n| 530 - 570       | Mediumwave AM      |\n| 610 - 690       | Mediumwave AM      |\n| 740 - 760       | Shortwave AM       |\n| 87.5 - 92.5     | FM (Standard)       |\n| 94.1 - 99.9     | FM (HD Radio)       |\n| 101.9 - 103.9   | FM (Various)        |\n| 107.9 - 108.3   | FM (Santa Monica)    |\n| 109.9 - 111.9   | FM (Various)        |\n\n\n**Note:** *These ranges are approximate and can vary slightly based on location due to regulatory changes and local station assignments.*","model":"gemma3:4b","duration":"00:00:05.2711090"}
```

**jq**. You can use jq to pretty print the JSON that is returned from the curl command

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me a table of radio frequencies that are considered AM or FM. Please include a column for frequency band in mhz and a second column showing the range name. I only want the two column table without other supporting text."}' \
| jq .

{
  "text": "| Frequency (MHz) | Range Name        |\n|-----------------|--------------------|\n| 530-570         | Mediumwave AM      |\n| 610-690         | Mediumwave AM      |\n| 740-760         | Shortwave AM       |\n| 87.5-92.5        | FM Local           |\n| 94.1-99.9        | FM Top 40          |\n| 95.5-96.5        | FM Adult Contemporary |\n| 103.9-104.7      | FM Sports          |\n| 105.1-107.9      | FM News/Talk        |\n| 107.5-108.1      | FM Gospel          |\n| 109.9-112.3      | FM Latin           |\n| 1490-1540        | Shortwave AM       |\n| 1620-1710        | Shortwave AM       |\n| 88-98            | FM HD Radio          |\n\n",
  "model": "gemma3:4b",
  "duration": "00:00:06.7653550"
}
```

**sed**.  I use sed to translate the newline "\n" into an actual newline

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me a table of radio frequencies that are considered AM or FM. Please include a column for frequency band in mhz and a second column showing the range name. I only want the two column table without other supporting text."}' \
| sed 's/\\n/\n/g'

{"text":"| Frequency (MHz) | Range Name        |
|-----------------|--------------------|
| 530 - 570       | Mediumwave         |
| 610 - 690       | Mediumwave         |
| 750 - 870       | Longwave           |
| 87.5 - 90.7     | FM (Narrowcast)    |
| 88 - 92.3       | FM                 |
| 101.1 - 102.9   | FM                 |
| 104.1 - 107.9   | FM                 |
| 108 - 111        | FM (Narrowcast)    |
| 108.1 - 108.3   | FM (Narrowcast)    |
| 109.9 - 112.2   | FM                 |
| 149 - 169       | FM (Transportation) |
| 171.9 - 174     | FM (Public Safety)  |
| 87.5 - 108.0    | Regional/Local FM   |
","model":"gemma3:4b","duration":"00:00:07.1476610"}
```

`-w "\nHTTP Status: %{http_code}\n"` you can use this to see the http response code which can be helpful for troubleshooting

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{"prompt": "Show me a table of radio frequencies that are considered AM or FM. Please include a column for frequency band in mhz and a second column showing the range name. I only want the two column table without other supporting text."}' \
  -w "\nHTTP Status: %{http_code}\n" 
  
{"text":"| Frequency (MHz) | Range Name          |\n|-----------------|----------------------|\n| 530 - 570        | Mediumwave           |\n| 620 - 680        | Mediumwave           |\n| 750 - 910        | Mediumwave           |\n| 87.5 - 89.1      | FM (N)               |\n| 94.1 - 96.9      | FM (W)               |\n| 103.9 - 105.5    | FM (E)               |\n| 107.9 - 108.1    | FM (P)               |\n| 109.9 - 112.4    | FM (K)               |\n| 123.9 - 134.0    | FM (C)               |\n| 149.7 - 153.5    | FM (M)               |\n| 162.3 - 173.8    | FM (H)               |\n| 174.1 - 176.0    | FM (S)               |\n| 194.1 - 200.5    | FM (A)               |\n| 88.1 - 89.1      | FM (N)               |\n| 94.1 - 96.9      | FM (W)               |\n| 103.9 - 105.5    | FM (E)               |\n| 107.9 - 108.1    | FM (P)               |\n| 109.9 - 112.4    | FM (K)               |\n| 123.9 - 134.0    | FM (C)               |\n| 149.7 - 153.5    | FM (M)               |\n| 162.3 - 173.8    | FM (H)               |\n| 174.1 - 176.0    | FM (S)               |\n| 194.1 - 200.5    | FM (A)               |","model":"gemma3:4b","duration":"00:00:12.3543630"}
HTTP Status: 200
```

---

## Project Structure

```text
Lesson01.BasicPrompting/
├── Features/
│   └── Models/
│       └── Execute/
│           ├── AiRequest.cs
│           ├── AiResponse.cs
│           ├── Endpoint.cs
│           └── Handler.cs
├── Infrastructure/
│   └── Ai/
│       ├── IAiProvider.cs
│       └── Providers/
│           ├── OllamaOptions.cs
│           └── OllamaProvider.cs
├── Lesson01.BasicPrompting.csproj
├── Program.cs
├── appsettings.json
└── README.md
```

The structure is deliberately small.

The feature contains the HTTP request/response behavior. The infrastructure layer contains the Ollama-specific implementation.

---

## The API

Lesson01 exposes one endpoint:

```http
POST /api/prompt
```

The request contains a single value:

```json
{
  "prompt": "Explain what an API is in two sentences."
}
```

The request model is intentionally minimal:

```csharp
public sealed class AiRequest
{
    public required string Prompt { get; init; }
}
```

There are no system prompts, temperature controls, model overrides, conversation IDs, tools, RAG, or structured-output settings in this lesson.

The lesson begins with the simplest useful contract:

```text
prompt in
    ↓
model response out
```

---

## Response

The API returns an `AiResponse` containing:

```text
Text
Model
Duration
```

Conceptually:

```json
{
  "text": "An API is a defined interface that allows software systems to communicate with one another...",
  "model": "gemma3:4b",
  "duration": "00:00:01.2345678"
}
```

`Text` is the generated model response.

`Model` identifies the Ollama model that handled the request.

`Duration` records how long the provider call took from the application's perspective.

The response metadata makes the endpoint more useful than returning a raw string because the caller can see which model ran and roughly how long generation took.

---

## Running the Lesson

From the repository root:

```bash
dotnet run --project Lesson01.BasicPrompting
```

The application uses the ASP.NET Core URL configured for the project/environment. The examples below assume:

```text
http://localhost:5000
```

---

## First Prompt

Send a request with `curl`:

```bash
curl -X POST \
  http://localhost:5000/api/prompt \
  -H "Content-Type: application/json" \
  -d '{
    "prompt": "Explain what an API is in two sentences."
  }'
```

---

## Endpoint

We will move to a more traditional `Controller` approach in a future lesson, but for now we are keeping this super simple.

`Endpoint.cs` defines the HTTP boundary using a minimal API endpoint.

Conceptually:

```csharp
app.MapPost(
    "/api/prompt",
    async (AiRequest request, Handler handler, CancellationToken cancellationToken) =>
    {
        var response = await handler.Handle(request, cancellationToken);
        return Results.Ok(response);
    });
```

The endpoint is responsible for HTTP concerns:

```text
route
request binding
cancellation token
HTTP response
```

It does not know how Ollama works.

That separation matters even in a small lesson.

---

## Handler

`Handler` represents the application-level use case.

Its job is simple:

```text
receive AiRequest
    ↓
call IAiProvider
    ↓
return AiResponse
```

The handler depends on:

```csharp
IAiProvider
```

rather than directly on:

```text
OllamaApiClient
```

That keeps provider-specific implementation details out of the feature.

The lesson intentionally does not add extra service layers between the endpoint and the provider. There is no business logic here yet that would justify them.

---

## Why `IAiProvider` Exists

The provider abstraction is small:

```csharp
public interface IAiProvider
{
    Task<AiResponse> SendAsync(
        AiRequest request,
        CancellationToken cancellationToken = default);
}
```

Its purpose is not abstraction for abstraction's sake.

It creates a useful boundary:

```text
Feature code
    ↓
IAiProvider
────────────────────────
provider boundary
────────────────────────
OllamaProvider
    ↓
Ollama SDK
```

The feature asks for an AI response.

The provider decides how to communicate with a particular model host.

This means `Handler` does not need to know about:

- `OllamaApiClient`;
- Ollama chat request types;
- Ollama message roles;
- streaming response chunks;
- Ollama endpoint configuration.

---

## OllamaProvider

`OllamaProvider` is the concrete `IAiProvider` implementation.

It receives:

```text
HttpClient
IOptions<OllamaOptions>
```

and configures the Ollama client using the configured endpoint and default model.

The provider creates a chat request containing one user message:

```text
Role: User
Content: request.Prompt
```

Conceptually:

```text
AiRequest.Prompt
    ↓
Ollama ChatRequest
    ↓
User message
    ↓
SelectedModel
```

**No system message is added in this lesson.**

---

## Streaming Internally, One Response Externally

Ollama's chat API returns results as an asynchronous stream.

The provider reads that stream:

```csharp
await foreach (var response in _ollama.ChatAsync(chatRequest, cancellationToken))
```

and accumulates the response chunks into a `StringBuilder`.

Conceptually:

```text
Ollama chunk 1 ─┐
Ollama chunk 2 ─┼─→ StringBuilder → complete text
Ollama chunk 3 ─┘
```

The HTTP API itself does not stream to the caller in this lesson.

Instead:

```text
provider receives stream
    ↓
provider assembles complete response
    ↓
API returns one AiResponse
```

This keeps the first lesson simple while still showing that the underlying model API may behave differently from the application's public API.

---

## Cancellation

ASP.NET Core supplies a `CancellationToken` to the endpoint.

That token is passed through:

```text
Endpoint
 ↓
Handler
 ↓
IAiProvider
 ↓
OllamaProvider
 ↓
Ollama ChatAsync
```

If the HTTP client disconnects or the request is cancelled, the model call can also be cancelled rather than continuing unnecessary work.

This is a small implementation detail with an important production principle:

> Long-running AI calls should participate in normal application cancellation behavior.

---

## Configuration

The Ollama connection is configured in `appsettings.json`:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "gemma3:4b"
  }
}
```

`Program.cs` binds the `Ollama` section to `OllamaOptions`:

```text
appsettings.json
    ↓
OllamaOptions
    ↓
OllamaProvider
```

The provider then uses:

```text
Endpoint
    → HttpClient.BaseAddress

Model
    → OllamaApiClient.SelectedModel
```

The endpoint itself does not contain either value.

That keeps runtime configuration out of feature code.

---

## Dependency Injection

`Program.cs` registers the main pieces:

```text
Handler
IAiProvider → OllamaProvider
HttpClient
OllamaOptions
```

At runtime:

```text
Endpoint asks for Handler
    ↓
DI creates Handler
    ↓
Handler asks for IAiProvider
    ↓
DI supplies OllamaProvider
```

This is the first important architectural boundary in the course:

```text
application behavior
    ≠
provider implementation
```

---

## Vertical Slice Design

The prompt feature is grouped together under:

```text
Features/Models/Execute/
```

The files involved in the use case are colocated:

```text
AiRequest
AiResponse
Endpoint
Handler
```

This makes it easy to follow the complete feature from HTTP request to application handler.

Provider-specific code lives separately under:

```text
Infrastructure/Ai/
```

For this small application, that is enough structure.

There is no need for a larger multi-project Clean Architecture layout or additional layers merely to forward one prompt to a model.

---

## Hands-On Lab

The learner-directed basic-prompt, formatting, repeatability, response-time, provider-failure, and cancellation exercises are in [LAB.md](LAB.md). The LAB preserves the runnable curl commands while keeping this README focused on the completed architecture and reference behavior.

---

## What This Lesson Does Not Guarantee

A prompt such as:

```text
Return exactly three bullet points.
```

is an instruction to the model, not a deterministic application constraint.

The model may usually follow it, but ordinary prompting does not provide the same guarantees as normal typed application code.

Lesson01 therefore treats the model response as generated text:

```text
string in
    ↓
LLM
    ↓
string out
```

That is sufficient for establishing the basic integration path.

---

## Error Handling

Lesson01 keeps error handling deliberately minimal.

The endpoint declares a possible `500 Internal Server Error`, but the project does not yet add custom exception handlers or provider-specific error translation.

Examples of failures that can surface include:

```text
Ollama is not running
configured model is missing
configured endpoint is incorrect
request is cancelled
provider call fails
```

The goal is to understand the successful AI request path before adding more elaborate error-handling infrastructure.

---

## Deliberately Out of Scope

Lesson01 does not add:

- conversation history;
- conversation IDs;
- system prompts as a separate API field;
- temperature controls;
- max-token controls;
- per-request model selection;
- multiple AI providers;
- tool calling;
- MCP;
- RAG;
- embeddings;
- vector search;
- structured outputs;
- JSON Schema enforcement;
- agents;
- write operations;
- authentication;
- persistent storage;
- retries or resilience policies;
- streaming HTTP responses;
- production observability.

Those concepts are intentionally absent so the first AI integration remains easy to understand.

---

## Testing Strategy

This lesson is simple enough to test manually with `curl`.

Useful checks include:

```text
POST /api/prompt returns HTTP 200 when Ollama is available
response text is non-empty
response model matches the configured Ollama model
response duration is populated
cancellation is propagated
```

Because the response text is generated by an LLM, tests should generally avoid asserting one exact natural-language answer.

A better assertion is usually about the application contract:

```text
Did a response arrive?
Was the expected model used?
Was metadata returned?
Did failures propagate appropriately?
```

---

## Lesson01 Acceptance Criteria

Lesson01 is complete when:

```text
✓ the application starts successfully
✓ POST /api/prompt accepts a JSON prompt
✓ Handler delegates the request through IAiProvider
✓ OllamaProvider sends the prompt to the configured local model
✓ the complete generated response is returned to the caller
✓ the response includes model metadata
✓ the response includes request duration
✓ cancellation flows through the request path
✓ feature code does not directly depend on the Ollama SDK
```

---

## What Lesson01 Is Really Teaching

The code is small, but it establishes an important architectural pattern:

```text
HTTP API
    ↓
application feature
    ↓
provider abstraction
    ↓
AI runtime
```

The important takeaway is not simply:

> Call Ollama from C#.

It is:

> **Treat the LLM as an external application capability behind a small, explicit boundary.**

That keeps the first integration easy to reason about while leaving the application free to evolve without embedding provider-specific SDK details directly into its feature code.
