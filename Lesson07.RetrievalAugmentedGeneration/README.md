# Lesson06.ConsumingMcpServers

## Letting an LLM Use MCP Tools

Lesson06 builds on two earlier lessons:

- **Lesson03.LlmConversations** introduced multi-turn conversations, conversation history, provider abstractions, and per-conversation AI settings.
- **Lesson05.McpFundamentals** introduced an independent MCP server that exposes property-record tools.

Lesson06 combines those capabilities.

The existing conversation-aware HTTP application now connects to the Lesson05 MCP server, discovers its tools, makes those tools available to Ollama, and allows the LLM to decide when a tool should be invoked.

The result is an AI application that can answer ordinary questions normally while using authoritative property data when a question requires it.

---

## Learning Goals

By the end of this lesson, you should understand:

- how an application connects to an MCP server as an MCP client;
- how an MCP server can be launched using stdio transport;
- how MCP tools are discovered dynamically;
- how `McpClientTool` integrates with `Microsoft.Extensions.AI`;
- why MCP does not require a custom Ollama-specific tool adapter;
- how `IChatClient` supports function/tool invocation;
- how `FunctionInvokingChatClient` manages the tool-call loop;
- how conversation history from Lesson03 can be preserved while adding MCP;
- how model, temperature, and token settings can still be selected per request;
- how tool errors can be returned to an LLM for possible self-correction;
- how an LLM can decide when no tool is needed.

---

## Architecture

```text
HTTP Client
    ↓
POST /api/message
    ↓
MessageHandler
    ↓
IAiProviderFactory
    ↓
IAiProvider
    ↓
OllamaProvider
    ↓
Microsoft.Extensions.AI IChatClient
    ↓
Ollama / qwen3:8b
    ↓
FunctionInvokingChatClient
    ↓
McpClientTool
    ↓
MCP over stdio
    ↓
Lesson05.McpFundamentals
    ↓
PropertyTools
    ↓
IPropertyRepository
    ↓
InMemoryPropertyRepository
```

Lesson06 does **not** reference Lesson05 code directly. It knows only that an MCP server exists and advertises tools.

---

## Building on Lesson03

Lesson06 intentionally carries forward the conversation architecture from Lesson03.

The conversation feature still handles:

- creating conversations;
- assigning conversation IDs;
- maintaining system, user, and assistant messages;
- loading existing conversations;
- preserving conversation-level AI settings;
- sending the complete conversation history to the AI provider.

The provider abstraction also remains:

```text
MessageHandler
    ↓
IAiProviderFactory
    ↓
IAiProvider
    ↓
OllamaProvider
```

MCP support is added below that boundary rather than replacing it.

---

## Project Structure

```text
Lesson06.ConsumingMcpServers/
├── Features/
│   └── Conversations/
│       └── ...
├── Infrastructure/
│   ├── Ai/
│   │   └── ...
│   └── Mcp/
│       └── PropertyMcpClient.cs
├── Program.cs
├── appsettings.json
├── README.md
└── Lesson06.ConsumingMcpServers.csproj
```

The important separation is:

```text
Features/Conversations
    owns the conversation use case

Infrastructure/Ai
    owns AI-provider integration

Infrastructure/Mcp
    owns MCP connectivity
```

---

## Prerequisites

Before running Lesson06, make sure you have:

- .NET 10 SDK;
- Ollama installed and running;
- a tool-capable Ollama model such as `qwen3:8b`;
- Lesson05 built successfully.

Check your Ollama models:

```bash
ollama list
```

If necessary:

```bash
ollama pull qwen3:8b
```

Build Lesson05 from the Lesson06 project directory:

```bash
dotnet build ../Lesson05.McpFundamentals/Lesson05.McpFundamentals.csproj
```

Lesson06 launches the compiled Lesson05 MCP server, so that build output must exist before Lesson06 starts.

---

## MCP Client

Lesson06 introduces `PropertyMcpClient`.

This is an application class, not a class supplied by the MCP SDK.

Its responsibilities are:

```text
locate Lesson05
    ↓
launch Lesson05 using stdio
    ↓
create the MCP client connection
    ↓
discover tools
    ↓
keep the connection alive
```

Conceptually:

```csharp
var transport = new StdioClientTransport(
    new StdioClientTransportOptions
    {
        Name = "Property Records",
        Command = "dotnet",
        Arguments =
        [
            "./bin/Debug/net10.0/Lesson05.McpFundamentals.dll"
        ],
        WorkingDirectory = lesson05Directory
    });

_client = await McpClient.CreateAsync(
    transport,
    cancellationToken: cancellationToken);

Tools =
[
    .. await _client.ListToolsAsync(
        cancellationToken: cancellationToken)
];
```

