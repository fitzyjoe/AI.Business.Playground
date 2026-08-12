# Lesson04.StructuredOutputs

## Structured Outputs for OCR Correspondence

Lesson04 takes plain text produced by OCR and asks an LLM to do two things at once:

```text
classify the document
+
extract structured fields
```

The lesson then constrains the model to a generated JSON Schema, deserializes the response into strongly typed C# records, and performs deterministic normalization and business validation.

The central lesson is:

> **An LLM can turn messy natural-language input into structured application data, but the application still owns schema enforcement and business validation.**

---

## Business Scenario

Assume paper correspondence has already been scanned and converted to plain text by OCR.

Lesson04 does **not** implement scanning or OCR. Its input is already-extracted text.

The application recognizes exactly two document types:

```text
HearingSchedule
ValueNotice
```

For both document types, the model extracts common property/customer fields:

```text
CustomerName
PropertyAddress
ParcelNumber
```

A hearing schedule also extracts:

```text
HearingDate
HearingTime
Location
```

A value notice also extracts:

```text
TaxYear
AssessedValue
ProtestDeadline
```

---

## Learning Goals

By the end of Lesson04, you should understand:

- how structured output differs from free-form prompting;
- how to generate a JSON Schema from a C# type;
- how to send that schema to an LLM as a response constraint;
- how schema-constrained output improves deserialization reliability;
- how classification and extraction can be represented in one strongly typed result;
- why schema validation and business validation are different concerns;
- why nullable fields are useful when OCR text is missing or ambiguous;
- how to reject malformed or semantically invalid model output with normal C# code.

---

## Request Flow

```text
OCR text
    ↓
POST /api/correspondence/analyze
    ↓
AnalyzeCorrespondenceHandler
    ↓
generate JSON Schema from CorrespondenceAnalysis
    ↓
Ollama request with structured response format
    ↓
JSON response
    ↓
strict deserialization
    ↓
NormalizeAndValidate()
    ↓
AnalyzeCorrespondenceResponse
```

---

## API

Lesson04 exposes one endpoint:

```http
POST /api/correspondence/analyze
```

Example request body:

```json
{
  "documentText": "...OCR text..."
}
```

The response contains:

```text
Analysis
Model
Duration
```

`Analysis` is a strongly typed `CorrespondenceAnalysis` object.

---

## Project Structure

```text
Lesson04.StructuredOutputs/
├── Features/
│   └── Correspondence/
│       ├── AnalyzeCorrespondenceController.cs
│       ├── AnalyzeCorrespondenceHandler.cs
│       ├── AnalyzeCorrespondenceRequest.cs
│       ├── AnalyzeCorrespondenceResponse.cs
│       ├── CorrespondenceAnalysis.cs
│       ├── CorrespondenceType.cs
│       ├── HearingScheduleDetails.cs
│       ├── InvalidCorrespondenceAnalysisException.cs
│       ├── StructuredOutputException.cs
│       └── ValueNoticeDetails.cs
├── Infrastructure/
│   └── Ai/
│       ├── AiChatMessage.cs
│       ├── AiChatRequest.cs
│       ├── AiChatResponse.cs
│       ├── AiMessageRole.cs
│       ├── AiProviderFactory.cs
│       ├── IAiProvider.cs
│       ├── IAiProviderFactory.cs
│       ├── StructuredOutputJson.cs
│       └── Providers/
│           ├── OllamaOptions.cs
│           └── OllamaProvider.cs
├── Samples/
│   ├── hearing-schedule.txt
│   ├── hearing-schedule-missing-time.txt
│   └── value-notice.txt
├── Program.cs
├── appsettings.json
└── README.md
```

---

## The Structured Result

The top-level structured result is `CorrespondenceAnalysis`.

Conceptually:

```text
CorrespondenceAnalysis
├── DocumentType
├── CustomerName
├── PropertyAddress
├── ParcelNumber
├── HearingSchedule
└── ValueNotice
```

The selected `DocumentType` determines which detail object should contain data.

For a hearing schedule:

```text
DocumentType = HearingSchedule
HearingSchedule != null
ValueNotice = null
```

For a value notice:

```text
DocumentType = ValueNotice
ValueNotice != null
HearingSchedule = null
```

`NormalizeAndValidate()` enforces this relationship after deserialization.

---

## Classification Without Model Training

The model is not trained specifically on these sample documents.

Instead, the system prompt tells the model that every document belongs to exactly one of two known categories:

```text
HearingSchedule
ValueNotice
```

The LLM uses its pretrained language understanding to classify the OCR text into one of those categories.

This is a zero-shot classification pattern: the categories are described in the prompt rather than learned through additional model training.

---

## Generating the JSON Schema

The handler generates a schema directly from the C# type:

```csharp
var schema = StructuredOutputJson.Options.GetJsonSchemaAsNode(typeof(CorrespondenceAnalysis));
```

That schema is used in two ways:

```text
1. It is supplied to Ollama as the structured response format.
2. It is also included in the user prompt for clarity.
```

This keeps the expected model output aligned with the application's actual C# type instead of maintaining a separate hand-written schema.

---

## Structured Output Request

The AI request is intentionally deterministic:

```text
Temperature = 0
Stream = false
ResponseFormat = generated JSON Schema
```

The system prompt also instructs the model to:

```text
extract only information supported by the OCR text
use null for missing or uncertain values
never invent identifiers, dates, values, or deadlines
return only the requested structured result
```

The goal is extraction, not creativity.

---

## Strict JSON Deserialization

