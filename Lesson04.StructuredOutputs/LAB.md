# Lesson04 Lab — Structured Outputs

This lab is the hands-on companion to [README.md](README.md).

## Goal

Add a third correspondence type, `TaxDelinquencyNotice`, with type-specific fields and deterministic validation.

## Predict

1. Which parts of the result contract are enforced by JSON Schema?
2. Which rules still require application validation?
3. What should the model return when a field is missing or unreadable?

## Run

Run the existing Hearing Schedule and Value Notice samples first. Inspect the generated structured result and the validation path.

## Build — Add `TaxDelinquencyNotice`

Add a third document type with these fields:

- `TaxYear`
- `AmountDue`
- `DueDate`

Update the C# result model, generated-schema-compatible structure, classification/extraction instructions, and deterministic validation. Preserve the existing two document types.

Create at least two sample inputs: one complete notice and one with an unreadable or missing value.

## Attack

- Omit `AmountDue`.
- Make `DueDate` ambiguous.
- Include text that looks like a number but is not a valid amount.
- Ask the model to invent missing information.

The desired behavior is to preserve uncertainty rather than fabricate a value.

## Explain

1. What reliability does structured output add compared with ordinary prompting?
2. Why is valid JSON still insufficient for business correctness?
3. Why should missing data remain `null` rather than be guessed?

## Lab Completion Criteria

```text
✓ TaxDelinquencyNotice can be classified
✓ type-specific fields deserialize into C# types
✓ schema remains generated from application types
✓ deterministic validation covers the new type
✓ incomplete source data does not force invented values
```
