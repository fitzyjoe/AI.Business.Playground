# Lesson09.Agents

## Evolving Conversations into Agent-Backed Conversations

Lesson09 introduces Microsoft Agent Framework without starting a second, parallel conversation system.

The public conversation API remains:

```http
POST /api/message
```

What changes is the implementation behind that endpoint.

Earlier lessons manually maintained conversation messages and replayed the full history to the LLM. Lesson09 replaces that hand-built message-history orchestration with an Agent Framework `AgentSession`.

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
ChatClientAgent
    ↓
MCP tools + knowledge-search tool + proposal tool
```

The application still owns the conversation ID and business configuration. Agent Framework owns the conversational session state used by the agent.

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
```

```text
Lesson09

AgentSession owns agent conversation history
internal knowledge search becomes an agent tool
agent chooses property/RAG/proposal tools
```

Agent Framework gives that behavior an explicit `ChatClientAgent` and `AgentSession` abstraction.

---

## Learning Goals

By the end of this lesson, you should understand:

- how a `ChatClientAgent` sits on top of an `IChatClient`;
- how one agent can participate in many independent conversations;
- how `AgentSession` can hold the conversational state for one conversation;
- why the application can still own the external `conversationId`;
- how serialized agent-session state can be stored with an application conversation record;
- how RAG can move from an application-mandated step to an agent-selectable tool;
- how MCP tools and local `AIFunction` tools can coexist in one agent;
- why a model/tool loop from an earlier lesson was already agent-like behavior;
- why tool availability is a stronger safety boundary than prompt instructions alone.

---

## The Core Model

There are three different concepts in this lesson.

### `PropertyReviewAgent`

The agent defines:

```text
instructions
model client
available capabilities
```

There is one application-level `PropertyReviewAgent` service.

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

### `AgentSession`

`AgentSession` contains the framework-owned state for one ongoing interaction with the agent.

Conceptually:

```text
                  PropertyReviewAgent
                         │
            ┌────────────┼────────────┐
            ↓            ↓            ↓
     Conversation A Conversation B Conversation C
            │            │            │
      AgentSession A AgentSession B AgentSession C
```

A conversation is not an agent.

A conversation is one ongoing interaction with an agent.

---

## Why We No Longer Store `Conversation.Messages`

Earlier lessons stored:

```text
Conversation.Messages
```

and rebuilt the complete message list for every provider request.

Lesson09 removes that duplicate history model.

Instead:

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

The application does not independently maintain a second copy of the agent's chat history.

---

## Agent Capabilities

The agent receives four categories of capability.

### Property MCP tools

The existing `PropertyMcpClient` supplies tools from the Lesson05 MCP server, including property lookup/search operations.

### Internal knowledge search

Lesson09 exposes `KnowledgeRetriever.SearchAsync(...)` as:

```text
search_internal_knowledge
```

The agent can decide whether internal knowledge is relevant to the user's request.

### Property-review proposal

The existing `PropertyReviewTools` exposes:

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

That is intentional.

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

The instructions also tell the agent that approval requires the application/human workflow, but the stronger protection is that no approval tool is supplied to the agent.

---

## RAG Changes in Lesson09

In Lesson08, `MessageHandler` automatically ran:

```text
KnowledgeRetriever.SearchAsync(userMessage)
```

for every message.

The application decided that RAG would always happen.

In Lesson09, retrieval becomes another capability:

```text
search_internal_knowledge
```

Now the model can decide:

```text
Do I need authoritative property data?
Do I need internal company guidance?
Do I need to propose a review?
Do I already have enough information to answer?
```

This is a change in **who chooses retrieval**, not a magical difference between an "LLM" and an "agent."

---

## API

Lesson09 keeps the existing endpoint:

```http
POST /api/message
```

There is deliberately no separate `/api/agent/run` endpoint.

A new conversation starts when `conversationId` is omitted.

An existing conversation continues when `conversationId` is supplied.

Conversation-level settings can only be supplied when starting the conversation, preserving the behavior from the earlier conversation lesson.

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

The response includes the conversation ID:

```json
{
  "conversationId": "...",
  "content": "...",
  "model": "qwen3:8b",
  "duration": "..."
}
```

For this request, the agent should be able to choose a property MCP tool.

---

