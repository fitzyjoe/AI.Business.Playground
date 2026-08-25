# Lesson09 Lab — Agents

This lab is the hands-on companion to [README.md](README.md).

## Goal

Understand the existing agent/session behavior, then add a new bounded agent tool to `PropertyReviewAgent` and observe when the agent chooses to use it.

## Predict

1. What does `AgentSession` own that the application previously managed manually?
2. Which state still belongs to the application rather than the Agent Framework?
3. Why should a new agent tool be narrowly scoped rather than a generic "do anything" function?
4. Why should an evaluation care more about a grounded outcome than one exact tool sequence?

## Run — Exercise 1: Start an Agent-Backed Conversation

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

## Run — Exercise 2: Prove the Agent Conversation Has History

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

## Run — Exercise 3: Agent-Selected Knowledge Retrieval

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

## Run — Exercise 4: MCP + Knowledge Search

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

Do not treat that exact sequence as a required implementation detail if another grounded sequence produces the same correct outcome.

## Run — Exercise 5: Create a Pending Proposal

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

## Attack — Exercise 6: Attempt to Cross the Approval Boundary

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

## Run — Exercise 7: Explicit Provider and Model Settings

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

## Attack — Exercise 8: Unsupported Provider

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

## Run — Exercise 9: Optional Conversation Instructions

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

## Compare Providers

Run the same agent task once with Ollama and once with OpenAI.

Evaluate outcomes rather than exact prose or exact tool order:

```text
Did the answer use authoritative property data when needed?
Did it search internal knowledge when useful?
Did it avoid inventing unavailable facts?
Did it keep proposals separate from approval/execution?
```

Different providers may choose different valid tool sequences.

## Build — Add a Bounded Tool

Add a tool called `summarize_property_review_status` that accepts a parcel number and returns a concise application-generated summary of any pending or executed property-review records for that parcel.

Requirements:

- expose a narrow, well-described tool contract;
- use existing application repositories/services rather than bypassing them;
- return bounded structured or concise data;
- make the tool available to `PropertyReviewAgent`;
- do not give the tool approval or execution authority.

## Run — Observe Agent Selection for the New Tool

Ask a question that clearly requires the new status tool, for example:

```text
What is the current review status for parcel 0304-12-0042?
```

Then compare with:

```text
What is the assessed value of parcel 0304-12-0042?
```

which should use MCP property data, and:

```text
What evidence should I prepare before a property-tax hearing?
```

which should use knowledge retrieval.

Finally, ask a combined question where the agent may reasonably use more than one tool.

Do not require an exact tool order; evaluate whether the outcome is appropriately grounded.

## Attack — Keep the New Tool Bounded

Ask the agent to use the new status tool to approve, reject, or create a review.

Verify that the tool's narrow contract prevents capability expansion. The new tool should summarize status; it should not become an alternate write path.

## Explain

1. What makes this an agent rather than just a chat client with manually orchestrated calls?
2. Why does the application still own business authority even when the agent chooses tools?
3. Why should evaluation focus on outcomes rather than one exact tool sequence?
4. What does `AgentSession` own, and what remains application-owned?
5. Why is a narrow tool description and contract part of the safety design?

## Lab Completion Criteria

```text
✓ AgentSession preserves multi-turn agent history
✓ agent can choose MCP, knowledge, and proposal tools
✓ provider/model settings still work at conversation creation
✓ unsupported providers are rejected
✓ approval remains outside the agent capability set
✓ new bounded tool is available to PropertyReviewAgent
✓ agent chooses it for appropriate status questions
✓ unrelated questions continue to use more appropriate capabilities
✓ new tool cannot approve or execute reviews
✓ AgentSession and application-owned business state remain distinct
```
