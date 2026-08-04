# Lesson 04: Structured Outputs

In this lesson, we take plain text produced by OCR and send it
to an LLM for document classification and field extraction.

We generate a JSON Schema from a C# type and supply that schema
to the LLM. This constrains the response to a predictable shape
that can be deserialized and validated by ordinary C# code.

# Demonstrates

```text
OCR text
↓
Generated JSON Schema
↓
Structured LLM response
↓
Strongly typed deserialization
↓
Normalization and validation
↓
Parsed API response
```

# Test cases

```bash
# Hearing Schedule
jq -Rs '{ documentText: . }' Samples/hearing-schedule.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
-H 'Content-Type: application/json' \
--data-binary @- | jq .
```

```bash
# Hearing Schedule Missing Time.
jq -Rs '{ documentText: . }' Samples/hearing-schedule-missing-time.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
-H 'Content-Type: application/json' \
--data-binary @- | jq .
```

```bash
# Value Notice
jq -Rs '{ documentText: . }' Samples/value-notice.txt |
curl -X POST http://localhost:5000/api/correspondence/analyze \
-H 'Content-Type: application/json' \
--data-binary @- | jq .
```