# Lesson 04: Structured Outputs

In this lesson, we take the plain text results of an OCR scan, and we send it to the LLM for classification and
indexing.  We create a JSON schema from an object and we provide it to the LLM.  This shows how we can make an LLM
produce structured output that we can programmatically rely upon.

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