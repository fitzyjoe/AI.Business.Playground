# Lesson08.SafeWriteOperations

## Safe AI-Initiated Write Operations

Lesson08 is where the course shifts from AI that only reads to AI that can propose changes.

Earlier lessons provided:

```text
MCP → authoritative structured data
RAG → internal unstructured knowledge
```

Lesson08 adds:

```text
User request
    ↓
LLM recognizes write intent
    ↓
propose_property_review
    ↓
PendingPropertyReview
    ↓
human/application approval
    ↓
PropertyReview
```

The central lesson is:

> **The LLM may propose an action. The application remains responsible for approval and execution.**

That boundary is identical whether the selected chat provider is Ollama or OpenAI.

---

## Learning Goals

By the end of Lesson08, you should understand:

- why an LLM request is not authorization;
- why write operations need a stronger boundary than reads;
- how to expose a safe write proposal as an AI tool;
- why the model should not receive approval or execution capabilities;
- how deterministic application validation differs from model reasoning;
- how explicit approval separates proposal from execution;
- how idempotency prevents repeated approval from duplicating a write;
- how MCP, RAG, and safe write proposals can coexist;
- why safety controls must remain application-owned regardless of provider.

---

## Architecture

```text
POST /api/message
    ↓
MessageHandler
    ├→ KnowledgeRetriever
    │      ↓
    │   Vector Store
    │      ↓
    │   embedding provider: Ollama or OpenAI
    │
    └→ IAiProviderFactory
           ↓
       chat provider: Ollama or OpenAI
           ↓
       function invocation
          ↙        ↘
     MCP reads   propose_property_review
                     ↓
              PropertyReviewService
                     ↓
              PendingPropertyReview
```

Approval remains outside the model tool path:

```text
POST /api/pending-property-reviews/{id}/approve
    ↓
PropertyReviewService.Approve()
    ↓
PropertyReview
```

There is intentionally no `approve_property_review` AI tool.

---

## Provider Story

Lesson08 carries forward two independent provider choices:

```text
Conversation.Provider
    → chat: ollama | openai

Rag.EmbeddingProvider
    → embeddings: ollama | openai
```

Changing providers must not change the write-safety boundary.

Both chat providers receive the same tool set, including the local `propose_property_review` function.

---

## Project Structure

```text
Lesson08.SafeWriteOperations/
├── Features/
│   ├── Conversations/
│   │   ├── InMemoryConversationRepository.cs
│   │   └── ...
│   ├── Knowledge/
│   │   ├── KnowledgeChunk.cs
│   │   ├── KnowledgeRetriever.cs
│   │   ├── KnowledgeSearchResult.cs
│   │   └── RagOptions.cs
│   └── PropertyReviews/
│       ├── InMemoryPendingPropertyReviewRepository.cs
│       ├── InMemoryPropertyReviewRepository.cs
│       ├── PendingPropertyReview.cs
│       ├── PropertyReview.cs
│       ├── PropertyReviewService.cs
│       ├── PropertyReviewTools.cs
│       └── ...
├── Infrastructure/
│   ├── Ai/
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

The feature-specific repositories and RAG classes are co-located with the features they implement.

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

For OpenAI embeddings, also configure an OpenAI embedding model such as `text-embedding-3-small` and appropriate dimensions.

---

## Proposal Is Not Execution

The desired flow is:

```text
LLM proposes
    ↓
application validates
    ↓
pending resource is created
    ↓
human/application approves or rejects
    ↓
application executes deterministically
```

The undesired flow is:

```text
LLM decides
    ↓
database changes immediately
```

The application boundary is what makes the write safe, not confidence in the model.

---

## Provider-Neutral Tool Availability

The same tools are available to both chat providers:

```text
MCP read tools
propose_property_review
```

The model may decide to call the proposal tool, but it does not receive approval or execution capabilities.

A provider change therefore affects model behavior, not application authority.

---

## Running Lesson08

```bash
dotnet run --project Lesson08.SafeWriteOperations
```

### Propose with Ollama

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Create a high-priority property review for parcel 0304-12-0042 because the client believes the assessment is excessive.",
    "provider": "ollama"
  }'
```

### Propose with OpenAI

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Create a high-priority property review for parcel 0304-12-0042 because the client believes the assessment is excessive.",
    "provider": "openai"
  }'
```

The provider may propose a pending review. It cannot approve or execute it.

---

## Deterministic Approval

Approval and rejection remain normal HTTP/application operations rather than model tools.

Repeated approval should not create duplicate business records. The proposal-to-execution relationship remains traceable through IDs and lifecycle timestamps.

---

## Embedding Provider Changes

As in Lesson07, do not mix vectors from different embedding models in the same index.

Changing embedding provider/model/dimensions means the knowledge chunks must be re-embedded. The in-memory index is rebuilt at startup in this lesson.

---

## Deliberately Out of Scope

Lesson08 does not add:

- model authorization;
- user identity or roles;
- production audit storage;
- arbitrary generic write workflows;
- automatic approval;
- provider failover;
- durable vector storage.

---

## Lesson08 Acceptance Criteria

```text
✓ both Ollama and OpenAI can drive the conversation/tool loop
✓ both providers receive MCP read tools and propose_property_review
✓ chat-provider selection remains conversation-level configuration
✓ embedding-provider selection remains independent from chat-provider selection
✓ the LLM can create a pending proposal but cannot approve or execute it
✓ deterministic validation occurs in application code
✓ approval/rejection occurs outside the LLM tool path
✓ repeated approval does not duplicate execution
✓ executed PropertyReview records remain traceable to their pending proposals
```

---

## What Lesson08 Is Really Teaching

> **AI can participate in write workflows without being granted authority to perform the write itself. That boundary must survive provider changes.**
