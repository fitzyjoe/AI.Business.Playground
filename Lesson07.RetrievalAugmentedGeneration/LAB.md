# Lesson07 Lab — Retrieval-Augmented Generation

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add a new company knowledge document and make a question reliably retrieve the relevant chunk without changing the distinction between RAG and MCP.

## Predict

1. When should the application use MCP instead of RAG?
2. Why must document and query embeddings come from the same embedding space?
3. What happens to the current in-memory index when the application restarts?

## Run — Search the Knowledge Endpoint Directly

Start Lesson07 using the setup in [README.md](README.md), then query retrieval directly before involving the chat model:

```bash
curl -s \
  --get \
  http://localhost:5000/api/knowledge/search \
  --data-urlencode 'query=What should I prepare before a property-tax hearing?' \
  | jq .
```

Inspect which source chunks are returned. This isolates semantic retrieval from generation and makes it easier to reason about whether a later RAG answer was grounded in useful context.

## Run — Compare MCP Grounding with RAG Grounding

Ask a procedure or policy question that should come from internal prose:

```text
What should I prepare before a property-tax hearing?
```

Then ask a question requiring authoritative property data:

```text
What is the assessed value of parcel 0304-12-0042?
```

Compare the grounding paths. The first should depend on RAG; the second should depend on MCP property data.

## Build — Add New Internal Knowledge

Create a new markdown document describing an internal business procedure not already represented in the knowledge base.

Then implement or tune the retrieval behavior so a natural user question about that procedure returns the intended source among the top results. You may adjust chunking, document wording, query wording, or `TopResults`, but preserve the provider-neutral embedding abstraction.

## Run — Verify Retrieval

Test:

- a question that should retrieve the new document;
- a semantically similar paraphrase;
- an unrelated question that should not depend on the new document.

Use the direct knowledge endpoint as well as the conversation API so you can distinguish retrieval quality from model response quality.

## Run — Compare Embedding Providers

Run the same chat provider with Ollama embeddings, then with OpenAI embeddings.

For example, keep the chat provider as OpenAI and first configure:

```json
"Rag": {
  "EmbeddingProvider": "ollama",
  "EmbeddingModel": "embeddinggemma",
  "EmbeddingDimensions": 768,
  "TopResults": 3
}
```

Run the direct search and record the returned chunks.

Then switch to an OpenAI embedding configuration such as:

```json
"Rag": {
  "EmbeddingProvider": "openai",
  "EmbeddingModel": "text-embedding-3-small",
  "EmbeddingDimensions": 768,
  "TopResults": 3
}
```

Restart the application so the in-memory index is rebuilt, run the same search again, and compare the results.

The point is not to prove one embedding provider is universally better. The point is to see that embedding-provider choice is independent from chat-provider choice.

## Run — Change `TopResults`

Change `Rag.TopResults`, restart the application, and repeat the same query.

Compare:

```text
number of chunks returned
which sources are included
how much context is supplied to chat
whether less-relevant material starts to appear
```

Explain the trade-off between too little context and too much context.

## Attack

Add a misleading sentence or instruction-like text to the new document and observe that retrieved text is context, not application authority.

Also change the configured embedding model and explain why the existing vectors cannot safely be reused even when the new model produces the same number of dimensions.

## Explain

1. Why are chat-provider and embedding-provider choices independent?
2. Why is RAG appropriate for internal prose while MCP is better for authoritative structured facts?
3. What makes a retrieval result relevant but not automatically trustworthy?
4. Why is the direct knowledge endpoint useful when diagnosing RAG behavior?
5. What changes when `TopResults` is increased?

## Lab Completion Criteria

```text
✓ direct semantic search returns useful existing knowledge
✓ new markdown knowledge is indexed
✓ intended question retrieves the new source
✓ paraphrased question still retrieves useful context
✓ MCP and RAG remain distinguishable grounding mechanisms
✓ chat and embedding provider choices remain independent
✓ changing embedding spaces rebuilds the index
✓ TopResults behavior can be observed and explained
```
