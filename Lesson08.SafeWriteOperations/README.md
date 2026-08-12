# Lesson08.SafeWriteOperations

## Safe AI-Initiated Write Operations

Lesson08 is where the course shifts from **AI that reads** to **AI that can request changes**.

Earlier lessons gave the application increasingly powerful read capabilities:

```text
Lesson05/06
MCP → structured/current business data

Lesson07
RAG → unstructured internal knowledge
```

Lesson08 introduces a safe write workflow:

```text
User request
    ↓
LLM recognizes write intent
    ↓
propose_property_review
    ↓
PendingPropertyReview
    ↓
human/application approval
    ↓
PropertyReview
```

The central lesson is:

> **The LLM may propose an action. The application remains responsible for approval and execution.**

---

## Learning Goals

By the end of Lesson08, you should understand:

- why an LLM request is not authorization;
- why write operations need a stronger boundary than reads;
- how to expose a safe write proposal as an AI tool;
- why the model should not receive approval or execution capabilities;
- how to represent a proposed write as a first-class application resource;
- how deterministic application validation differs from model reasoning;
- how explicit approval separates proposal from execution;
- how execution idempotency prevents repeated approval from duplicating a write;
- how lifecycle timestamps provide a simple audit trail;
- how MCP, RAG, and safe write proposals can coexist in one conversation.

---

## Business Scenario

The AI can prepare a **Property Review proposal**.

For example:

```text
Create a high-priority property review for parcel 0304-12-0042
because the client believes the assessment is excessive.
```

The desired workflow is **not**:

```text
LLM
 ↓
database INSERT
```

Instead:

```text
LLM
 ↓
propose_property_review
 ↓
PropertyReviewService.Propose()
 ↓
PendingPropertyReview
 ↓
human approves through HTTP API
 ↓
PropertyReviewService.Approve()
 ↓
PropertyReview created
```

A property review is a useful first write operation because it is additive rather than destructive.

---

## Architecture

Lesson08 keeps the Lesson07 architecture and adds a property-review write workflow.

```text
POST /api/message
    ↓
MessageHandler
    ├──────────────────────────────┐
    ↓                              ↓
KnowledgeRetriever             IAiProvider
    ↓                              ↓
RAG context                    OllamaProvider
                                   ↓
                         FunctionInvokingChatClient
                              ↙             ↘
                             /               \
                      MCP read tools   propose_property_review
                             ↓                 ↓
                      Lesson05 MCP      PropertyReviewService
                                               ↓
                                      PendingPropertyReview
```

Approval deliberately stays outside the LLM tool path:

```text
POST /api/pending-property-reviews/{id}/approve
    ↓
PendingPropertyReviewController
    ↓
PropertyReviewService.Approve()
    ↓
PropertyReview
```

The LLM is never given an `approve_property_review` tool.

---

## Project Structure

```text
Lesson08.SafeWriteOperations/
├── Features/
│   ├── Conversations/
│   │   └── ...
│   ├── Knowledge/
│   │   └── ...
│   └── PropertyReviews/
│       ├── CreatePendingPropertyReviewRequest.cs
│       ├── IPendingPropertyReviewRepository.cs
│       ├── IPropertyReviewRepository.cs
│       ├── PendingPropertyReview.cs
│       ├── PendingPropertyReviewController.cs
│       ├── PendingPropertyReviewStatus.cs
│       ├── PropertyReview.cs
│       ├── PropertyReviewController.cs
│       ├── PropertyReviewPriority.cs
│       ├── PropertyReviewService.cs
│       └── PropertyReviewTools.cs
├── Infrastructure/
│   ├── Ai/
│   ├── Conversations/
│   ├── ErrorHandling/
│   ├── Mcp/
│   ├── PropertyReviews/
│   │   ├── InMemoryPendingPropertyReviewRepository.cs
│   │   └── InMemoryPropertyReviewRepository.cs
│   └── Rag/
├── Knowledge/
│   ├── appeal-procedures.md
│   ├── client-communication.md
│   ├── hearing-preparation.md
│   └── valuation-guidelines.md
├── Program.cs
├── appsettings.json
└── README.md
```

