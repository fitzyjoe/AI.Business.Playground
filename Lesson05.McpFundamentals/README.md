# Lesson05.McpFundamentals

## Building a Property Records MCP Server

Lesson05 introduces the **Model Context Protocol (MCP)** by building a small, read-only MCP server in C#.

The server exposes property-record lookup capabilities that can be discovered and invoked by any compatible MCP client.

Unlike the previous lessons, this project does **not** call an LLM.

The goal is to first understand and test the MCP server independently. A later lesson will connect an LLM to the server and allow the model to select and invoke MCP tools.

## Learning Goals

By the end of this lesson, you should understand:

- what MCP is;
- the roles of an MCP host, client, and server;
- what an MCP tool is;
- how MCP tools differ from ordinary C# methods;
- how to build an MCP server using the C# MCP SDK;
- how tool input and output schemas are generated from C# types;
- how to expose business services through MCP;
- how dependency injection works with MCP tools;
- how to return structured tool results;
- the difference between invalid input and a valid "not found" result;
- why standard output must be reserved for the MCP protocol when using stdio;
- how to test an MCP server using MCP Inspector.

## Architecture

The Lesson05 architecture is:

```text
MCP Inspector
      ↓
MCP Client Connection
      ↓
Lesson05.McpFundamentals
      ↓
PropertyTools
      ↓
IPropertyRepository
      ↓
InMemoryPropertyRepository
```

The MCP Inspector acts as the client while developing and testing the server.

In a later lesson, the Inspector will be replaced by an AI-enabled application.

## MCP Roles

### MCP Host

The host is the application that wants to use MCP capabilities.

Examples might eventually include:

- an AI chat application;
- an IDE;
- an agent;
- a desktop application.

For this lesson, the MCP Inspector effectively plays the host role.

### MCP Client

An MCP client manages the connection between a host and an MCP server.

It can:

- initialize the connection;
- discover tools;
- inspect tool schemas;
- invoke tools;
- receive tool results.

### MCP Server

The MCP server exposes capabilities using the MCP standard.

Lesson05 is an MCP server.

It exposes two property-related tools.

## Tools

Lesson05 exposes two read-only MCP tools.

### `lookup_property_by_parcel`

Looks up one property using an exact parcel number.

Example input:

```json
{
  "parcelNumber": "0304-12-0042"
}
```

Example successful result:

```json
{
  "found": true,
  "property": {
    "parcelNumber": "0304-12-0042",
    "ownerName": "ABC Commercial Holdings LLC",
    "propertyAddress": "1200 Main Street, McLean, VA 22101",
    "jurisdiction": "Fairfax County, Virginia",
    "taxYear": 2026,
    "assessedValue": 8450000
  }
}
```

A parcel that does not exist is still a valid tool request.

Example:

```json
{
  "found": false,
  "property": null
}
```

"Not found" is a normal business result rather than a server failure.

### `search_properties_by_owner`

Searches properties using all or part of an owner's name.

Example input:

```json
{
  "ownerName": "ABC Commercial",
  "maxResults": 5
}
```

Example result:

```json
{
  "count": 2,
  "properties": [
    {
      "parcelNumber": "0304-12-0042",
      "ownerName": "ABC Commercial Holdings LLC",
      "propertyAddress": "1200 Main Street, McLean, VA 22101",
      "jurisdiction": "Fairfax County, Virginia",
      "taxYear": 2026,
      "assessedValue": 8450000
    },
    {
      "parcelNumber": "0304-12-0043",
      "ownerName": "ABC Commercial Holdings LLC",
      "propertyAddress": "1210 Main Street, McLean, VA 22101",
      "jurisdiction": "Fairfax County, Virginia",
      "taxYear": 2026,
      "assessedValue": 5275000
    }
  ]
}
```

## Project Structure

```text
Lesson05.McpFundamentals/
├── Features/
│   └── Properties/
│       ├── IPropertyRepository.cs
│       ├── PropertyLookupResult.cs
│       ├── PropertyRecord.cs
│       ├── PropertySearchResult.cs
│       └── PropertyTools.cs
│
├── Infrastructure/
│   └── Properties/
│       └── InMemoryPropertyRepository.cs
│
├── Program.cs
├── README.md
└── Lesson05.McpFundamentals.csproj
```

The tool class depends on `IPropertyRepository`, not directly on the in-memory implementation.

This means the storage implementation could later be replaced by:

- SQL Server;
- Azure SQL;
- an internal REST API;
- another business data source.

The MCP tool contract would not need to change.

## Running the Server

Build the project:

```bash
dotnet build
```