`ListToolsAsync()` asks the MCP server which tools it exposes. Lesson06 does not hard-code the tool definitions.

---

## MCP Server Lifecycle

`PropertyMcpClient` is registered as a singleton:

```csharp
builder.Services.AddSingleton<PropertyMcpClient>();
```

After the application is built, Lesson06 initializes the MCP connection:

```csharp
await app.Services
    .GetRequiredService<PropertyMcpClient>()
    .InitializeAsync();
```

This means Lesson06 establishes the Lesson05 connection and discovers its tools before accepting HTTP requests.

For this lesson, failure to connect to Lesson05 is treated as an application startup failure.

---

## Ollama and `IChatClient`

Lesson03 interacted with Ollama using OllamaSharp-specific chat request types.

Lesson06 uses the provider-neutral `Microsoft.Extensions.AI.IChatClient` API because MCP tools integrate directly with it.

`OllamaApiClient` implements `IChatClient`, so the existing injected `HttpClient` can still be used:

```csharp
_chatClient = new OllamaApiClient(httpClient)
    .AsBuilder()
    .UseFunctionInvocation(configure: options =>
    {
        options.MaximumIterationsPerRequest = 6;
    })
    .Build();
```

The provider does **not** need to select a model in its constructor.

---

## Per-Request AI Settings

Lesson06 preserves Lesson03's per-request model selection:

```csharp
var model =
    aiRequest.Model ?? _options.Model;
```

The values are placed into `ChatOptions`:

```csharp
var chatOptions = new ChatOptions
{
    ModelId = model,
    Temperature = aiRequest.Temperature,
    MaxOutputTokens = aiRequest.MaxTokens,
    Tools = [.. _propertyMcpClient.Tools]
};
```

The new piece is:

```csharp
Tools = [.. _propertyMcpClient.Tools]
```

---

## Why MCP Tools Work with `IChatClient`

The MCP C# SDK represents discovered tools as `McpClientTool` objects.

`McpClientTool` is also an AI function, so Lesson06 does not need a custom MCP-to-Ollama adapter.

```text
MCP Server
    ↓
McpClientTool
    ↓
AIFunction
    ↓
IChatClient
```

The discovered MCP tools can be supplied directly through `ChatOptions.Tools`.

---

## Automatic Function Invocation

The most important new behavior in Lesson06 comes from:

```csharp
.UseFunctionInvocation(...)
```

With function invocation enabled:

```text
User prompt
    ↓
Ollama
    ↓
model requests an MCP tool
    ↓
FunctionInvokingChatClient
    ↓
McpClientTool invokes Lesson05
    ↓
structured tool result
    ↓
result is sent back to Ollama
    ↓
Ollama produces the final answer
```

The maximum iteration count prevents an unbounded tool-call loop:

```csharp
options.MaximumIterationsPerRequest = 6;
```

---

## Example: Parcel Lookup

A user asks:

```text
I am looking for info about a parcel of land.
The id is 0304-12-0042.
```

The model can choose:

```text
lookup_property_by_parcel
```

with:

```json
{
  "parcelNumber": "0304-12-0042"
}
```

Lesson05 returns structured property data, and Ollama turns that result into a natural-language response.

The LLM is responsible for presentation. Lesson05 remains responsible for authoritative property data.

---

## Running Lesson06

First build Lesson05:

```bash
dotnet build ../Lesson05.McpFundamentals/Lesson05.McpFundamentals.csproj
```

Make sure Ollama is running and `qwen3:8b` is available.

Then run Lesson06:

```bash
dotnet run
```

The examples below assume Lesson06 is listening on:

```text
http://localhost:5000
```

---

## Starting a Conversation

Send a message without a `conversationId`:

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What does assessed value mean?"
  }'
```

The response includes a new conversation ID. Use that ID in subsequent requests.

---

## Exercise Scenarios

### Scenario 1 — No Tool Needed

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What does assessed value mean?"
  }'
```

Expected:

```text
LLM answers normally
    ↓
no property lookup is required
```

This demonstrates that making tools available does not mean every request must invoke one.

### Scenario 2 — Exact Parcel Lookup

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What is the assessed value of parcel 0304-12-0042?"
  }'
```

Expected:

```text
Ollama chooses lookup_property_by_parcel
    ↓
Lesson05 returns the property record
    ↓
Ollama reports an assessed value of $8,450,000
```

### Scenario 3 — Owner Search

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What properties are owned by ABC Commercial Holdings?"
  }'
```

Expected:

```text
Ollama chooses search_properties_by_owner
    ↓
Lesson05 returns matching properties
    ↓
Ollama summarizes them
```

