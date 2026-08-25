# Lesson07 Lab — Retrieval-Augmented Generation

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add a new company knowledge document and make a question reliably retrieve the relevant chunk without changing the distinction between RAG and MCP.

## Predict

1. When should the application use MCP instead of RAG?
2. Why must document and query embeddings come from the same embedding space?
3. What happens to the current in-memory index when the application restarts?

## Run

Run the existing knowledge search and ask a question covered by the current documents. Inspect which chunks are returned and compare that behavior with a property-data question grounded through MCP.

## Build — Add New Internal Knowledge

Create a new markdown document describing an internal business procedure not already represented in the knowledge base.

Then implement or tune the retrieval behavior so a natural user question about that procedure returns the intended source among the top results. You may adjust chunking, document wording, query wording, or `TopResults`, but preserve the provider-neutral embedding abstraction.

## Run — Verify Retrieval

Test:

- a question that should retrieve the new document;
- a semantically similar paraphrase;
- an unrelated question that should not depend on the new document;
- one chat-provider/embedding-provider combination where the two providers differ.

## Attack

Add a misleading sentence or instruction-like text to the new document and observe that retrieved text is context, not application authority. Also change the configured embedding model and explain why the index must be rebuilt.

## Explain

1. Why are chat-provider and embedding-provider choices independent?
2. Why is RAG appropriate for internal prose while MCP is better for authoritative structured facts?
3. What makes a retrieval result relevant but not automatically trustworthy?

## Lab Completion Criteria

```text
✓ new markdown knowledge is indexed
✓ intended question retrieves the new source
✓ paraphrased question still retrieves useful context
✓ chat and embedding provider choices remain independent
✓ MCP and RAG continue to serve different grounding roles
```
