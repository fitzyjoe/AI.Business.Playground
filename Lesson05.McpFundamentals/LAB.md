# Lesson05 Lab — MCP Fundamentals

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add a new read-only MCP property search tool without adding an LLM.

## Predict

1. Who decides which MCP tool to call in this lesson?
2. Where do MCP input and output schemas come from?
3. Why should the tool depend on `IPropertyRepository` rather than the in-memory implementation?

## Run

Build Lesson05 and inspect the existing tools with MCP Inspector. Look at their names, descriptions, input schemas, and output schemas.

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

## Run

Reconnect MCP Inspector and verify that the tool is discovered without changing the Inspector. Exercise successful, zero-result, and multi-result searches.

## Attack

Try blank input, a huge requested result count, and an address that does not exist. Also verify that logging does not write arbitrary text to stdout when using stdio transport.

## Explain

1. Why did adding an MCP capability not require modifying an LLM?
2. What is the difference between a normal C# method and an MCP tool contract?
3. Why is `not found` usually a business result rather than a protocol failure?

## Lab Completion Criteria

```text
✓ new tool is discovered by MCP Inspector
✓ input/output schemas are generated
✓ tool depends on IPropertyRepository
✓ results are structured
✓ invalid input and zero results are distinguishable
✓ no LLM is involved
```