`StructuredOutputJson` configures `System.Text.Json` more strictly than the default web serializer.

Important settings include:

```text
strict number handling
case-sensitive property names
unmapped members are disallowed
nullable annotations are respected
required constructor parameters are respected
string enum values are used
integer enum values are not allowed
```

This makes deserialization itself part of the validation boundary.

If the model returns JSON that cannot be deserialized into `CorrespondenceAnalysis`, the application throws `StructuredOutputException`.

---

## Schema Validation vs Business Validation

These are different layers.

### Schema / Structural Validation

The schema and serializer answer questions such as:

```text
Is the response valid JSON?
Does it match the expected object shape?
Are required properties present?
Are enum values valid strings?
Are unexpected properties present?
Are values represented using the expected JSON types?
```

### Business Validation

Normal application code answers questions such as:

```text
If DocumentType is HearingSchedule, were hearing details returned?
If DocumentType is ValueNotice, were value notice details returned?
Is the hearing time parseable?
Is the tax year reasonable?
Is the assessed value non-negative?
```

The LLM is responsible for producing a candidate structured result.

The application is responsible for deciding whether that result is acceptable.

---

## Hearing Schedule Validation

`HearingScheduleDetails` allows the hearing time to be absent:

```text
HearingTime = null
```

This is important because OCR text may omit or fail to capture the time.

If a hearing time is present, the application verifies that it can be parsed as a valid U.S. time.

A missing value is acceptable.

A malformed value is not.

---

## Value Notice Validation

`ValueNoticeDetails` applies simple business rules:

```text
TaxYear must be between 1900 and 3000 when present
AssessedValue must not be negative when present
```

These rules are deliberately simple, but they demonstrate why schema-valid JSON can still be unacceptable business data.

---

## Null Means Unknown, Not Invented

The prompt explicitly tells the model to use `null` when information is:

```text
missing
unreadable
ambiguous
uncertain
```

That is preferable to hallucinating a plausible-looking value.

For example, if a hearing notice contains a date but no readable time, a valid result can contain:

```json
{
  "hearingDate": "2026-09-14",
  "hearingTime": null,
  "location": "Fairfax County Government Center"
}
```

---

## Running the Lesson

Start the API:

```bash
dotnet run --project Lesson04.StructuredOutputs
```

The examples below assume the API is listening on:

```text
http://localhost:5000
```

---

## Test Case — Hearing Schedule

```bash
jq -Rs '{ documentText: . }' Samples/hearing-schedule.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
  -H 'Content-Type: application/json' \
  --data-binary @- | jq .
```

Expected characteristics:

```text
DocumentType = HearingSchedule
HearingSchedule contains extracted hearing details
ValueNotice = null
```

---

## Test Case — Hearing Schedule With Missing Time

```bash
jq -Rs '{ documentText: . }' Samples/hearing-schedule-missing-time.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
  -H 'Content-Type: application/json' \
  --data-binary @- | jq .
```

Expected characteristics:

```text
DocumentType = HearingSchedule
HearingTime = null
```

This demonstrates that missing information can remain explicitly unknown without causing the application to invent a value.

---

## Test Case — Value Notice

```bash
jq -Rs '{ documentText: . }' Samples/value-notice.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
  -H 'Content-Type: application/json' \
  --data-binary @- | jq .
```

Expected characteristics:

```text
DocumentType = ValueNotice
ValueNotice contains tax year, assessed value, and protest deadline when present
HearingSchedule = null
```

---

## Why Use `jq -Rs`?

The sample files contain multi-line OCR text.

This command:

```bash
jq -Rs '{ documentText: . }' Samples/hearing-schedule.txt
```

reads the entire file as a single string and safely JSON-escapes newlines and other characters before sending it to the API.

That avoids manually embedding multi-line OCR text inside a JSON string.

---

## Error Handling

Two custom exception types help distinguish failure modes.

### StructuredOutputException

Used when the model response cannot be converted into the expected C# structure.

Examples:

```text
invalid JSON
empty result
response shape incompatible with CorrespondenceAnalysis
```

### InvalidCorrespondenceAnalysisException

Used when the response can be deserialized but fails application-level validation.

Examples:

```text
HearingSchedule selected but hearing details missing
ValueNotice selected but value notice details missing
invalid hearing time
unreasonable tax year
negative assessed value
```

---

## Deliberately Out of Scope

Lesson04 does not implement:

- PDF parsing;
- document scanning;
- OCR;
- image preprocessing;
- many-document classification;
- few-shot examples;
- model fine-tuning;
- confidence scoring;
- persistent document storage;
- human review workflows;
- large-scale evaluation datasets.

The lesson focuses narrowly on the transition from unstructured OCR text to validated structured application data.

---

## Lesson04 Acceptance Criteria

Lesson04 is complete when:

```text
✓ OCR text can be submitted through /api/correspondence/analyze
✓ the model chooses HearingSchedule or ValueNotice
✓ common fields are extracted
✓ type-specific fields are extracted
✓ a JSON Schema is generated from CorrespondenceAnalysis
✓ the schema is used to constrain the model response
✓ the result deserializes into strongly typed C# records
✓ malformed structured output is rejected
✓ business validation runs after deserialization
✓ irrelevant detail objects are normalized to null
✓ missing/uncertain OCR values can remain null rather than being invented
```

---

## What Lesson04 Is Really Teaching

The lesson is not simply:

> How to make an LLM return JSON.

It is:

> **How to establish a reliable boundary between probabilistic language-model extraction and deterministic application code.**
