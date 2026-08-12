# Lesson03.LlmConversations

## Stateful LLM Conversations

Lesson03 introduces multi-turn conversations.

The important change is that each request is no longer an isolated prompt. The application owns a conversation, stores its history, and sends that history back to the LLM on every turn.

```text
First message
    ↓
create Conversation
    ↓
send system prompt + user message
    ↓
LLM response
    ↓
persist user + assistant messages

Later message
    ↓
load Conversation
    ↓
send system prompt + full history + new user message
    ↓
LLM response
    ↓
persist new turn
```

The central lesson is:

> **The application owns conversation state. The LLM provider remains stateless between requests.**

---

## Learning Goals

By the end of Lesson03, you should understand:

- how a server-side application represents a conversation;
- how the first message can create a conversation implicitly;
- how later messages identify an existing conversation with a `conversationId`;
- why the application must resend prior messages to a stateless LLM API;
- how system, user, and assistant roles are represented;
- why conversation-level settings should be established when the conversation is created;
- why a pending user message should be sent to the LLM before it is persisted;
- why the user and assistant messages should be persisted only after the AI call succeeds;
- how provider-specific details remain behind `IAiProvider` and `IAiProviderFactory`;
- how normal application validation and exception handling apply to AI-backed APIs.

---

## API

Lesson03 exposes one endpoint:

```http
POST /api/message
```

There is no separate endpoint for creating a conversation.

If `conversationId` is omitted, the request starts a new conversation.

If `conversationId` is supplied, the request continues that conversation.

---

## Starting a Conversation

Example:

```bash
curl -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "My name is Joe. What is a good name for a woodworking shop?"
  }'
```

The response includes the generated conversation ID:

```json
{
  "conversationId": "...",
  "content": "...",
  "model": "gemma3:4b",
  "duration": "..."
}
```

Save the `conversationId`; it is how the client continues the same conversation.

---

## Continuing a Conversation

Send another message with the returned ID:

```bash
curl -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "<CONVERSATION_ID>",
    "content": "What name did I tell you?"
  }'
```

The application loads the stored conversation and sends the previous messages along with the new user message.

The model can answer from earlier context because the application supplied that context again.

---

## Conversation State

`Conversation` stores both configuration and message history:

```text
Id
SystemPrompt
Provider
Model
Temperature
MaxTokens
Messages
CreatedAt
UpdatedAt
```

The configuration belongs to the conversation rather than to each individual turn.

That means the first request may define settings such as:

```text
SystemPrompt
Provider
Model
Temperature
MaxTokens
```

Later requests supply only:

```text
ConversationId
Content
```

This prevents a conversation from silently changing model behavior halfway through its history.

---

## Request Validation

`MessageRequest` validates normal request fields before the handler runs.

Examples:

```text
Content is required.
Temperature must be between 0.0 and 2.0.
MaxTokens must be greater than 0.
```

When continuing an existing conversation, these fields are rejected:

```text
Provider
SystemPrompt
Model
Temperature
MaxTokens
```

They can only be supplied when starting a new conversation.

For example, this is invalid:

```json
{
  "conversationId": "<CONVERSATION_ID>",
  "content": "Continue the conversation.",
  "temperature": 1.5
}
```

The conversation's original temperature remains authoritative.

---

## Building the LLM Request

For every turn, `MessageHandler` constructs the provider request in this order:

```text
system message
    ↓
previous conversation messages
    ↓
current pending user message
```

Conceptually:

```text
System: You are a helpful assistant.
User: My name is Joe.
Assistant: Nice to meet you, Joe.
User: What name did I tell you?
```

The new user message is included in the LLM request before it is stored in the conversation repository.

---

## Persist Only After Success

Lesson03 deliberately does not persist the new user message before calling the LLM.

The flow is:

```text
build pending user message
    ↓
send full request to provider
    ↓
provider succeeds
    ↓
create assistant message
    ↓
persist user + assistant messages together
```

If the provider call fails, the conversation history is not left with a user message that never received an assistant response.

This keeps stored history aligned with completed conversation turns.

---

## Provider Abstraction

The conversation feature does not talk directly to Ollama.

```text
MessageHandler
    ↓
IAiProviderFactory
    ↓
IAiProvider
    ↓
OllamaProvider
    ↓
Ollama
```

`MessageHandler` chooses the provider through the conversation's `Provider` value:

```text
ollama
```

`AiProviderFactory` currently supports only Ollama, but the conversation feature is not coupled directly to `OllamaApiClient`.

---

## Ollama Configuration

The default Ollama settings are stored in `appsettings.json`:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "gemma3:4b"
  }
}
```

The application validates at startup that:

```text
Endpoint is an absolute URI
Model is not blank
```

A conversation may override the default model when it is first created.

---

## Optional Conversation Settings

A first request may include additional settings:

```bash
curl -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "Give me three concise names for a tax consulting AI assistant.",
    "systemPrompt": "You are a concise naming assistant.",
    "temperature": 0.4,
    "maxTokens": 100
  }'
