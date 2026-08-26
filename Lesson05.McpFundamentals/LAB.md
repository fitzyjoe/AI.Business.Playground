# Lesson05 Lab — MCP Fundamentals

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add a new read-only MCP property search tool without adding an LLM.

## Predict

1. Who decides which MCP tool to call in this lesson?
2. Where do MCP input and output schemas come from?
3. Why should the tool depend on `IPropertyRepository` rather than the in-memory implementation?
4. Does an in-memory repository require its sample records to be hard-coded in C#?

## Run

Build Lesson05 and inspect the existing tools with MCP Inspector. Look at their names, descriptions, input schemas, and output schemas.

The sample data is defined in `Features/Properties/properties.json` and loaded into the singleton `InMemoryPropertyRepository`. Inspect the file so you know what data is available for testing.

Use the existing owner-search tool to verify that the larger dataset is loaded. For example, searching for:

```text
Reston Tech Holdings
```

should return five records.

## Build — Add `search_properties_by_address`

Add a new MCP tool that searches property records by all or part of an address.

Requirements:

- expose it through MCP;
- keep it read-only;
- accept an address search string and bounded maximum result count;
- use `IPropertyRepository` rather than reaching directly into the in-memory repository;
- return structured results;
- distinguish invalid input from a valid search that returns zero properties.

Add whatever repository method is needed to support the tool.

Do not read `properties.json` directly from the tool. The repository owns data access; the MCP tool owns the protocol-facing capability.

## Run

Reconnect MCP Inspector and verify that the tool is discovered without changing the Inspector.

Exercise all three result shapes:

- search for an exact address such as `1100 Innovation Drive` and expect one record;
- search for `Innovation Drive` and expect multiple records;
- search for an address that does not exist and expect a valid zero-result response.

The five `Innovation Drive` records are intentional so the address-search exercise has a useful multi-result case.

## Attack

Try blank input, a huge requested result count, and an address that does not exist. Also verify that logging does not write arbitrary text to stdout when using stdio transport.

## Explain

1. Why did adding an MCP capability not require modifying an LLM?
2. What is the difference between a normal C# method and an MCP tool contract?
3. Why is `not found` usually a business result rather than a protocol failure?
4. Why can the repository still be called `InMemoryPropertyRepository` even though its initial records come from a JSON file?

## Lab Completion Criteria

```text
✓ new tool is discovered by MCP Inspector
✓ input/output schemas are generated
✓ tool depends on IPropertyRepository
✓ results are structured
✓ invalid input and zero results are distinguishable
✓ exact and multi-result address searches work against the JSON-backed sample data
✓ no LLM is involved
```
