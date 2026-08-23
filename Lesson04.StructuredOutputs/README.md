# Lesson04.StructuredOutputs

## Structured Outputs for OCR Correspondence

Lesson04 takes plain text produced by OCR and asks an LLM to:

```text
classify the document
+
extract structured fields
```

The application generates a JSON Schema from its C# result type, asks the selected provider for a schema-constrained result, deserializes the response into strongly typed records, and performs deterministic business validation.

The central lesson is:

> **An LLM can turn messy natural-language input into structured application data, but the application still owns schema enforcement and business validation.**

Lesson04 now supports the same two providers introduced earlier: Ollama and OpenAI.

---

## Business Scenario

Assume paper correspondence has already been scanned and converted to text by OCR.

The application recognizes exactly two document types:

```text
HearingSchedule
ValueNotice
```

Common fields:

```text
CustomerName
PropertyAddress
ParcelNumber
```

Hearing schedule fields:

```text
HearingDate
HearingTime
Location
```

Value notice fields:

```text
TaxYear
AssessedValue
ProtestDeadline
```

---

## Learning Goals

By the end of Lesson04, you should understand:

- how structured output differs from free-form prompting;
- how to generate JSON Schema from a C# type;
- how the same application schema can be mapped to different provider APIs;
- how schema-constrained output improves deserialization reliability;
- why schema validation and business validation are separate concerns;
- why nullable fields are useful when OCR text is missing or ambiguous;
- how provider choice can remain separate from the correspondence feature.

---

## Request Flow

```text
OCR text
    ↓
POST /api/correspondence/analyze
    ↓
AnalyzeCorrespondenceHandler
    ↓
generate JSON Schema
    ↓
IAiProviderFactory
    ↓
selected provider
    ├── Ollama structured format
    └── OpenAI JSON-schema response format
    ↓
JSON response
    ↓
strict deserialization
    ↓
NormalizeAndValidate()
```

---

## API

```http
POST /api/correspondence/analyze
```

Ollama remains the default:

```json
{
  "documentText": "...OCR text..."
}
```

OpenAI can be selected explicitly:

```json
{
  "documentText": "...OCR text...",
  "provider": "openai"
}
```

`provider` supports:

```text
ollama
openai
```

---

## Provider-Neutral Structured Output

The handler creates one application-level `AiChatRequest` containing:

```text
Messages
Temperature = 0
ResponseFormat = generated JSON Schema
Stream = false
```

The schema is provider-neutral at the feature boundary.

`OllamaProvider` maps it to Ollama's structured response format.

`OpenAiProvider` maps the same schema to OpenAI's JSON-schema response format.

That means `AnalyzeCorrespondenceHandler` does not need separate extraction logic for each provider.

---

## Generating the Schema

```csharp
var schema = StructuredOutputJson.Options
    .GetJsonSchemaAsNode(typeof(CorrespondenceAnalysis));
```

The generated schema remains aligned with the application's actual C# type instead of requiring a separately maintained handwritten schema.

The schema is also included in the user prompt for clarity.

---

## Schema Validation vs. Business Validation

### Schema / structural validation

Questions such as:

```text
Is the response valid JSON?
Does it have the expected object shape?
Are required properties present?
Are enum values valid?
Are unexpected properties present?
```

### Business validation

Questions such as:

```text
If DocumentType is HearingSchedule, were hearing details returned?
If DocumentType is ValueNotice, were value-notice details returned?
Is a hearing time parseable?
Is a tax year reasonable?
Is assessed value non-negative?
```

The provider produces a candidate result. Application code decides whether that result is acceptable.

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
│       └── ...
├── Infrastructure/
│   └── Ai/
│       ├── AiChatMessage.cs
│       ├── AiChatRequest.cs
│       ├── AiChatResponse.cs
│       ├── AiProviderFactory.cs
│       ├── IAiProvider.cs
│       ├── IAiProviderFactory.cs
│       ├── StructuredOutputJson.cs
│       └── Providers/
│           ├── OllamaOptions.cs
│           ├── OllamaProvider.cs
│           ├── OpenAiOptions.cs
│           └── OpenAiProvider.cs
├── Samples/
├── Program.cs
├── appsettings.json
└── README.md
```

---

## Running the Lesson

```bash
dotnet run --project Lesson04.StructuredOutputs
```

Examples assume:

```text
http://localhost:5000
```

### Hearing Schedule with Ollama

```bash
jq -Rs '{ documentText: . }' Samples/hearing-schedule.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
  -H 'Content-Type: application/json' \
  --data-binary @- | jq .
```

### Hearing Schedule with OpenAI

```bash
jq -Rs '{ documentText: ., provider: "openai" }' Samples/hearing-schedule.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
  -H 'Content-Type: application/json' \
  --data-binary @- | jq .
```

The same pattern works for `Samples/value-notice.txt` and the missing-time sample.

---

## Why Compare Providers Here?

Structured output is a useful place to compare providers because the expected result contract is much stronger than ordinary prose.

You can compare:

```text
schema adherence
field extraction
missing-value handling
latency
```

without changing the business result type or validation rules.

---

## Null Means Unknown, Not Invented

The prompt tells the model to use `null` when information is missing, unreadable, ambiguous, or uncertain.

This policy applies regardless of provider.

---

## Deliberately Out of Scope

Lesson04 does not implement:

- PDF parsing;
- scanning or OCR;
- many-document classification;
- few-shot examples;
- model fine-tuning;
- confidence scoring;
- persistent document storage;
- human review workflows;
- provider failover.

---

## Lesson04 Acceptance Criteria

```text
✓ OCR text can be submitted through /api/correspondence/analyze
✓ Ollama or OpenAI can perform the extraction
✓ the model chooses HearingSchedule or ValueNotice
✓ common and type-specific fields are extracted
✓ JSON Schema is generated from CorrespondenceAnalysis
✓ each provider receives an appropriate structured-output constraint
✓ the result deserializes into strongly typed C# records
✓ malformed structured output is rejected
✓ deterministic business validation runs after deserialization
✓ missing/uncertain values can remain null rather than being invented
```

---

## What Lesson04 Is Really Teaching

> **How to establish a reliable, provider-independent boundary between probabilistic language-model extraction and deterministic application code.**