## Exercise 2 — Prove the Agent Conversation Has History

Store the conversation ID from the first response:

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

This history now comes from the restored `AgentSession`, not from `Conversation.Messages` being replayed by `MessageHandler`.

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

The agent can choose:

```text
search_internal_knowledge
```

because this is a company-guidance question.

Unlike Lesson08, `MessageHandler` does not automatically perform the vector search first.

---

## Exercise 4 — MCP + Knowledge Search in One Conversation

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "For parcel 0304-12-0042, tell me the assessed value and what our internal guidance says I should prepare before a hearing."
  }' | jq .
```

A reasonable tool sequence is:

```text
property MCP lookup
    ↓
search_internal_knowledge
    ↓
answer
```

The exact order is model-selected.

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
curl -s \
  http://localhost:5000/api/pending-property-reviews | jq .
```

Then inspect executed reviews:

```bash
curl -s \
  http://localhost:5000/api/property-reviews | jq .
```

The pending proposal may exist.

No executed `PropertyReview` should have been created by the agent.

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

The agent may create a pending proposal.

It cannot approve it because no approval tool exists in its capability set.

Approval remains application-controlled:

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/pending-property-reviews/<ID>/approve | jq .
```

---

## Exercise 7 — Conversation-Level Model Settings Still Work

Start a conversation with explicit settings:

```bash
curl -s \
  -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Explain the purpose of a property assessment review.",
    "temperature": 0.2,
    "maxTokens": 250
  }' | jq .
```

Those settings are stored on the application `Conversation` and applied through `ChatClientAgentRunOptions` each time that conversation runs.

As before, conversation-level settings cannot be changed by supplying them alongside an existing `conversationId`.

---

## Exercise 8 — Optional Conversation Instructions

A conversation may still supply its own additional system instructions when it is created:

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

The agent retains its application-defined safety and capability instructions. The conversation prompt supplies additional per-conversation guidance.

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

Lesson09 reduces its responsibility to approximately:

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

That is the architectural payoff of the lesson.

---

## `PropertyReviewAgent`

`PropertyReviewAgent` owns the Agent Framework integration.

It creates the `ChatClientAgent` with:

```text
Ollama IChatClient
agent instructions
MCP property tools
search_internal_knowledge
propose_property_review
```

It also provides the application with methods for:

```text
CreateSessionAsync
DeserializeSessionAsync
RunAsync
SerializeSessionAsync
```

The handler therefore does not need to know how Agent Framework represents its session state.

---

## Conversation Settings vs. Agent Instructions

The agent has application-defined instructions that establish its role and safety rules.

The conversation can additionally store:

```text
SystemPrompt
Provider
Model
Temperature
MaxTokens
```

Those settings are applied to each run of that conversation.

This keeps two different concerns separate:

```text
agent-level invariant behavior
    ≠
conversation-level configuration
```

---

## Persistence Scope

The lesson serializes `AgentSession` into the `Conversation` record, but the conversation repository is still in memory.

Therefore:

```text
AgentSession is serializable
```

but:

```text
Lesson09 conversations do not survive application restart
```

because `InMemoryConversationRepository` is intentionally still used.

A production application could persist the serialized session state in a database without changing the public `/api/message` contract.

---

## Deliberately Out of Scope

Lesson09 does not add:

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

The lesson focuses on one important evolution: **turn the existing conversation experience into an agent-backed conversation without creating a second parallel interaction model.**

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
✓ existing MCP property tools are available to the agent
✓ internal knowledge retrieval is available as search_internal_knowledge
✓ the agent chooses whether knowledge retrieval is needed
✓ propose_property_review remains available
✓ approve/reject/execute are not agent capabilities
✓ existing HTTP approval flow still works
✓ model, temperature, max-token, and optional system-prompt settings remain conversation-level
```

---

## Key Takeaway

Lesson09 is not a restart from conversations.

It is an evolution of them:

```text
hand-built conversation orchestration
                ↓
agent-backed conversation orchestration
```

The application still owns:

```text
conversation identity
business configuration
persistence boundary
approval boundary
```

Agent Framework now owns:

```text
agent session/history
agent invocation
model-selected use of allowed capabilities
```

The agent can decide how to use its capabilities, but the application still decides which capabilities exist at all.
