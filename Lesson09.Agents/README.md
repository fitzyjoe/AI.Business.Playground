# Lesson09.Agents

## Evolving Conversations into Agent-Backed Conversations

Lesson09 introduces Microsoft Agent Framework without starting a second, parallel conversation system.

The public conversation API remains:

```http
POST /api/message
```

What changes is the implementation behind that endpoint.

Earlier lessons manually maintained conversation messages and replayed the full history to the LLM. Lesson09 replaces that
hand-built message-history orchestration with an Agent Framework `AgentSession` while preserving the provider abstraction
introduced earlier in the course.

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
    ↓
IChatClient
    ↓
ChatClientAgent
    ↓
MCP tools + knowledge-search tool + proposal tool
```

The application still owns conversation identity, provider selection, business configuration, persistence, and approval boundaries.
Agent Framework owns the conversational session state and agent invocation.

---

## What Actually Changes

Lesson08 already had model-driven tool calling.

The LLM could decide whether it needed:

```text
property MCP tools
propose_property_review
```

So Lesson09 is **not** teaching that an agent is simply an LLM that can call tools.

The meaningful changes are:

```text
Lesson08

application owns message history
application always performs RAG
LLM chooses property/proposal tools
IAiProvider owns the chat execution loop
```

```text
Lesson09

AgentSession owns agent conversation history
internal knowledge search becomes an agent tool
agent chooses property/RAG/proposal tools
ChatClientAgent owns the agent execution loop
IAiProvider supplies the provider-specific IChatClient
```

Agent Framework gives the model/tool behavior explicit `ChatClientAgent` and `AgentSession` abstractions without eliminating
our application-level provider boundary.

---

## Learning Goals

By the end of this lesson, you should understand:

- how a `ChatClientAgent` sits on top of an `IChatClient`;
- how `IAiProvider` can expose a provider-specific `IChatClient` without owning a second chat loop;
- how `IAiProviderFactory` allows a conversation to select an AI provider;
- how one agent definition can participate in many independent conversations;
- how `AgentSession` can hold the conversational state for one conversation;
- why the application can still own the external `conversationId`;
- how serialized agent-session state can be stored with an application conversation record;
- how RAG can move from an application-mandated step to an agent-selectable tool;
- how MCP tools and local `AIFunction` tools can coexist in one agent;
- why tool availability is a stronger safety boundary than prompt instructions alone.

---

## The Core Model

There are several distinct responsibilities in this lesson.

### `Conversation`

The application conversation defines:

```text
Id
SystemPrompt
Provider
Model
Temperature
MaxTokens
AgentSessionState
CreatedAt
UpdatedAt
```

The application uses `Conversation.Id` as the public conversation identifier.

`Conversation.Provider` determines which registered `IAiProvider` should supply the model client.

### `AgentSession`

`AgentSession` contains the framework-owned state for one ongoing interaction with the agent.

Earlier lessons stored:

```text
Conversation.Messages
```

Lesson09 instead stores serialized Agent Framework session state:

```text
AgentSession
    ↓
SerializeSessionAsync(...)
    ↓
JsonElement
    ↓
Conversation.AgentSessionState
```

When the next HTTP request arrives:

```text
Conversation.AgentSessionState
    ↓
DeserializeSessionAsync(...)
    ↓
AgentSession
    ↓
agent continues the conversation
```

`AgentSession` replaces the message-history responsibility of `Conversation`; it does not replace the application's conversation
record itself.

### `InMemoryConversationRepository`

The repository stores application `Conversation` records by `conversationId`.

Conceptually:

```text
InMemoryConversationRepository
        │
        ├── Conversation A ──→ serialized AgentSession A
        ├── Conversation B ──→ serialized AgentSession B
        └── Conversation C ──→ serialized AgentSession C
```

The repository answers:

> Which application conversation corresponds to this ID?

The `AgentSession` answers:

> What conversational state does the agent need to continue that conversation?

---

## Provider Abstraction

Agent Framework consumes the standard `Microsoft.Extensions.AI.IChatClient` abstraction.

Lesson09 preserves our own provider-selection layer above it:

```text
Conversation.Provider
        ↓
IAiProviderFactory
        ↓
IAiProvider
        ↓
IChatClient
        ↓
