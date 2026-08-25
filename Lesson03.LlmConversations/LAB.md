# Lesson03 Lab — LLM Conversations

This lab is the hands-on companion to [README.md](README.md).

## Goal

Implement the path that continues an existing conversation: load the conversation, construct the next provider request from prior state plus the new user message, and persist the completed turn only after a successful model response.

## Predict

1. Does the LLM provider remember previous HTTP requests?
2. Which object owns provider/model/temperature settings after a conversation begins?
3. Should the new user message be persisted before or after the provider succeeds?
4. What should happen if a caller tries to change provider on an existing conversation?

## Run the Starter

Start a new conversation and save the returned `conversationId`. Then attempt a follow-up using that ID. The workshop starter intentionally leaves the continuation path incomplete.

## Build — Continue a Conversation

Complete the existing-conversation path so that it:

- looks up the conversation by ID;
- rejects unknown IDs appropriately;
- rejects attempts to replace conversation-level settings;
- rebuilds the LLM request with system prompt, prior history, and the pending user message;
- uses the provider stored on the conversation;
- persists both the user and assistant messages only after provider success.

## Run — Prove Memory

Tell the model a fact in the first turn and ask for it later without repeating it. Also start one Ollama conversation and one OpenAI conversation and prove each retains its original provider.

## Attack

- Try changing provider on a later turn.
- Try changing temperature on a later turn.
- Use an unknown conversation ID.
- Make the selected provider unavailable during a follow-up and verify a half-completed turn is not persisted.

## Explain

1. Why is conversation memory application state rather than provider state?
2. Why are conversation-level model settings immutable after creation in this design?
3. Why is "persist only after success" important?

## Lab Completion Criteria

```text
✓ existing conversations can be continued
✓ prior history reaches the selected provider
✓ provider selection remains fixed
✓ invalid setting changes are rejected
✓ failed provider calls do not leave partial turns
```
