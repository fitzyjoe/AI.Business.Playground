# Lesson07.RetrievalAugmentedGeneration

## Adding Retrieval-Augmented Generation (RAG)

Lesson07 adds retrieval from internal documents alongside the MCP capabilities from Lesson06.

The application now has two ways to ground an LLM response:

- **MCP tools** provide structured, operational business data.
- **RAG** retrieves relevant information from internal documents.

Lesson07 also introduces an important provider distinction:

> **The chat provider and the embedding provider are independent choices.**

You can use OpenAI for chat and Ollama for embeddings, Ollama for chat and OpenAI for embeddings, or the same provider for both.

---

## Learning Goals

By the end of Lesson07, you should understand:

- what embeddings are and why they are useful for semantic search;
- how an embedding model differs from a chat/generation model;
- how documents are split into searchable chunks;
- how embeddings are stored and searched in a vector store;
- how retrieved chunks can augment a chat request;
- how RAG and MCP serve different grounding needs;
- why chat-provider selection should not force an embedding-provider selection;
- why changing embedding models requires rebuilding the index;
- why retrieved content should be treated as reference material rather than instructions.

---

## Architecture

```text
                    ┌→ IAiProviderFactory → OllamaProvider / OpenAiProvider
POST /api/message ──┤
                    └→ KnowledgeRetriever
                           ↓
                      Vector Store
                           ↓
                selected embedding provider
                  ├→ Ollama
                  └→ OpenAI
```

The RAG flow is:

```text
Knowledge documents
    ↓
split into chunks
    ↓
embedding provider/model
    ↓
vectors
    ↓
vector store

User question
    ↓
embedding provider/model
    ↓
semantic similarity search
    ↓
top matching chunks
    ↓
temporary context
    ↓
selected chat provider
```

---

## Chat Provider vs. Embedding Provider

Conversation configuration controls chat:

```text
Conversation.Provider = ollama | openai
```

RAG configuration controls embeddings:

```text
Rag.EmbeddingProvider = ollama | openai
```

These choices are deliberately independent.

For example:

```text
Chat:       openai
Embeddings: ollama
```

is a valid configuration.

---

## Project Structure

```text
Lesson07.RetrievalAugmentedGeneration/
├── Features/
│   ├── Conversations/
│   │   ├── InMemoryConversationRepository.cs
│   │   └── ...
│   └── Knowledge/
│       ├── KnowledgeChunk.cs
│       ├── KnowledgeController.cs
│       ├── KnowledgeRetriever.cs
│       ├── KnowledgeSearchResult.cs
│       └── RagOptions.cs
├── Infrastructure/
│   ├── Ai/
│   │   └── Providers/
│   │       ├── OllamaProvider.cs
│   │       └── OpenAiProvider.cs
│   ├── ErrorHandling/
│   └── Mcp/
│       └── PropertyMcpClient.cs
├── Knowledge/
│   └── *.md
├── Program.cs
├── appsettings.json
└── README.md
```

The RAG behavior is co-located with the `Knowledge` feature. Provider-specific AI clients remain infrastructure.

---

## Default Configuration

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

This default uses Ollama embeddings, regardless of which chat provider a conversation chooses.

---

## Using OpenAI Embeddings

One valid configuration is:

```json
"Rag": {
  "EmbeddingProvider": "openai",
  "EmbeddingModel": "text-embedding-3-small",
  "EmbeddingDimensions": 768,
  "TopResults": 3
}
```

Set the API key:

```bash
export OPENAI_AI_BUSINESS_PLAYGROUND="your-api-key"
```

`EmbeddingDimensions` must match the dimensions produced/configured for the selected embedding model.

---

## Do Not Mix Embedding Spaces

Two embedding models with the same vector dimension do not necessarily produce compatible vectors.

Do not index documents with one model and then query that same index with another.

Changing any of these should be treated as an index rebuild:

```text
embedding provider
embedding model
embedding dimensions
```

Lesson07 uses an in-memory vector store and rebuilds its knowledge collection at startup, so switching configuration naturally regenerates the embeddings.

---

## Knowledge Retrieval

At startup, `KnowledgeRetriever`:

```text
loads Knowledge/*.md
    ↓
splits documents into chunks
    ↓
upserts chunks into the vector store
    ↓
selected embedding generator creates vectors
```

At request time it embeds the query, performs similarity search, and returns the top configured results.

The retrieved text is temporary context. It does not become authoritative application state.

---

## MCP vs. RAG

Use MCP for authoritative structured facts such as:

```text
parcel number
owner
assessed value
```

Use RAG for internal prose such as:

```text
appeal procedures
hearing preparation
valuation guidance
client communication guidance
```

They solve different retrieval problems and can coexist in the same request flow.

---

## Running Lesson07

Build Lesson05 first if needed for the MCP server:

```bash
dotnet build Lesson05.McpFundamentals/Lesson05.McpFundamentals.csproj
```

Then:

```bash
dotnet run --project Lesson07.RetrievalAugmentedGeneration
```

### OpenAI Chat with Ollama Embeddings

Keep `Rag.EmbeddingProvider` as `ollama`, then start a conversation with:

```bash
curl -X POST http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "What should I prepare before a property-tax hearing?",
    "provider": "openai"
  }'
```

### Ollama Chat with OpenAI Embeddings

Set `Rag.EmbeddingProvider` to `openai`, restart the application so the in-memory index is rebuilt, then start an Ollama conversation normally.

---

## Hands-On Lab

The learner-directed direct-search, MCP-vs-RAG, embedding-provider, `TopResults`, new-knowledge, and untrusted-retrieval experiments are in [LAB.md](LAB.md).

---

## Deliberately Out of Scope

Lesson07 does not add:

- persistent vector storage;
- index migration/versioning;
- enterprise search systems;
- reranking;
- hybrid keyword/vector search;
- agents choosing whether RAG is needed;
- authorization over documents;
- provider failover.

---

## Lesson07 Acceptance Criteria

```text
✓ internal markdown documents are indexed at startup
✓ semantic search returns relevant knowledge chunks
✓ RAG context can augment conversation requests
✓ Ollama and OpenAI are both available as chat providers
✓ Ollama and OpenAI are both available as embedding providers
✓ chat-provider choice is independent from embedding-provider choice
✓ embedding configuration validates provider, model, dimensions, and TopResults
✓ MCP and RAG remain separate grounding mechanisms
✓ switching embedding spaces rebuilds the in-memory index at startup
```

---

## What Lesson07 Is Really Teaching

> **RAG is a knowledge-retrieval feature that consumes embedding infrastructure; it is not inherently tied to the provider used for chat generation.**