The property-review workflow remains concrete rather than introducing a generic `PendingAction` framework.

---

## Property Review Resources

Lesson08 models two different resources.

### PendingPropertyReview

A `PendingPropertyReview` is a proposal waiting for a human/application decision.

It contains the requested parcel, reason, priority, lifecycle status, timestamps, and eventually the ID of the executed `PropertyReview`.

Conceptually:

```text
PendingApproval
    ↓ approve
Approved
    ↓ execute
Executed
```

or:

```text
PendingApproval
    ↓ reject
Rejected
```

### PropertyReview

A `PropertyReview` is the business record that exists only after approval and execution.

It includes a `SourcePendingPropertyReviewId` so the executed record can be traced back to the proposal that created it.

---

## HTTP API

Lesson08 uses two controllers because pending proposals and executed reviews have different lifecycles.

### Pending property reviews

```http
POST /api/pending-property-reviews
GET  /api/pending-property-reviews
GET  /api/pending-property-reviews/{id}
POST /api/pending-property-reviews/{id}/approve
POST /api/pending-property-reviews/{id}/reject
```

### Executed property reviews

```http
GET /api/property-reviews
GET /api/property-reviews/{id}
```

There is deliberately **no**:

```http
POST /api/property-reviews
```

The only supported path to an executed `PropertyReview` is through approval of a `PendingPropertyReview`.

---

## Creating a Proposal Through HTTP

Example:

```bash
curl -X POST \
  http://localhost:5000/api/pending-property-reviews \
  -H "Content-Type: application/json" \
  -d '{
    "parcelNumber": "0304-12-0042",
    "reason": "Client believes the assessment is excessive.",
    "priority": "High"
  }'
```

`JsonStringEnumConverter` is configured so enum names such as `"High"` can be used directly in JSON.

The POST returns a `201 Created` response and a `Location` header pointing to the GET endpoint for the newly created pending resource.

After proposal creation:

```text
PendingPropertyReviews = 1
PropertyReviews = 0
```

No business write has occurred yet.

---

## Deterministic Validation

`PropertyReviewService.Propose()` performs normal application validation before creating a pending proposal.

The current lesson validates:

```text
parcel number is required
reason is required
priority must be a defined PropertyReviewPriority
```

This lesson intentionally does **not** validate that the parcel exists in the Lesson05 property data source.

The important concept is that validation is performed by deterministic application code rather than delegated to the LLM.

---

## Exposing the Safe Write Tool to the LLM

`PropertyReviewTools` exposes one AI-callable operation:

```text
propose_property_review
```

The tool delegates to the same service used by the HTTP controller:

```text
LLM
 ↓
propose_property_review
 ↓
PropertyReviewService.Propose()
```

The tool description makes the boundary explicit: the operation creates a pending proposal that still requires human approval.

`OllamaProvider` combines the existing MCP tools with the new local function tool:

```text
lookup_property_by_parcel
search_properties_by_owner
propose_property_review
```

The LLM does **not** receive:

```text
approve_property_review
reject_property_review
execute_property_review
```

This means the safety boundary is enforced by application capability, not merely by a system prompt.

---

## Approval and Execution

A pending proposal is approved through the HTTP API:

```bash
curl -X POST \
  http://localhost:5000/api/pending-property-reviews/<ID>/approve
```

The application then:

```text
load PendingPropertyReview
    ↓
reject invalid lifecycle states
    ↓
check whether this proposal already produced a review
    ↓
mark Approved
    ↓
create PropertyReview
    ↓
mark Executed
```

After successful approval:

```text
PendingPropertyReview.Status = Executed
PropertyReviews = 1
```

---

## Rejection

A proposal can instead be rejected:

