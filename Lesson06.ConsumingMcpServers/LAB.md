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

## Run — No Tool Needed

Ask:

```text
What does assessed value mean?
```

The model should be able to answer without a property lookup.

## Run — Exact Parcel Lookup

Ask:

```text
What is the assessed value of parcel 0304-12-0042?
```

The selected provider should be able to call `lookup_property_by_parcel` and answer from Lesson05 data.

## Run — Owner Search

Ask:

```text
What properties are owned by ABC Commercial Holdings?
```

The provider should be able to select `search_properties_by_owner`.

## Run — Make the Model Select the New Address Tool

Ask a question that cannot be answered from a parcel number but can be answered from a partial street address. For example:

```text
Find the property record for the parcel on Maple Avenue and tell me its assessed value.
```

Use an address fragment that actually exists in the Lesson05 data. Observe whether the model chooses `search_properties_by_address` and uses its result.

## Attack — Property Not Found

Ask for a nonexistent parcel and verify that the answer does not invent property data.

## Attack — Tool Validation Error

Ask for more results than Lesson05 permits. The tool should return its deterministic validation error to the model.

Also try:

- an address that does not exist;
- an intentionally vague address;
- invalid input to the new address tool.

## Run — Multi-Turn Conversation

Start a conversation with either provider, then use the returned `conversationId` for a follow-up such as:

```text
What other properties do they own?
```

Verify that normal conversation history still works while MCP tools are available.

## Compare Providers

Repeat an MCP-backed request once with Ollama and once with OpenAI. Compare whether both providers can use the same discovered MCP tool set even if they make different tool-selection decisions.

## Explain

1. Why does the AI provider not need to "speak MCP"?
2. Which part of the system decides what tools are available?
3. Which part decides whether a tool would help with the current request?
4. Why did Lesson06 not need a compile-time dependency on Lesson05 to see the new tool?
5. Why can a tool validation error be useful to the model rather than being hidden from it?

## Lab Completion Criteria

```text
✓ Lesson06 discovers tools dynamically through MCP
✓ the new address-search tool appears without a direct Lesson05 code reference
✓ both providers can use the discovered tools
✓ model can select the new tool for an address-based question
✓ ordinary questions can still be answered without a tool
✓ missing data does not cause invented property facts
✓ tool validation errors remain visible to the model
✓ multi-turn conversation still works
```