ChatClientAgent
```

`IAiProvider` is deliberately smaller than it was in earlier lessons:

```csharp
public interface IAiProvider
{
    string Name { get; }
    string DefaultModel { get; }
    IChatClient ChatClient { get; }
}
```

It no longer has a custom `SendAsync()` method because Agent Framework now owns the chat/agent execution loop.

The provider abstraction instead owns provider-specific construction and defaults.

### `OllamaProvider`

The current lesson includes one implementation:

```text
OllamaProvider
    ↓
OllamaApiClient
    ↓
IChatClient
```

`OllamaProvider` exposes:

```text
Name = "ollama"
DefaultModel = configured Ollama model
ChatClient = OllamaApiClient
```

### `AiProviderFactory`

The factory resolves providers by name:

```text
"ollama"
    ↓
OllamaProvider
```

A future provider can implement the same `IAiProvider` contract and be registered with dependency injection without changing
`MessageHandler` or the agent's tool logic.

Only Ollama is implemented in this lesson, but the architecture is no longer hard-wired to Ollama inside `PropertyReviewAgent`.

---

## `PropertyReviewAgent`

`PropertyReviewAgent` owns the Agent Framework integration.

It receives:

```text
IAiProviderFactory
Property MCP tools
KnowledgeTools
PropertyReviewTools
```

For each conversation it resolves the selected provider:

```text
Conversation.Provider
    ↓
IAiProviderFactory.GetProvider(...)
    ↓
IAiProvider
```

The provider supplies an `IChatClient`, which is then used by a `ChatClientAgent`.

The agent instances are cached per provider:

```text
PropertyReviewAgent
    ├── ollama → ChatClientAgent using OllamaProvider.ChatClient
    └── another provider → another ChatClientAgent
```

Each conversation still gets its own `AgentSession`.

That distinction is important:

```text
ChatClientAgent
    = agent definition + model client + tools

AgentSession
    = one conversation's framework-owned state
```

---

## Conversation-Level Model Selection

A conversation can optionally specify a model when it is created.

If no model is supplied:

```text
Conversation.Model
    = null
        ↓
IAiProvider.DefaultModel
```

If a model is supplied:

```text
Conversation.Model
    ↓
ChatOptions.ModelId
```

This preserves the earlier lesson behavior while moving the actual model request through Agent Framework.

---

## Agent Capabilities

The agent receives the existing property MCP tools plus two local functions.

### Property MCP tools

`PropertyMcpClient` supplies property lookup/search operations from the Lesson05 MCP server.

### Internal knowledge search

Lesson09 exposes `KnowledgeRetriever.SearchAsync(...)` as:

```text
search_internal_knowledge
```

The agent decides whether internal company knowledge is relevant to the current request.

### Property-review proposal

`PropertyReviewTools` exposes:

```text
propose_property_review
```

This creates a pending proposal only.

### No approval capability

The agent does **not** receive tools for:

```text
approve_property_review
reject_property_review
execute_property_review
```

The instructions also tell the agent that approval requires the application/human workflow, but the stronger protection is that
those capabilities were never delegated to the agent.

---

## Trust Boundary

The agent can create:

```text
PendingPropertyReview
```

but cannot turn it into:

```text
PropertyReview
```

The boundary remains:

```text
agent
    ↓
propose_property_review
    ↓
PendingPropertyReview
────────────────────────────
human/application approval
────────────────────────────
    ↓
PropertyReview
```

---

## RAG Changes in Lesson09

In Lesson08, `MessageHandler` automatically ran:

```text
KnowledgeRetriever.SearchAsync(userMessage)
```

for every message.

In Lesson09, retrieval becomes another capability:

```text
search_internal_knowledge
```

The model can now decide:

```text
Do I need authoritative property data?
Do I need internal company guidance?
Do I need to propose a review?
Do I already have enough information to answer?
```

This is a change in **who chooses retrieval**, not a magical distinction between an "LLM" and an "agent."

---

## API

Lesson09 keeps the existing endpoint:

```http
POST /api/message
```

There is deliberately no separate `/api/agent/run` endpoint.

A new conversation starts when `conversationId` is omitted.

An existing conversation continues when `conversationId` is supplied.

Provider, model, temperature, max-token, and system-prompt settings can only be supplied when starting a conversation.

---

## Running the Lesson

Build the MCP server first because Lesson09 launches it over stdio:

```bash
dotnet build Lesson05.McpFundamentals/Lesson05.McpFundamentals.csproj
```

Then run Lesson09:

```bash
ASPNETCORE_URLS=http://localhost:5000 \
dotnet run --project Lesson09.Agents
```

The examples below assume:

```text
http://localhost:5000
```

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

Only Ollama is registered in this lesson.

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

`AiProviderFactory` should reject the unsupported provider rather than allowing `PropertyReviewAgent` to contain
provider-specific branching logic.

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

## MessageHandler Is Smaller Now

Earlier `MessageHandler` was responsible for:

```text
load conversation
create user message
run RAG
build system messages
append previous history
call provider
create assistant message
append both messages
save conversation
```

Lesson09 reduces its responsibility to:

```text
load/create Conversation
    ↓