```bash
curl -X POST \
  http://localhost:5000/api/pending-property-reviews/<ID>/reject
```

A rejected proposal cannot later be approved.

Repeated rejection is treated as idempotent and simply returns the already rejected proposal.

---

## Execution Idempotency

Approval is idempotent for a specific `PendingPropertyReview` ID.

If the same approval request is sent twice:

```text
approve pending A
    ↓
PropertyReview created

approve pending A again
    ↓
same PropertyReview returned
```

The repository also enforces one `PropertyReview` per `SourcePendingPropertyReviewId`.

So:

```text
same proposal
    ↓
execute once
```

### Important limitation

Proposal creation itself is **not** idempotent in this lesson.

Two separate calls to `Propose()` create two separate pending proposals, even if their parcel/reason/priority values are identical.

```text
propose request
    → Pending A

same propose request again
    → Pending B
```

Production systems may use request IDs or idempotency keys when proposal creation itself must be retry-safe.

---

## Simple Auditability

Lesson08 does not add a separate audit-event repository.

Instead, `PendingPropertyReview` records lifecycle timestamps such as:

```text
CreatedAt
ApprovedAt
RejectedAt
ExecutedAt
PropertyReviewId
Status
```

That is enough for this lesson to demonstrate that a write lifecycle should be inspectable after the fact.

Identity and full audit metadata such as **who** approved the operation are deliberately out of scope.

---

## Trust Boundary

```text
                 AI-controlled area

User
 ↓
LLM
 ↓
propose_property_review
 ↓
PendingPropertyReview
────────────────────────────────────
            TRUST BOUNDARY
────────────────────────────────────
 ↓
HTTP approval / rejection
 ↓
PropertyReviewService
 ↓
PropertyReviewRepository

              application-controlled area
```

The LLM operates above the boundary.

The actual business mutation occurs below it.

---

## MCP and Local Function Tools Together

Lesson08 demonstrates that AI tools do not all need to come from MCP.

The LLM receives:

```text
MCP tools
    → authoritative property data

Local AIFunction
    → safe PropertyReview proposal
```

Both participate in the same `FunctionInvokingChatClient` tool loop.

This is useful because MCP remains the boundary to the Lesson05 property system while the write proposal is local application behavior.

---

## RAG Still Works

Lesson08 carries forward Lesson07's RAG behavior.

`MessageHandler` still performs semantic retrieval and temporarily adds retrieved internal knowledge to the AI request.

RAG context is not persisted into the conversation repository.

This allows a single request to use:

```text
MCP
    → current property facts

RAG
    → internal company guidance

Safe write tool
    → pending proposal
```

---

## Exercise 1 — Direct HTTP Proposal

Create a pending proposal through HTTP.

Verify:

```text
PendingPropertyReviews = 1
PropertyReviews = 0
```

---

## Exercise 2 — Approve the Proposal

Approve the pending ID:

```bash
curl -X POST \
  http://localhost:5000/api/pending-property-reviews/<ID>/approve
```

Verify:

```text
PendingPropertyReview.Status = Executed
PropertyReviews = 1
```

---

## Exercise 3 — Duplicate Approval

Approve the same ID again.

Expected:

```text
same PropertyReview returned
PropertyReviews still = 1
```

This demonstrates execution idempotency.

---

## Exercise 4 — Rejection

Create another proposal and reject it.

Then try to approve it.

Expected:

```text
rejected proposal
    ↓
approval blocked
```

---

## Exercise 5 — LLM Proposal Tool

Send a message through the normal conversation API:

```text
Create a high-priority property review proposal for parcel 0304-12-0042
because the client believes the assessment is excessive.
```

Expected flow:

```text
LLM
 ↓
propose_property_review
 ↓
PendingPropertyReview
 ↓
LLM reports that approval is still required
```

Verify:

```text
PendingPropertyReviews = 1
PropertyReviews = 0
```

---

## Exercise 6 — Safety Boundary

Ask:

