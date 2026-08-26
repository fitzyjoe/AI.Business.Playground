# AI.Business.Playground

A progressive set of C#/.NET lessons for building business applications with LLMs. The course starts with a single prompt and builds toward conversations, structured outputs, MCP, RAG, safe writes, agents, anomaly investigation, and production controls.

## Lesson Progression

| Lesson | Topic | Coding challenge |
| --- | --- | --- |
| 01 | Basic Prompting | Complete the `Handler` / `IAiProvider` path so a prompt reaches Ollama and returns model + duration. |
| 02 | Controlling LLM Behavior | Add `MaxTokens` through the application abstraction and both providers. |
| 03 | LLM Conversations | Implement the existing-conversation continuation path. |
| 04 | Structured Outputs | Add a third correspondence type with fields and deterministic validation. |
| 05 | MCP Fundamentals | Add a new property lookup/search MCP tool. |
| 06 | Consuming MCP Servers | Make the AI application consume and select the new MCP tool. |
| 07 | Retrieval-Augmented Generation | Add company knowledge and make an intended question retrieve it. |
| 08 | Safe Write Operations | Complete rejection/lifecycle behavior and try to make the LLM bypass it. |
| 09 | Agents | Add a new bounded agent tool and observe when the agent chooses it. |
| 10 | Monitoring and Anomaly Detection | Add a new metric/anomaly scenario that triggers bounded AI investigation. |
| 11 | Production AI Platform | Add `MaxConversationTurns`, a deterministic test, and a live AI evaluation. |

## How to Use the Lessons

Each lesson now has two complementary documents:

- **`README.md`** — architecture, design rationale, implementation details, configuration, and reference examples for the completed solution.
- **`LAB.md`** — the guided hands-on exercise. Labs use the sequence **Predict → Run → Build → Attack → Explain** and include a focused coding assignment.