restore/create AgentSession
    ↓
run PropertyReviewAgent
    ↓
serialize updated AgentSession
    ↓
save Conversation
```

The handler does not choose an AI SDK or build a provider-specific chat request.

---

## Responsibilities After the Refactor

```text
MessageHandler
    → conversation lifecycle

InMemoryConversationRepository
    → application conversation storage

AgentSession
    → framework-owned conversation state/history

PropertyReviewAgent
    → instructions, tools, Agent Framework integration

IAiProviderFactory
    → provider selection

IAiProvider
    → provider-specific IChatClient + default model

OllamaProvider
    → Ollama-specific client construction

ChatClientAgent
    → agent invocation/tool loop
```

This avoids two undesirable extremes:

```text
PropertyReviewAgent directly hard-wired to Ollama
```

and:

```text
our own IAiProvider reimplementing the chat loop that Agent Framework already provides
```

---

## Persistence Scope

The lesson serializes `AgentSession` into the `Conversation` record, but the conversation repository is still in memory.

Therefore the session is serializable, but Lesson09 conversations do not survive application restart.

A production implementation could persist the serialized session state in a database without changing the public
`POST /api/message` contract.

---

## Deliberately Out of Scope

Lesson09 does not add:

- a second concrete LLM provider implementation;
- multiple cooperating agents;
- supervisor agents;
- agent handoffs;
- autonomous approval;
- durable database-backed conversation storage;
- background agents;
- workflow graphs;
- shell execution;
- arbitrary SQL tools;
- arbitrary file-write tools;
- production authentication or RBAC.

The provider boundary is present and switchable, but Ollama is the only implementation included in this lesson.

---

## Acceptance Criteria

Lesson09 is complete when:

```text
✓ POST /api/message remains the conversational API
✓ omitting conversationId creates a new conversation
✓ supplying conversationId resumes the existing conversation
✓ Conversation no longer maintains a duplicate Messages list
✓ AgentSession is serialized into Conversation.AgentSessionState
✓ restored AgentSession provides multi-turn history
✓ PropertyReviewAgent uses ChatClientAgent
✓ PropertyReviewAgent does not directly construct OllamaApiClient
✓ IAiProvider exposes Name, DefaultModel, and IChatClient
✓ IAiProviderFactory selects the provider from Conversation.Provider
✓ OllamaProvider owns Ollama-specific chat-client construction
✓ Conversation.Model overrides the provider default model when supplied
✓ existing MCP property tools are available to the agent
✓ internal knowledge retrieval is available as search_internal_knowledge
✓ the agent chooses whether knowledge retrieval is needed
✓ propose_property_review remains available
✓ approve/reject/execute are not agent capabilities
✓ existing HTTP approval flow still works
✓ temperature, max-token, and optional system-prompt settings remain conversation-level
```

---

## Key Takeaway

Lesson09 is an evolution of the existing conversation architecture:

```text
hand-built conversation orchestration
                ↓
agent-backed conversation orchestration
```

But Agent Framework does not eliminate every application abstraction.

The application still owns:

```text
conversation identity
provider selection
business configuration
persistence boundary
approval boundary
```

Agent Framework owns:

```text
agent session/history
agent invocation
model-selected use of allowed capabilities
```

And `IChatClient` becomes the handoff point between our provider abstraction and Agent Framework:

```text
IAiProvider
    ↓
IChatClient
    ↓
ChatClientAgent
```

That lets the application remain provider-neutral without maintaining a second, competing chat-execution pipeline.