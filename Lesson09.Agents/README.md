# Lesson09.Agents

## Evolving Conversations into Agent-Backed Conversations

Lesson09 introduces Microsoft Agent Framework without starting a second, parallel conversation system.

The public API remains:

```http
POST /api/message
```

What changes is the implementation behind it.

Earlier lessons manually replayed message history and directly managed the tool-calling chat loop. Lesson09 introduces `ChatClientAgent` and `AgentSession` while preserving application-owned conversation identity, provider selection, business configuration, and approval boundaries.

Lesson09 also carries both Ollama and OpenAI forward as real chat providers.

---

## Architecture

```text
/api/message
    ↓
MessageHandler
    ↓
Conversation
    ↓
AgentSession
    ↓
PropertyReviewAgent
    ↓
IAiProviderFactory
    ↓
IAiProvider
    ├── OllamaProvider
    └── OpenAiProvider
    ↓ supplies
IChatClient
    ↓
ChatClientAgent
    ↓
MCP tools + knowledge-search tool + proposal tool
```

The application owns:

```text
conversationId
provider selection
model/temperature/token settings
serialized agent session state
write approval boundaries
```

Agent Framework owns the agent/session execution model.

---

## What Changes from Lesson08

```text
Lesson08
application owns message history
application performs RAG before the provider call
provider owns function-invocation chat loop
```

```text
Lesson09
AgentSession owns conversational state
knowledge search becomes an agent tool
agent chooses when RAG is useful
ChatClientAgent owns the execution loop
IAiProvider supplies IChatClient
```

An agent is not simply "an LLM that can call tools"; Lesson08 already had tool calling. The important shift is the explicit agent/session abstraction and model-directed orchestration of available capabilities.

---

## Provider Abstraction

`IAiProvider` is now deliberately small:

```csharp
public interface IAiProvider
{
    string Name { get; }
    string DefaultModel { get; }
    IChatClient ChatClient { get; }
}
```

`AiProviderFactory` receives all registered `IAiProvider` implementations and indexes them by `Name`.

That means adding OpenAI does not require another hard-coded provider switch in this lesson:

```text
IEnumerable<IAiProvider>
    ↓
AiProviderFactory
    ├── ollama
    └── openai
```

This is an intentional evolution from the explicit factory switches in earlier lessons.

---

## Agent Sessions

`Conversation` still owns the public conversation ID and configuration, but conversational state is now serialized Agent Framework state:

```text
AgentSession
    ↓
SerializeSessionAsync(...)
    ↓
Conversation.AgentSessionState
```

On the next turn:

```text
Conversation.AgentSessionState
    ↓
DeserializeSessionAsync(...)
    ↓
AgentSession
```

The conversation continues using the provider that was selected when it was created.

---

## Agent Tools

`PropertyReviewAgent` receives:

```text
property MCP tools
search_internal_knowledge
propose_property_review
```

The agent decides which tools are useful for a particular request.

The safe-write boundary from Lesson08 remains intact. There is still no approval or execution tool.

---

## Chat and Embedding Providers Remain Independent

Chat provider:

```text
Conversation.Provider = ollama | openai
```

Embedding provider:

```text
Rag.EmbeddingProvider = ollama | openai
```

The knowledge tool uses the configured embedding provider regardless of which chat provider is running the agent.

For example:

```text
Agent chat: openai
Knowledge embeddings: ollama
```

is valid.

---

## Project Structure

```text
Lesson09.Agents/
├── Features/
│   ├── Agents/
│   │   └── PropertyReviewAgent.cs
│   ├── Conversations/
│   │   ├── InMemoryConversationRepository.cs
│   │   └── ...
│   ├── Knowledge/
│   │   ├── KnowledgeChunk.cs
│   │   ├── KnowledgeRetriever.cs
│   │   ├── KnowledgeSearchResult.cs
│   │   ├── KnowledgeTools.cs
│   │   └── RagOptions.cs
│   └── PropertyReviews/
│       ├── InMemoryPendingPropertyReviewRepository.cs
│       ├── InMemoryPropertyReviewRepository.cs
│       └── ...
├── Infrastructure/
│   ├── Ai/
│   │   ├── AiProviderFactory.cs
│   │   ├── IAiProvider.cs
│   │   └── Providers/
│   │       ├── OllamaProvider.cs
│   │       └── OpenAiProvider.cs
│   ├── ErrorHandling/
│   └── Mcp/
├── Knowledge/
├── Program.cs
├── appsettings.json
└── README.md
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
  },
  "Rag": {
    "EmbeddingProvider": "ollama",
    "EmbeddingModel": "embeddinggemma",
    "EmbeddingDimensions": 768,
    "TopResults": 3
  }
}
```

To use OpenAI for chat or embeddings:

```bash
export OPENAI_AI_BUSINESS_PLAYGROUND="your-api-key"
```

---

## Running Lesson09

```bash
dotnet run --project Lesson09.Agents
```

### Start an OpenAI-Backed Agent Conversation

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Research parcel 0304-12-0042, check our hearing guidance, and prepare a high-priority review proposal if appropriate.",
    "provider": "openai"
  }'
```

Use the returned `conversationId` for later turns. The serialized `AgentSession` allows the agent to continue that conversation.

---

## Provider Comparison Exercise

Run the same agent task once with Ollama and once with OpenAI.

Evaluate outcomes rather than exact prose or exact tool order:

```text
Did the answer use authoritative property data when needed?
Did it search internal knowledge when useful?
Did it avoid inventing unavailable facts?
Did it keep proposals separate from approval/execution?
```

Different providers may choose different valid tool sequences.

---

## Embedding Configuration

For OpenAI embeddings, one option is:

```json
"Rag": {
  "EmbeddingProvider": "openai",
  "EmbeddingModel": "text-embedding-3-small",
  "EmbeddingDimensions": 768,
  "TopResults": 3
}
```

Changing embedding provider/model/dimensions requires rebuilding the vector index. The in-memory index is recreated at startup.

---

## Deliberately Out of Scope

Lesson09 does not yet add:

- background/scheduled agents;
- multi-agent coordination;
- user authentication or authorization;
- production AI request limits;
- provider allowlists;
- production telemetry;
- persistent agent-state storage;
- provider failover.

---

## Lesson09 Acceptance Criteria

```text
✓ the public conversation API remains /api/message
✓ Conversation keeps application identity and provider configuration
✓ AgentSession owns framework conversation state
✓ both Ollama and OpenAI are registered IAiProvider implementations
✓ AiProviderFactory discovers providers by Name
✓ ChatClientAgent can run with either provider's IChatClient
✓ the agent can choose MCP, knowledge, and proposal tools
✓ chat provider and embedding provider remain independent
✓ the model cannot approve or execute a property review
✓ agent session state is serialized back into the conversation
```

---

## What Lesson09 Is Really Teaching

> **How to introduce an agent framework without surrendering application ownership of identity, provider choice, business state, or safety boundaries.**
