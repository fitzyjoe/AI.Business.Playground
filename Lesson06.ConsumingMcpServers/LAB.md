# Lesson06 Lab — Consuming MCP Servers

This lab is the hands-on companion to [README.md](README.md).

## Goal

Make the Lesson06 application discover the Lesson05 MCP tools and demonstrate that the model can select the new address-search tool when appropriate.

## Predict

1. Does Ollama or OpenAI connect directly to the MCP server?
2. Which layer discovers MCP tools?
3. Why can the same MCP tool work with either chat provider?

## Run the Starter

Build Lesson05 and start Lesson06. The workshop starter intentionally establishes the MCP connection but leaves the discovered `Tools` collection empty.

Ask one general question that requires no property data, then ask for an authoritative property lookup. Observe the difference when the model has no MCP property tools available.

## Build — Discover and Expose the MCP Tools

Complete `PropertyMcpClient.InitializeAsync` so that it discovers the Lesson05 tools dynamically and exposes them through `PropertyMcpClient.Tools`.

Then make sure the new `search_properties_by_address` tool from the Lesson05 lab is available to the model without adding a direct code reference to Lesson05.

Requirements:

- Lesson06 must discover tools through MCP rather than referencing Lesson05 implementation code;
- the discovered tools must be made available through the provider-neutral chat abstraction;
- both Ollama and OpenAI conversations must be able to use them;
- no provider-specific MCP integration should be added.

## Run — Make the Model Select the New Tool

First repeat the property lookup from the starter run and verify that grounding now works.

Then ask a question that cannot be answered from a parcel number but can be answered from a partial street address. Observe whether the model chooses `search_properties_by_address` and uses its result.

Also ask a general property-tax question and verify that a tool call is not required.

## Attack

- Ask for an address that does not exist.
- Ask for an intentionally vague address.
- Trigger the new tool's validation error.
- Compare tool selection between Ollama and OpenAI.

## Explain

1. Why does the AI provider not need to "speak MCP"?
2. Which part of the system decides what tools are available?
3. Which part decides whether a tool would help with the current request?
4. Why did Lesson06 not need a compile-time dependency on Lesson05 to see the new tool?

## Lab Completion Criteria

```text
✓ Lesson06 discovers tools dynamically through MCP
✓ the new address-search tool appears without a direct Lesson05 code reference
✓ both providers can use the discovered tools
✓ model can select the new tool for an address-based question
✓ ordinary questions can still be answered without a tool
```