```

If no system prompt is supplied, Lesson03 uses:

```text
You are a helpful assistant.
```

If no provider is supplied, Lesson03 uses:

```text
ollama
```

If no model is supplied, `OllamaProvider` uses the default model from configuration.

---

## In-Memory Repository

`InMemoryConversationRepository` stores conversations in a `ConcurrentDictionary<Guid, Conversation>`.

It is registered as a singleton so conversation state survives across HTTP requests while the application process is running.

This is intentionally simple for the lesson.

Restarting the application clears all conversations.

A production implementation would normally persist conversation state in a database or another durable store.

---

## Conversation Not Found

If a request supplies a conversation ID that does not exist, `MessageHandler` throws `ConversationNotFoundException`.

The centralized exception handler converts that into:

```http
404 Not Found
```

with a problem-details response containing the requested conversation ID.

Example:

```bash
curl -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "00000000-0000-0000-0000-000000000001",
    "content": "Hello"
  }'
```

This demonstrates that AI-backed endpoints should still use ordinary HTTP error semantics.

---

## Project Structure

```text
Lesson03.LlmConversations/
├── Features/
│   └── Conversations/
│       ├── Conversation.cs
│       ├── ConversationMessage.cs
│       ├── ConversationNotFoundException.cs
│       ├── ConversationRole.cs
│       ├── IConversationRepository.cs
│       ├── MessageController.cs
│       ├── MessageHandler.cs
│       ├── MessageRequest.cs
│       └── MessageResponse.cs
├── Infrastructure/
│   ├── Ai/
│   │   ├── AiChatRequest.cs
│   │   ├── AiChatResponse.cs
│   │   ├── AiProviderFactory.cs
│   │   ├── IAiProvider.cs
│   │   ├── IAiProviderFactory.cs
│   │   └── Providers/
│   │       ├── OllamaOptions.cs
│   │       └── OllamaProvider.cs
│   ├── Conversations/
│   │   └── InMemoryConversationRepository.cs
│   └── ErrorHandling/
│       └── ConversationNotFoundExceptionHandler.cs
├── Program.cs
├── appsettings.json
└── README.md
```

---

## Exercise 1 — Start a New Conversation

Send a POST without `conversationId`.

Expected:

```text
new Conversation created
conversationId returned
user + assistant messages persisted
```

---

## Exercise 2 — Continue the Conversation

Use the returned ID in a second request.

Ask something that depends on the first turn.

Example:

```text
Turn 1:
My favorite programming language is Java.

Turn 2:
What programming language did I say I prefer?
```

The second answer should demonstrate that prior history was supplied to the model.

---

## Exercise 3 — Separate Conversations

Start two conversations without supplying IDs.

Give each one different information.

Then continue each conversation independently.

Expected:

```text
Conversation A history does not appear in Conversation B.
Conversation B history does not appear in Conversation A.
```

---

## Exercise 4 — Custom System Prompt

Start a conversation with:

```json
{
  "content": "Explain dependency injection.",
  "systemPrompt": "Explain technical topics using short analogies."
}
```

Continue that conversation and observe that the same system prompt remains part of every turn.

---

## Exercise 5 — Conversation Settings Are Immutable

Start a conversation with a temperature.

Then try to continue it while supplying a different temperature.

Expected:

```http
400 Bad Request
```

The same applies to:

```text
Provider
SystemPrompt
Model
MaxTokens
```

---

## Exercise 6 — Unknown Conversation

Send a valid but nonexistent `conversationId`.

Expected:

```http
404 Not Found
```

---

## Exercise 7 — Provider Failure

Stop Ollama or otherwise make the provider request fail.

Send a new message to an existing conversation.

The important behavior is:

```text
pending user message is not persisted
assistant message is not persisted
```

The conversation should contain only completed turns.

---

## Important Design Distinctions

### Conversation state vs. model state

The model does not remember the previous HTTP request on its own.

The application recreates context by sending conversation history again.

### System prompt vs. conversation messages

The system prompt is stored as conversation configuration and inserted into each provider request.

It is not duplicated into `Conversation.Messages`.

### Pending turn vs. persisted history

The current user message participates in the LLM request immediately, but is persisted only after the provider returns successfully.

### Application abstraction vs. provider implementation

`MessageHandler` works with `IAiProvider` rather than with Ollama-specific request types.

---

## Deliberately Out of Scope

Lesson03 does not add:

- a database-backed conversation repository;
- authentication or per-user ownership;
- conversation listing or deletion endpoints;
- conversation titles;
- token-window truncation or summarization;
- retrieval-augmented generation;
- tool calling;
- message editing;
- branching conversations;
- retry queues;
- distributed concurrency control;
- provider failover.

Those concerns are separate from the foundational lesson: owning multi-turn conversation state in the application.

---

## Lesson03 Acceptance Criteria

Lesson03 is complete when:

```text
✓ POST /api/message starts a conversation when no conversationId is supplied
✓ the response returns the conversationId
✓ later requests can continue the same conversation
✓ full prior history is sent to the LLM
✓ the system prompt is included on every turn
✓ conversation-level settings are established at creation
✓ later requests cannot replace conversation-level settings
✓ user and assistant messages are persisted only after a successful AI response
✓ unknown conversation IDs return 404
✓ the AI provider remains behind IAiProvider / IAiProviderFactory
✓ in-memory conversation state survives across HTTP requests
```

---

## What Lesson03 Is Really Teaching

The lesson is not simply:

> How to call a chat model more than once.

The lesson is:

> **How an application owns conversation identity, configuration, and history while treating the LLM provider as a stateless dependency.**
