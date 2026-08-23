# Lesson03.LlmConversations

## Stateful LLM Conversations

Lesson03 introduces multi-turn conversations.

The application now owns a conversation, stores its history, and sends that history back to the selected LLM provider on every turn.

```text
First message
    ↓
create Conversation
    ↓
send system prompt + user message
    ↓
selected provider responds
    ↓
persist user + assistant messages

Later message
    ↓
load Conversation
    ↓
send system prompt + full history + new user message
    ↓
selected provider responds
    ↓
persist new turn
```

The central lesson is:

> **The application owns conversation state. The LLM provider remains stateless between requests.**

Lesson03 carries forward the Ollama/OpenAI provider choice introduced in Lesson02.

---

## Learning Goals

By the end of Lesson03, you should understand:

- how a server-side application represents a conversation;
- how the first message can create a conversation implicitly;
- how later messages identify an existing conversation with a `conversationId`;
- why the application must resend prior messages to a stateless LLM API;
- how system, user, and assistant roles are represented;
- why provider, model, temperature, and other settings belong to the conversation;
- why the selected provider should not silently change midway through a conversation;
- why pending messages should be persisted only after the AI call succeeds;
- how both Ollama and OpenAI remain behind the same application abstraction.

---

## API

Lesson03 exposes:

```http
POST /api/message
```

There is no separate conversation-creation endpoint.

If `conversationId` is omitted, the request starts a new conversation. If it is supplied, the request continues the existing conversation.

---

## Starting a Conversation

Default provider (`ollama`):

```bash
curl -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "My name is Joe. What is a good name for a woodworking shop?"
  }'
```

Starting an OpenAI conversation:

```bash
curl -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "content": "My name is Joe. What is a good name for a woodworking shop?",
    "provider": "openai"
  }'
```

The response includes a `conversationId`, generated content, the model, and duration.

---

## Continuing a Conversation

```bash
curl -X POST \
  http://localhost:5000/api/message \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "<CONVERSATION_ID>",
    "content": "What name did I tell you?"
  }'
```

The provider is not supplied again. The application uses the provider stored on the conversation.

---

## Conversation State

`Conversation` stores configuration and history:

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

The first request may establish:

```text
SystemPrompt
Provider
Model
Temperature
MaxTokens
```

Later requests supply only the conversation ID and new content.

This prevents a conversation from quietly switching from Ollama to OpenAI, changing model, or changing sampling behavior halfway through its history.

---

## Provider Abstraction

```text
MessageHandler
    ↓
Conversation.Provider
    ↓
IAiProviderFactory
    ↓
IAiProvider
    ├── OllamaProvider
    └── OpenAiProvider
```

Both providers receive the same application-level `AiChatRequest`, which includes conversation messages and optional model controls.

The provider implementations translate those values to their respective SDKs.

---

## Configuration

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "gemma3:4b"
  },
  "OpenAI": {
    "Model": "gpt-5.2"
  }
}
```

To use OpenAI:

```bash
export OPENAI_AI_BUSINESS_PLAYGROUND="your-api-key"
```

The key is required only when an OpenAI conversation actually executes an OpenAI request.

---

## Building the LLM Request

For every turn, `MessageHandler` constructs the request in this order:

```text
system message
    ↓
previous conversation messages
    ↓
current pending user message
```

The new user message participates in the provider request immediately, but it is not persisted until the provider returns successfully.

---

## Persist Only After Success

```text
build pending user message
    ↓
call selected provider
    ↓
provider succeeds
    ↓
create assistant message
    ↓
persist user + assistant messages
```

If either Ollama or OpenAI fails, the conversation is not left with a half-completed turn.

---

## Request Validation

When continuing a conversation, these settings are rejected:

```text
Provider
SystemPrompt
Model
Temperature
MaxTokens
```

They are conversation-level settings and can only be supplied when the conversation starts.

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
│       ├── InMemoryConversationRepository.cs
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
│   │       ├── OllamaProvider.cs
│   │       ├── OpenAiOptions.cs
│   │       └── OpenAiProvider.cs
│   └── ErrorHandling/
│       └── ConversationNotFoundExceptionHandler.cs
├── Program.cs
├── appsettings.json
└── README.md
```

`InMemoryConversationRepository` is co-located with the conversation feature because it is the concrete storage implementation used by that feature in this lesson.

---

## Useful Exercises

### Provider Persistence

Start one conversation with `provider = "openai"` and another with `provider = "ollama"`. Continue both without supplying a provider.

Each should continue using its original provider.

### Conversation Memory

Tell the model a fact in the first turn, then ask for it in a later turn.

### Immutable Settings

Try changing provider or temperature on a later turn. Expect request validation to reject it.

### Provider Failure

Make the selected provider unavailable and send a message to an existing conversation. Verify that the failed turn is not persisted.

---

## Deliberately Out of Scope

Lesson03 does not add:

- a durable database-backed conversation repository;
- authentication or per-user ownership;
- conversation listing or deletion;
- token-window truncation or summarization;
- RAG;
- tools;
- provider failover;
- distributed concurrency control.

---

## Lesson03 Acceptance Criteria

```text
✓ POST /api/message starts a conversation when no conversationId is supplied
✓ later requests can continue the same conversation
✓ full prior history is sent to the selected provider
✓ both Ollama and OpenAI can back a conversation
✓ provider selection is stored when the conversation is created
✓ later requests cannot replace conversation-level settings
✓ user and assistant messages are persisted only after a successful response
✓ unknown conversation IDs return 404
✓ provider-specific SDK code remains behind IAiProvider / IAiProviderFactory
✓ in-memory conversation state survives across HTTP requests
```

---

## What Lesson03 Is Really Teaching

> **How an application owns conversation identity, configuration, and history while treating the selected LLM provider as a stateless dependency.**