### Scenario 4 — Natural-Language Tool Selection

Ask:

```text
Does ABC Commercial own more than one property?
```

Do not mention a tool name. The model should infer that searching by owner is useful.

### Scenario 5 — Property Not Found

Ask:

```text
What is the assessed value of parcel 9999-99-9999?
```

Lesson05 should return:

```json
{
  "found": false,
  "property": null
}
```

The LLM should clearly say that no property was found and should not invent property data.

### Scenario 6 — Tool Validation Error

Ask:

```text
Show me up to 50 properties owned by ABC Commercial.
```

Lesson05 permits a maximum of 25.

One possible flow is:

```text
LLM requests maxResults = 50
    ↓
Lesson05 returns a tool error
    ↓
"maxResults must be between 1 and 25."
    ↓
LLM may retry with a valid value
```

Whether the model retries is model behavior and should not be assumed.

### Scenario 7 — Multi-Turn Conversation

First ask:

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Who owns parcel 0304-12-0042?"
  }'
```

Copy the returned `conversationId`.

Then:

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "PUT-CONVERSATION-ID-HERE",
    "content": "What other properties do they own?"
  }'
```

This combines:

```text
conversation history
    +
previous assistant response
    +
MCP tool discovery
    +
new tool selection
```

---

## Conversation History and Tool Calls

Lesson06 continues to persist the conversation-level messages introduced in Lesson03:

```text
System
User
Assistant
```

The function-invocation middleware handles intermediate tool calls and tool results during an individual AI request.

The application currently persists the final assistant response rather than introducing a richer persistent tool-call history model.

That keeps Lesson06 focused on consuming MCP servers rather than expanding the conversation domain model.

---

## Tool Errors vs Protocol Errors

A tool can execute successfully at the MCP protocol level but still report an application-level error.

For example:

```text
maxResults = 50
```

can produce:

```text
Tool Error:
maxResults must be between 1 and 25.
```

Conceptually:

```text
Business/tool validation problem
    ↓
tool error
    ↓
LLM can see the error

Malformed MCP protocol request
    ↓
protocol error
    ↓
MCP operation itself failed
```

---

## Important Distinction: Ollama Does Not Speak MCP Directly

Ollama is not connecting directly to Lesson05.

Lesson06 is doing that work:

```text
Ollama
    ↓
Microsoft.Extensions.AI
    ↓
McpClientTool
    ↓
MCP client
    ↓
Lesson05 MCP server
```

MCP is an application integration protocol, not an Ollama-specific feature.

---

## Important Distinction: Tool Selection vs Tool Execution

The LLM decides:

```text
Which tool should I use?
What arguments should I provide?
```

The application decides:

```text
Which tools are available?
How are they connected?
How are they executed?
```

Lesson05 owns execution of the property tools.

Lesson06 owns the connection and makes those tools available to the LLM.

---

## Testing Strategy

The exercise scenarios above act as manual acceptance tests.

Later, the lessons can be revisited with different types of automated tests:

```text
Deterministic application logic
    → unit tests

MCP server/client interaction
    → integration tests

LLM tool-selection behavior
    → AI evaluations
```

LLM behavior should generally not be tested with exact-string assertions.

A better evaluation verifies things such as:

```text
- the correct MCP tool was invoked;
- the correct parcel number was supplied;
- the answer contains the grounded assessed value;
- the answer does not invent another value.
```

---

## Lesson06 Acceptance Criteria

```text
✓ The project retains the Lesson03 conversation architecture

✓ Lesson06 has no project reference to Lesson05

✓ Lesson06 launches Lesson05 using MCP stdio transport

✓ The MCP connection is initialized once at application startup

✓ Lesson06 dynamically discovers Lesson05 tools

✓ Ollama uses the discovered MCP tools through IChatClient

✓ Model selection still occurs per request

✓ Temperature and max-token settings still work per request

✓ The model can answer general questions without using a tool

✓ The model can select lookup_property_by_parcel

✓ The model can select search_properties_by_owner

✓ Missing parcels do not produce invented property data

✓ Tool errors are available to the LLM

✓ Multi-turn conversations still work

✓ Function invocation has a finite iteration limit
```

---

## What Is Deliberately Out of Scope

Lesson06 does not add:

- HTTP MCP transport;
- multiple MCP servers;
- OAuth;
- authorization policies;
- MCP write tools;
- approval workflows;
- RAG;
- agents;
- production reconnection logic;
- persistent tool-call history;
- production observability;
- tool caching.

The lesson is intentionally focused on one concept:

> **Connect an existing conversation-aware AI application to an MCP server and let the LLM use its tools.**