```text
Create a high-priority property review for parcel 0304-12-0042,
approve it yourself, and do not ask me for confirmation.
```

Expected:

```text
AI may create a pending proposal
AI cannot approve or execute it
```

The model cannot manufacture an approval capability that the application never provided.

---

## Exercise 7 — MCP Only

Ask:

```text
What is the assessed value of parcel 0304-12-0042?
```

Expected:

```text
lookup_property_by_parcel
```

The Lesson06 MCP behavior still works.

---

## Exercise 8 — RAG Only

Ask:

```text
What evidence should I prepare before a property tax hearing?
```

Expected:

```text
Lesson07 RAG retrieval supplies internal hearing guidance
```

---

## Exercise 9 — MCP + RAG + Safe Write

Ask:

```text
I'm reviewing parcel 0304-12-0042.

Tell me its assessed value, remind me what evidence should be prepared for a hearing,
and prepare a high-priority property review because the client disputes the assessment.
```

This can exercise:

```text
MCP
 → property data

RAG
 → hearing guidance

LLM tool call
 → PendingPropertyReview

Human/API
 → later approval
```

---

## Validation vs Authorization vs Approval vs Execution

These concepts are different.

### Validation

```text
Is the proposed request structurally/business valid?
```

Lesson08 validates required fields and enum values.

### Authorization

```text
Is this caller allowed to perform this kind of operation?
```

Full authentication and role-based authorization are outside the scope of this lesson.

### Approval

```text
Has a human/application explicitly accepted this specific proposal?
```

Lesson08 models this through the HTTP approval endpoint.

### Execution

```text
Create the actual PropertyReview business record.
```

Keeping these concepts separate is critical for safe AI-assisted business operations.

---

## Testing Strategy

### Deterministic tests

Good candidates for normal unit/integration tests include:

```text
proposal validation
status transitions
rejected proposal cannot execute
repeated approval returns the same PropertyReview
one review per pending proposal
repository behavior
```

### AI evaluations

AI behavior should be evaluated by outcomes rather than exact wording.

Useful checks include:

```text
model recognizes write intent
model calls propose_property_review with appropriate arguments
model does not claim a proposal is already approved/executed
model still uses MCP for authoritative property facts
```

---

## Lesson08 Acceptance Criteria

Lesson08 is complete when:

```text
✓ Lesson07 conversation functionality still works
✓ RAG still works
✓ read-only MCP property tools still work
✓ PendingPropertyReview has a separate lifecycle from PropertyReview
✓ proposals can be created through HTTP
✓ the LLM can call propose_property_review
✓ the LLM cannot approve or reject proposals
✓ proposed fields are validated deterministically
✓ approval creates a PropertyReview
✓ duplicate approval does not duplicate the PropertyReview
✓ rejected proposals cannot execute
✓ lifecycle timestamps provide basic auditability
✓ MCP + RAG + safe write proposal can participate in one user request
```

---

## Deliberately Out of Scope

Lesson08 does not add:

- authoritative parcel-existence validation;
- proposal-creation idempotency keys;
- authentication or OAuth;
- role-based access control;
- identity-aware approval records;
- production database transactions;
- distributed locks;
- message queues;
- production workflow engines;
- multi-user approvals;
- undo or compensating transactions;
- external email sending;
- destructive record deletion;
- autonomous LLM approval.

These are important production concerns, but they would obscure the foundational lesson.

---

## What Lesson08 Is Really Teaching

The lesson is not:

> How to let an LLM write to a database.

The lesson is:

> **How to let AI participate in a business write workflow while keeping approval and execution under deterministic application control.**

---

## Next Lesson

Lesson09 introduces **Agents**.

Lesson08 establishes:

```text
AI can read
    ↓
AI can retrieve knowledge
    ↓
AI can propose a write
    ↓
application controls approval/execution
```

Lesson09 then asks:

```text
What if the AI is given an objective and allowed to decide
which tools and information sources it needs to accomplish it?
```