The compiled executable will be located under:

```text
bin/Debug/net10.0/
```

The MCP server uses **standard input/output transport**.

Normally, you do not launch it directly for testing. Instead, an MCP client such as MCP Inspector launches the process and communicates with it.

## MCP Inspector

MCP Inspector is used to test the server without involving an LLM.

It serves a role similar to Postman or curl for an HTTP API.

It can:

- connect to the server;
- list tools;
- inspect generated schemas;
- execute tool calls;
- inspect structured results.

Launch the Inspector:

```bash
npx @modelcontextprotocol/inspector \
  "./bin/Debug/net10.0/Lesson05.McpFundamentals"
```

If you modify the C# project, remember:

```bash
dotnet build
```

Then disconnect and reconnect the Inspector.

The Inspector launches the compiled executable. It does not automatically rebuild the project when source files change.

## Inspecting Tool Definitions

The Inspector CLI can return the full tool definitions:

```bash
npx @modelcontextprotocol/inspector --cli \
  "./bin/Debug/net10.0/Lesson05.McpFundamentals" \
  --method tools/list
```

The result includes:

- tool name;
- title;
- description;
- input schema;
- output schema;
- tool annotations.

For example, the parcel lookup input schema is generated automatically from the C# method signature:

```json
{
  "type": "object",
  "properties": {
    "parcelNumber": {
      "description": "The exact parcel number, including punctuation. Example: 0304-12-0042.",
      "type": "string"
    }
  },
  "required": [
    "parcelNumber"
  ]
}
```

The output schema is generated from `PropertyLookupResult` and `PropertyRecord`.

## Structured Tool Results

The tools use:

```csharp
UseStructuredContent = true
```

This causes MCP to advertise an output schema derived from the C# return type.

For example:

```csharp
public sealed record PropertyLookupResult(
    bool Found,
    PropertyRecord? Property);
```

produces a structured MCP result rather than a manually formatted string.

This is similar to Lesson04, where C# types were used to define structured LLM output.

The difference is:

```text
Lesson04
C# type
    ↓
JSON Schema
    ↓
Constrains LLM output

Lesson05
C# method + return type
    ↓
MCP input/output schemas
    ↓
Defines tool contract
```

## Nullable vs Optional Properties

A useful issue encountered while building this lesson involves nullable properties.

Consider:

```csharp
public sealed record PropertyLookupResult(
    bool Found,
    PropertyRecord? Property);
```

The generated MCP schema may say:

```json
{
  "required": [
    "found",
    "property"
  ]
}
```

while also allowing:

```json
"property": null
```

This means:

```text
property is required
but
property's value may be null
```

That is different from making the property optional.

A not-found result should therefore look like:

```json
{
  "found": false,
  "property": null
}
```

rather than:

```json
{
  "found": false
}
```

Serializer configuration must be consistent with the advertised MCP schema.

## Standard Input/Output Transport

This server uses stdio transport.

The streams have specific purposes:

```text
stdin
    MCP requests sent to the server

stdout
    MCP protocol responses

stderr
    logging and diagnostics
```

Because stdout carries MCP protocol messages, do not use:

```csharp
Console.WriteLine("Server started");
```

inside the server.

Writing arbitrary text to stdout can corrupt the MCP protocol stream.

Use `ILogger` instead.

The application configures logging so that logs are written to stderr.

## Dependency Injection

`PropertyTools` receives the repository through dependency injection:

```csharp
public sealed class PropertyTools(
    IPropertyRepository _propertyRepository)
```

The repository is registered in `Program.cs`:

```csharp
builder.Services.AddSingleton<
    IPropertyRepository,
    InMemoryPropertyRepository>();
```

The MCP SDK creates the tool class and resolves its dependencies from the service container.

This keeps MCP concerns separate from business and data-access concerns.

## Read-Only Tools

The lesson intentionally begins with read-only tools.

The MCP attributes identify these tools as read-only:

```csharp
ReadOnly = true
```

Read-only capabilities are a safer starting point than tools that:

- modify database records;
- send emails;
- create payments;
- delete data;
- trigger workflows.

Write operations and approval boundaries will be introduced later in the course.

## Important Distinction: MCP Is Not the LLM

Lesson05 contains no LLM.

The flow is currently:

```text
You
 ↓
MCP Inspector
 ↓
MCP Server
 ↓
Property Repository
```

The MCP server does not decide which tool should be called.

It simply:

- advertises available capabilities;
- defines their schemas;
- executes valid tool requests;
- returns results.

A later lesson will introduce an LLM that can decide which MCP tool to invoke.
