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

Use the start/continue curl examples in [README.md](README.md) to create a conversation and save the returned `conversationId`.

Then attempt a follow-up using that ID. The workshop starter intentionally leaves the continuation path incomplete, so observe what fails before fixing it.

## Build — Continue a Conversation

Complete the existing-conversation path so that it:

- looks up the conversation by ID;
- rejects unknown IDs appropriately;
- rejects attempts to replace conversation-level settings;
- rebuilds the LLM request with system prompt, prior history, and the pending user message;
- uses the provider stored on the conversation;
- persists both the user and assistant messages only after provider success.

## Run — Conversation Memory

Tell the model a fact in the first turn, then ask for it in a later turn without repeating it.

For example, start with:

```text
My favorite programming language is Java. Remember that for later.
```

Then continue the same conversation with:

```text
What programming language did I tell you I prefer?
```

The answer should depend on application-owned conversation history being sent back to the provider.

## Run — Provider Persistence

Start one conversation with:

```text
provider = "openai"
```

and another with:

```text
provider = "ollama"
```

Continue both conversations without supplying a provider on the later request.

Each should continue using the provider selected when the conversation was created.

## Attack — Immutable Settings

Try changing provider or temperature on a later turn. Request validation should reject the attempt because these are conversation-level settings.

Also try changing one of the other conversation-owned settings:

```text
SystemPrompt
Model
MaxTokens
```

The continuation request should not be allowed to silently change the conversation's configuration.

## Attack — Unknown Conversation

Send a continuation request with a made-up `conversationId` and verify that the application returns the expected not-found behavior.

## Attack — Provider Failure

Make the selected provider unavailable and send a message to an existing conversation.

Verify that the failed turn is not persisted. When the provider becomes available again, the conversation should not contain a user message that never received a corresponding assistant response.

## Explain

1. Why is conversation memory application state rather than provider state?
2. Why are conversation-level model settings immutable after creation in this design?
3. Why is "persist only after success" important?
4. Why can two conversations in the same application safely use different providers?

## Lab Completion Criteria

```text
✓ existing conversations can be continued
✓ prior history reaches the selected provider
✓ provider selection remains fixed
✓ conversation memory works across HTTP requests
✓ invalid setting changes are rejected
✓ unknown conversation IDs are handled appropriately
✓ failed provider calls do not leave partial turns
```
