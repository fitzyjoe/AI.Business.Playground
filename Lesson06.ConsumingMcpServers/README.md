# Lesson06.ConsumingMcpServers

## Letting an LLM Use MCP Tools

Lesson06 combines the conversation architecture from Lesson03 with the independent MCP server built in Lesson05.

The application connects to the MCP server, discovers its property-record tools, exposes them to the selected chat provider, and allows the model to decide when a tool should be invoked.

The same MCP integration now works with either Ollama or OpenAI.

---

## Learning Goals

By the end of Lesson06, you should understand:

- how an application connects to an MCP server as a client;
- how an MCP server can be launched with stdio transport;
- how MCP tools are discovered dynamically;
- how `McpClientTool` integrates with `Microsoft.Extensions.AI`;
- why MCP tool integration is not specific to Ollama or OpenAI;
- how `IChatClient` supports provider-neutral function invocation;
- how conversation state and provider selection continue to work while tools are added;
- how tool errors can be returned to the model for possible self-correction;
- why making a tool available does not require the model to call it.

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
    ├── OllamaProvider
    └── OpenAiProvider
    ↓
Microsoft.Extensions.AI IChatClient
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
```

Lesson06 does not reference Lesson05 code directly. It communicates with Lesson05 through MCP.

---

## Provider-Neutral Tool Calling

Both chat providers expose the discovered MCP tools through `ChatOptions.Tools`.

Conceptually:

```csharp
var chatOptions = new ChatOptions
{
    Temperature = aiRequest.Temperature,
    MaxOutputTokens = aiRequest.MaxTokens,
    Tools = [.. _propertyMcpClient.Tools]
};
```

Both providers also wrap their `IChatClient` with function-invocation middleware:

```csharp
.UseFunctionInvocation(configure: options =>
{
    options.MaximumIterationsPerRequest = 6;
})
```

The important lesson is that MCP tools are application capabilities. They are not an Ollama-specific feature and they are not an OpenAI-specific feature.

---

## Project Structure

```text
Lesson06.ConsumingMcpServers/
├── Features/
│   └── Conversations/
│       ├── InMemoryConversationRepository.cs
│       └── ...
├── Infrastructure/
│   ├── Ai/
│   │   ├── AiProviderFactory.cs
│   │   ├── IAiProvider.cs
│   │   ├── IAiProviderFactory.cs
│   │   └── Providers/
│   │       ├── OllamaOptions.cs
│   │       ├── OllamaProvider.cs
│   │       ├── OpenAiOptions.cs
│   │       └── OpenAiProvider.cs
│   ├── ErrorHandling/
│   └── Mcp/
│       └── PropertyMcpClient.cs
├── Program.cs
├── appsettings.json
└── README.md
```

---

## Prerequisites

Build Lesson05 first:

```bash
dotnet build Lesson05.McpFundamentals/Lesson05.McpFundamentals.csproj
```

Then choose at least one chat provider.

### Ollama

Make sure Ollama is running and a tool-capable model such as `qwen3:8b` is installed:

```bash
ollama list
ollama pull qwen3:8b
```

### OpenAI

Set:

```bash
export OPENAI_AI_BUSINESS_PLAYGROUND="your-api-key"
```

---

## Configuration

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "qwen3:8b"
  },
  "OpenAI": {
    "Model": "gpt-5.2"
  }
}
```

A conversation may choose `ollama` or `openai` when it is created. That provider remains part of the conversation's configuration.

---

## MCP Client Lifecycle

`PropertyMcpClient` is a singleton. At application startup it:

```text
locates Lesson05
    ↓
launches it with stdio
    ↓
creates an MCP client
    ↓
discovers available tools
    ↓
keeps the connection alive
```

Failure to establish that MCP connection is treated as startup failure in this lesson.

---

## Automatic Tool Invocation

A representative flow is:

```text
User asks for assessed value
    ↓
selected provider determines a property lookup is useful
    ↓
model requests lookup_property_by_parcel
    ↓
FunctionInvokingChatClient invokes McpClientTool
    ↓
Lesson05 returns authoritative property data
    ↓
tool result returns to the model
    ↓
model produces final answer
```

The provider can also answer ordinary questions without invoking any MCP tool.

---

## Running Lesson06

```bash
dotnet run --project Lesson06.ConsumingMcpServers
```

Examples assume:

```text
http://localhost:5000
```

### Start an Ollama Conversation

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What is the assessed value of parcel 0304-12-0042?",
    "provider": "ollama"
  }'
```

### Start an OpenAI Conversation

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What is the assessed value of parcel 0304-12-0042?",
    "provider": "openai"
  }'
```

---

## Hands-On Lab

The learner-directed no-tool, parcel lookup, owner search, missing-property, validation-error, address-tool, multi-turn, and provider-comparison scenarios are in [LAB.md](LAB.md).

---

## MCP Does Not Mean the Provider Speaks MCP

Neither Ollama nor OpenAI connects directly to Lesson05.

```text
AI provider
    ↓
Microsoft.Extensions.AI
    ↓
McpClientTool
    ↓
MCP client
    ↓
Lesson05 MCP server
```

MCP is the application integration boundary.

---

## Tool Selection vs. Tool Execution

The model chooses:

```text
Which available tool would help?
What arguments should I propose?
```

The application decides:

```text
Which tools are available?
How are they connected?
How are they executed?
What limits do they enforce?
```

---

## Deliberately Out of Scope

Lesson06 does not add:

- HTTP MCP transport;
- multiple MCP servers;
- OAuth;
- authorization policies;
- MCP write tools;
- approval workflows;
- RAG;
- agents;
- provider failover;
- production reconnection logic;
- persistent tool-call history.

---

## Lesson06 Acceptance Criteria

```text
✓ Lesson06 launches Lesson05 over MCP stdio
✓ the MCP connection is initialized once at startup
✓ Lesson05 tools are discovered dynamically
✓ both Ollama and OpenAI can use the discovered tools through IChatClient
✓ a conversation selects its provider at creation
✓ model, temperature, and max-token settings still work
✓ general questions can be answered without a tool
✓ property lookups are grounded in Lesson05 data
✓ missing parcels do not produce invented property data
✓ tool errors are visible to the LLM
✓ multi-turn conversations still work
✓ function invocation has a finite iteration limit
```

---

## What Lesson06 Is Really Teaching

> **Connect a conversation-aware AI application to an MCP server and make the discovered tools available through a provider-neutral chat abstraction.**
