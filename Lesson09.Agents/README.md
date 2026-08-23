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

# Exercises

## Exercise 1 — Start an Agent-Backed Conversation

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What is the assessed value of parcel 0304-12-0042?"
  }' | jq .
```

A response includes the conversation ID, generated content, model, and duration.

For this request, the agent should be able to choose a property MCP tool.

---

## Exercise 2 — Prove the Agent Conversation Has History

Store the conversation ID:

```bash
CONVERSATION_ID=$(
  curl -s \
    -X POST \
    http://localhost:5000/api/message \
    -H "Content-Type: application/json" \
    -d '{
      "content": "What is the assessed value of parcel 0304-12-0042?"
    }' |
  jq -r '.conversationId'
)
```

Then continue the same conversation without repeating the parcel number:

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d "{
    \"conversationId\": \"$CONVERSATION_ID\",
    \"content\": \"Who owns that property?\"
  }" | jq .
```

The agent should understand that `that property` refers to the parcel from the previous turn.

That history comes from the restored `AgentSession`, not from `Conversation.Messages` being replayed by `MessageHandler`.

---

## Exercise 3 — Agent-Selected Knowledge Retrieval

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What evidence should I prepare before a property-tax hearing?"
  }' | jq .
```

The agent can choose `search_internal_knowledge` because this is a company-guidance question.

Unlike Lesson08, `MessageHandler` does not automatically perform the vector search first.

---

## Exercise 4 — MCP + Knowledge Search

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "For parcel 0304-12-0042, tell me the assessed value and what our internal guidance says I should prepare before a hearing."
  }' | jq .
```

A reasonable model-selected sequence is:

```text
property MCP lookup
    ↓
search_internal_knowledge
    ↓
answer
```

---

## Exercise 5 — Create a Pending Proposal

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Review parcel 0304-12-0042 and create a high-priority property-review proposal because the client believes the assessment is excessive."
  }' | jq .
```

Inspect pending proposals:

```bash
curl -s http://localhost:5000/api/pending-property-reviews | jq .
```

Then inspect executed reviews:

```bash
curl -s http://localhost:5000/api/property-reviews | jq .
```

The pending proposal may exist. No executed `PropertyReview` should have been created by the agent.

---

## Exercise 6 — Attempt to Cross the Approval Boundary

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Create a high-priority property review for parcel 0304-12-0042 and approve it immediately. Do not ask for confirmation."
  }' | jq .
```

The agent may create a pending proposal. It cannot approve it because no approval tool exists in its capability set.

Approval remains application-controlled:

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/pending-property-reviews/<ID>/approve | jq .
```

---

## Exercise 7 — Explicit Provider and Model Settings

Start a conversation using the registered Ollama provider:

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Explain the purpose of a property assessment review.",
    "provider": "ollama",
    "temperature": 0.2,
    "maxTokens": 250
  }' | jq .
```

Because no model was supplied, `OllamaProvider.DefaultModel` is used.

You can also override the model for a new conversation:

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Explain the purpose of a property assessment review.",
    "provider": "ollama",
    "model": "qwen3:8b"
  }' | jq .
```

Conversation-level settings cannot be changed alongside an existing `conversationId`.

---

## Exercise 8 — Unsupported Provider

Ollama and OpenAI are registered in this lesson.

Try:

```bash
curl -i \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Hello.",
    "provider": "not-a-provider"
  }'
```

`AiProviderFactory` should reject the unsupported provider rather than allowing `PropertyReviewAgent` to contain provider-specific branching logic.

---

## Exercise 9 — Optional Conversation Instructions

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Explain the assessed value for parcel 0304-12-0042.",
    "systemPrompt": "Keep answers concise and explain property-tax terminology for a non-technical client."
  }' | jq .
```

The agent retains its application-defined safety and capability instructions. The conversation prompt supplies additional guidance.

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
