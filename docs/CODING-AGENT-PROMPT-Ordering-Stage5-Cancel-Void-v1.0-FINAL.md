# CODING AGENT PROMPT — AeroTech Ordering Stage 5 v1.0 FINAL
## Cancel Unissued Commercial Scope + Reservation Release/Cancel + Local ETKT Void

### Execution mode

Implement this Stage end-to-end in one pass across both repositories.

Do not stop for design decisions. All domain/product decisions required by this Stage are frozen below.

Only stop with:

```text
BLOCKED_SOURCE: <exact missing or contradictory authoritative fact>
```

when a real current-source contradiction makes the frozen Stage impossible without inventing business/provider truth.

These are NOT blockers:
- no external ETKT void provider;
- no executable EMD profile;
- no cancellation-pricing/penalty quote provider;
- no Payment/JetPay;
- no DCS source.

Do not start Stage 6.

---

# 0. Reviewed baselines

Ordering:

```text
repo: https://github.com/aliifarhadi/AeroTech.Ordering.Final
branch: k8s-stg
Stage-4-closed HEAD: b9c5fddfe2c4edb4201f312eadc81c3ec8f5e5d0
```

FlightFlow:

```text
repo: https://github.com/aliifarhadi/Aerotech.FlightFlow
branch: k8s-stg
reviewed HEAD: fe70d1cb584765cb5ddff170538d912ad1ca380a
```

Before coding:
1. fetch both repos;
2. report exact HEADs;
3. inspect any unexpected drift;
4. preserve all Stage 1–4 behavior.

---

# 1. Authority

1. `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`
2. this Stage-5 document
3. current AeroTech source contracts
4. current public IATA / Amadeus / Sabre business semantics
5. donor code only as historical evidence

Do not restore:
- TrafficDocument;
- generic servicing Operation aggregate;
- Funding/Payment gates;
- deterministic/mock external void provider;
- donor cancellation quote adapter without real current source;
- donor Payment void;
- donor EMD servicing.

Add this exact file to:

```text
docs/CODING-AGENT-PROMPT-Ordering-Stage5-Cancel-Void-v1.0-FINAL.md
```

Update `CLAUDE.md` active Stage authority accordingly.

---

# 2. Entering status

```text
STAGE_1_CLOSED
STAGE_2_CLOSED
STAGE_3_CLOSED
STAGE_4_CLOSED
STAGE_5_IN_PROGRESS
```

---

# 3. Product model

Cancel and Void are distinct.

```text
Cancel:
Active unissued commercial scope
 -> settle reservation resource
 -> append OrderChange(Cancel)
 -> mark commercial occurrences Cancelled

Void:
Issued accountable ETKT
 -> verify void authority/window/coupon state
 -> coupon FinancialStatus=Void
 -> ticket StatusSummary=Voided
 -> append DocumentVoidRecord
```

Never equate:

```text
Cancel == Void
Void == Refund
Cancel == Payment reversal
```

---

# 4. Benchmark decisions

## 4.1 Coupon truth

Sabre/public airline document behavior recognizes coupon states such as:

```text
OPEN / USED / VOID / EXCH / RFND / SUSP
```

Document servicing must therefore remain coupon-aware.

## 4.2 Current LocalDocumentAuthority void profile

Current public IATA BSP guidance and Amadeus/Sabre public behavior support same-day void semantics.

Freeze current AeroTech local profile:

```text
Void window = from IssuedAt until next local midnight of the issuing-office calendar day
Eligibility = now < VoidDeadline
```

This is a current capability profile, not a universal airline invariant.

Do NOT implement `IssuedAt + 24 hours`.

## 4.3 Same issuing office

Local Stage-5 Void requires:

```text
ticket.IssuanceContext.IssuingOfficeId != null
caller.AirlineOfficeId != null
caller.AirlineOfficeId == ticket.IssuanceContext.IssuingOfficeId
```

Otherwise no local void authority exists.

## 4.4 Source gates

```text
EXTERNAL_ETKT_VOID = DEFERRED_SOURCE_GATED
EMD_VOID = DEFERRED_SOURCE_GATED
CANCELLATION_PRICING_DECISION = DEFERRED_SOURCE_GATED
```

Do not fake any of them.

---

# 5. Current-source findings

## 5.1 FlightFlow CancelConfirmed is not Stage-5 safe yet

Current handler:
- loads Flights before lock;
- derives keys via `flight.ToString()`;
- mutates stale aggregates;
- lacks corrected reload-under-lock pattern;
- returns generic 200 without authoritative state body;
- can silently no-op/partially act for non-Confirmed/mixed refs.

Ordering must not consume it as authoritative until corrected.

## 5.2 Provider granularity

Current FlightFlow can cancel selected confirmed seat-hold references.

Therefore post-confirm scope is:

```text
ReservationUnit / provider SeatHoldReference
```

## 5.3 Pre-confirm release granularity

Current capability:

```text
PreConfirmationReleaseScope = Operation
PostConfirmationCancelScope = Unit
```

Never fake partial Held release.

---

# 6. Stage scope

Implement now:

### FlightFlow
- corrected CancelConfirmed contract;
- exact selected-set validation;
- common Flight-ID lock keys;
- reload under lock;
- same-effect replay;
- structured success;
- deterministic errors;
- concurrency tests.

### Ordering
- full unissued cancellation;
- targeted Air-service cancellation;
- dependency closure;
- Held Release integration;
- Confirmed CancelConfirmed integration/recovery;
- EndedByChangeId for Item/Service;
- Stage-5 OrderChange provenance;
- commercial-version increment;
- local ETKT Void;
- DocumentVoidRecord;
- local void-deadline policy;
- Void FulfillmentTask;
- ElectronicTicketVoided V1;
- query history;
- Backoffice APIs;
- migrations/tests.

Do not implement:
- Refund;
- Exchange/Reissue;
- Payment/JetPay;
- EMD;
- external document provider;
- DCS;
- independent seat unassignment;
- group/split/disruption/accounting.

---

# 7. FlightFlow CancelConfirmed correction

Keep endpoint concept:

```text
POST Service/v1/Flights/Seat-Confirmations/{holdBatchId}/Cancellation
```

Body:

```text
SeatHoldReferences[]
ReasonCode
```

Do NOT add fake provider idempotency identity.

## 7.1 Structured success

Documented success is HTTP `200` with:

```text
HoldBatchId : string
Seats : IReadOnlyList<CancelledConfirmedSeatDto>
```

Each:

```text
SeatHoldReference : string
Status : FlightSeatHoldStatus
```

Success requires:
- exact requested reference set;
- every returned status = Cancelled.

No array-order inference.

## 7.2 Error codes

Use next free codes, expected at reviewed baseline:

```text
1186 SeatHoldNotFoundForCancellation
1187 CannotCancelHeldSeatHold
1188 CannotCancelReleasedSeatHold
1189 CannotCancelExpiredSeatHold
1190 CannotCancelSeatHoldWithInconsistentStatuses
```

If source drift consumed them, use next free codes and report exact mapping.

Semantics:

```text
missing ref / batch mismatch -> 1186, zero mutation
all Confirmed -> cancel all atomically, 200
all Cancelled -> same-effect 200, zero second mutation
all Held -> 1187
all Released -> 1188
all Expired -> 1189
mixed -> 1190
```

Never partially cancel the Confirmed subset of a mixed request.

## 7.3 Locking algorithm

1. resolve distinct Flight IDs for requested seat refs;
2. acquire same sorted/distinct Flight-ID locks used by Confirm/Release/Expire;
3. reload authoritative Flight aggregates inside locks;
4. resolve exact requested holds;
5. verify all refs exist and belong to `holdBatchId`;
6. evaluate all selected statuses;
7. guard before mutation;
8. mutate all selected Confirmed holds;
9. update snapshots;
10. commit under lock.

Forbidden:

```text
flight.ToString() lock key
preloaded aggregate mutation
partial mixed mutation
```

## 7.4 Capacity

Confirmed -> Cancelled:
- decrement Confirmed once;
- return capacity once;
- replay changes no capacity.

## 7.5 Capability after correction

```text
AcceptsIdempotencyIdentity = VerifiedNo
DuplicateMutationSemantics = SameEffect for exact already-Cancelled selection
SupportsAuthoritativeReadBack = VerifiedNo
SupportsSafeReplayAfterAmbiguousOutcome = VerifiedYes
ResultGranularity = AtomicSelectedUnits
```

## 7.6 FlightFlow tests

At minimum:

```text
FF-C01 Confirmed -> Cancelled / capacity once
FF-C02 exact replay / no second effect
FF-C03 unknown ref -> no mutation
FF-C04 wrong HoldBatchId -> no mutation
FF-C05 Held -> deterministic rejection
FF-C06 Released -> deterministic rejection
FF-C07 Expired -> deterministic rejection
FF-C08 Confirmed+Cancelled mixed -> no partial
FF-C09 Confirmed+Released mixed -> no partial
FF-C10 multi-flight same sorted lock set
FF-C11 Cancel vs Confirm race
FF-C12 Cancel vs Release/Expire race
FF-C13 lost response + exact replay
FF-C14 undocumented 2xx not authoritative success
```

---

# 8. Ordering reservation-provider contract

Extend `IReservationProvider` with typed confirmed-cancellation behavior.

Use domain concepts equivalent to:

```text
ConfirmedCancellationIntent
ConfirmedCancellationOutcome
```

Do not leak FlightFlow DTOs.

Intent must preserve:
- provider key;
- HoldBatch/provider operation ref;
- target ReservationUnit IDs;
- exact ProviderUnitRefs;
- commercial reason provenance.

ProviderRequest:

```text
InteractionType = CancelConfirmed
IdempotencyKey = null
```

Persist exact serialized provider request before dispatch.

For current voluntary Backoffice cancellation, provider-native reason is:

```text
PaxRequest
```

Do not pretend `VoidReason` values are native FlightFlow reasons.

Commercial reason remains on OrderChange.

Only:

```text
HTTP 200 + valid exact structured Cancelled result
```

is authoritative success.

Unexpected/malformed 2xx => Unknown.

---

# 9. FulfillmentReservation selected-unit cancellation

Add selected-unit behavior.

Success:
```text
target units -> Cancelled
unaffected units unchanged
root recomputed from all units
```

Examples:
```text
all Cancelled -> reservation Cancelled
Confirmed + Cancelled -> Mixed
```

Unknown:
```text
target units -> Unknown
unaffected units unchanged
root recomputed truthfully
commercial state remains Active
```

Coded provider state rejection:
- apply authoritative observed target status;
- do not commit commercial cancellation in that pass;
- later retry replans from known state.

Do not automatically chain a new provider mutation after an unexpected state in the same request.

---

# 10. CancelConfirmed FulfillmentTask

Use existing:

```text
TaskType = CancelConfirmed
InteractionType = CancelConfirmed
Action = Cancel
FulfillmentReservationId = reservation.Id
TargetKind = ReservationUnit
```

Task identity:

```text
IdempotencyKey = $"cancel-confirmed:{taskId}"
CorrelationReference = $"order:{orderId}:reservation:{reservationId}:cancel:{taskId}"
```

Before provider call:
1. create task;
2. start attempt;
3. build exact request;
4. RecordRequest;
5. persist request/task;
6. then dispatch.

Recovery:
- find original CancelConfirmed request;
- replay exact payload;
- never rebuild from mutable state.

Missing original request:
```text
dedicated recovery-request-missing error
zero provider call
```

Unknown original + generic recovery refusal without state:
```text
remain Unknown/resumable
```

---

# 11. Commercial cancellation API

Implement:

```text
POST Backoffice/v1/Orders/{orderId}/Cancellations
```

Request:

```text
ServiceIds : IReadOnlyList<long>?
Reason : VoidReason
ReasonDetail : string?
```

Semantics:
```text
null/empty -> all current active cancellable scope
non-empty -> targeted scope
```

Use current Backoffice authorization and `ISalesContextFactory`.

No client idempotency key.

---

# 12. Cancellation eligibility

Only active commercial services are candidates.

Repeat already-cancelled scope:
- no new OrderChange;
- no version increment;
- no new task.

Before cancellation, inspect ETKT truth.

Any selected service covered by active/non-void ticket coupon blocks commercial cancellation:

```text
DOCUMENT_SERVICING_REQUIRED
```

Do not silently Void inside Cancel.

After ticket Void, covered services may be cancelled.

Block overlapping unresolved tasks for:
```text
ReserveInventory
ConfirmInventory
ReleaseReserved
CancelConfirmed
IssueTicket
VoidTicket
```

where scope overlaps.

Do not use OrderStatus alone.

---

# 13. Dependency closure

## Air -> Seat

Cancelling Air automatically includes active associated Seat service(s).

## Adult -> lap infant

Cancelling an adult Air service includes dependent active lap-infant Air service(s) on the same segment.

## Infant-only cancellation

Infant-only Air cancellation does NOT cancel parent seat capacity.

No FlightFlow capacity mutation is required for the dependent infant occurrence alone.

## Seat-only

Current source has no seat-unassignment contract.

Seat-only targeted cancellation is rejected:

```text
SEAT_CANCELLATION_REQUIRES_ANCILLARY_STAGE
```

Do not clear SeatNumber, cancel parent capacity, or fake EMD behavior.

---

# 14. Resource settlement

Classify latest truthful resource state.

## ReservationMode.None
No provider mutation.

## Held
If selected scope covers all active provider-resource scope in that hold:
- use existing Stage-3 Release path/recovery.

If retained active scope shares same Held operation and capability is operation-scoped:
```text
PARTIAL_HELD_CANCELLATION_NOT_SUPPORTED_BY_PROVIDER
```

Do not release whole hold and secretly recreate retained scope.

## Confirmed
Use corrected CancelConfirmed for exact resource-owning selected units.

## Released / Expired / Cancelled / Rejected
No new provider mutation required.

## Pending / Unknown / Mixed / Waitlisted
No commercial commit.

Unknown != failed.

---

# 15. Multi-reservation behavior

Provider effects settle independently.

If one succeeds and another is Unknown:
- keep successful provider truth;
- do not roll it back;
- do not commit commercial cancellation;
- retry resumes only unresolved obligations.

No parallel generic operation aggregate.

---

# 16. Commercial mutation

Materialize:

### OrderItem
```text
EndedByChangeId : long?
```

### OrderService
```text
EndedByChangeId : long?
```

### OrderChange
```text
SourceSystem : string?
ReasonCode : string?
IsInvoluntary : bool
WaiverCode : string?
```

Current Backoffice voluntary cancellation:

```text
ChangeType = Cancel
SourceSystem = null
ReasonCode = request.Reason.ToString()
IsInvoluntary = false
WaiverCode = null
SourceReference = null
```

Do not infer involuntary from `ScheduleChange`.

On commit:
1. CommercialVersion increments once;
2. append one OrderChange(Cancel) with resulting version;
3. selected/dependency-closed active services -> Cancelled + EndedByChangeId;
4. Item -> Cancelled + EndedByChangeId only if no active service remains in item;
5. preserve all old rows/history.

No cancellation quote exists today, so:
- no penalty invented;
- no refund credit invented;
- no synthetic pricing line;
- no Payment call;
- CustomerTotal not forced to zero.

Report:
```text
CANCELLATION_PRICING_DECISION = DEFERRED_SOURCE_GATED
```

---

# 17. Order summary after cancellation

Fix `OrderReservationSummarizer` to use only:

```text
CommercialStatus == Active
AND provider requires reservation
```

Cancelled services must no longer influence current reservation summary.

Full cancellation:
```text
OrderStatus = Cancelled
```
only when:
- no active commercial service remains;
- no non-void document remains requiring servicing.

Partial:
- if all active ticketable Air has active non-void ETKT coverage -> Ticketed;
- else summarize active reservation truth.

No new PartiallyCancelled status.


---

# 18. Local ETKT Void policy

Stage 5 supports only:

```text
ElectronicTicket.Authority == Local
```

## 18.1 Compute VoidDeadline for NEW local ETKTs

Update local Issue flow additively.

Resolve selected `DocumentStock.OfficeId`.

If:
- OfficeId exists;
- AirlineOffice reference exists;
- `TimeZoneId` exists and is valid;

compute:

```text
issuanceLocal = IssuedAt converted to issuing-office timezone
localBoundary = midnight starting next local calendar day
VoidDeadline = that boundary converted to an absolute DateTimeOffset instant
```

Eligibility:

```text
now < VoidDeadline
```

Do not use:
```text
IssuedAt + 24h
```

If office/timezone missing:
```text
VoidDeadline = null
```

Issue still succeeds.

## 18.2 Historical Stage-4 tickets

Do NOT backfill null VoidDeadline.

Historical null:
```text
not voidable by current local policy
refund-required disposition
```

No historical timezone inference.

---

# 19. Reference-data support

Add smallest local reference query:

```text
AirlineOfficeId -> TimeZoneId
```

Use synchronized `AeroTech.Ordering.ReferenceData`.

Do not call external Core synchronously from Issue/Void command.

No fallback to host timezone or UTC when source timezone missing.

---

# 20. DocumentVoidRecord

Materialize exactly:

| Field | Type | Null |
|---|---|---:|
| VoidFulfillmentTaskId | long | No |
| ReasonCode | string? | Yes |
| ReasonText | string? | Yes |
| ProviderReference | string? | Yes |
| ActorId | long? | Yes |
| VoidedAt | DateTimeOffset | No |

Attach:

```text
ElectronicTicket.VoidRecord : DocumentVoidRecord?
```

Current local mapping:

```text
VoidFulfillmentTaskId = void task Id
ReasonCode = request.Reason.ToString()
ReasonText = normalized ReasonDetail
ProviderReference = null
ActorId = current caller ActorId
VoidedAt = now
```

Append once.

Do not use donor generic `OperationId` internally.

---

# 21. ElectronicTicket Void rules

Eligibility requires:

```text
Authority == Local
StatusSummary == Issued
VoidRecord == null
VoidDeadline != null
now < VoidDeadline
IssuingOfficeId != null
caller AirlineOfficeId == IssuingOfficeId
```

Every coupon must satisfy:

```text
FinancialStatus == Open
ControlStatus == Local
UsedAt == null
```

Any:
```text
Used / Exchanged / Refunded / Suspended / Void
```
or non-local/unknown control blocks new Void.

On successful Void:

```text
each coupon.FinancialStatus = Void
ticket.StatusSummary = Voided
ticket.VoidRecord = record
ticket.DocumentVersion++
```

Keep all immutable issue/price/fare/segment/document history.

---

# 22. Void FulfillmentTask

Add explicit enum values without renumbering existing ones:

```text
OrderFulfillmentTaskType.VoidTicket = 11
ProviderInteractionType.VoidTicket = 10
```

Current local task:

```text
TaskType = VoidTicket
FulfillmentProviderKey = LocalDocumentAuthority
FulfillmentReservationId = null
IdempotencyKey = $"void-ticket:{taskId}"
CorrelationReference = $"order:{orderId}:void:{taskId}"
```

Targets:

```text
ElectronicTicket
TicketCoupon
Action = Void
```

One task may target multiple newly-voided tickets.

Start one attempt.
Create zero ProviderInteractions.
Complete Succeeded in same DB commit as document mutation.

---

# 23. Void API

Implement:

```text
POST Backoffice/v1/Orders/{orderId}/Documents/Voids
```

Request:

```text
Tickets : IReadOnlyList<VoidElectronicTicketRequest>
Reason : VoidReason
ReasonDetail : string?
```

Each:

```text
ElectronicTicketId : long
ExpectedDocumentVersion : int
```

At least one distinct ticket.

No client idempotency key.

## Atomic batch

Preflight all newly targeted tickets before mutation.

If one newly targeted ticket is ineligible:
```text
void none
```

Already-Voided requested tickets are already satisfied.

If all already Voided:
- no new task;
- no version increment;
- return current truth.

If mix already-Voided + newly eligible:
- mutate only new eligible tickets;
- one new task targets only newly changed docs;
- return all requested truth.

ExpectedDocumentVersion is required for non-Voided target and must equal current version.

---

# 24. Void not eligible -> Refund path

If any of these apply:

```text
VoidDeadline null
window closed
issuing office missing/mismatch
coupon not Open
control not Local
Authority External
```

do not mutate.

Return precise business disposition:

```text
VOID_NOT_ELIGIBLE_REFUND_REQUIRED
```

HTTP 409.

If current error model supports structured metadata expose:

```text
RefundRequiredInstead = true
```

Do not implement refund.

---

# 25. Order summary after Void

Void does NOT cancel commercial services.

After Void:
- services remain Active;
- reservation remains its truthful state;
- document truth becomes Voided.

Recompute summary:

```text
all active Air still covered by non-void ETKT -> Ticketed
otherwise -> active reservation summary
```

Typical full ticket void with all capacity Confirmed:

```text
OrderStatus = Confirmed
```

Partial multi-passenger void may also summarize Confirmed while other ETKTs remain issued; document truth is authoritative.

Never auto-cancel service from Void.

---

# 26. Required lifecycle

Must work:

```text
Create
-> Reserve
-> Confirm
-> Issue
-> VoidDocuments
-> Order summary Confirmed
-> CancelOrder/CancelServices
-> CancelConfirmed capacity
-> OrderChange(Cancel)
-> Cancelled when no active scope remains
```

Do not create combined Void+Cancel command.

---

# 27. ElectronicTicketVoided V1

Publish existing V1 event truthfully.

Map:

```text
ElectronicTicketId = ticket.Id
OrderId = ticket.CurrentServicingOrderId
DocumentNumber = ticket.DocumentNumber
OperationId = VoidRecord.VoidFulfillmentTaskId
Reason = command VoidReason
ReasonDetail = VoidRecord.ReasonText
VoidedBy = current ActorId
VoidedAt = VoidRecord.VoidedAt
ProviderReference = null
DocumentVersion = current version
```

Outbox must be same transaction as Void.

Do not rename V1 fields.

---

# 28. OrderCancelled V1 disposition

Existing V1 carries legacy:
- required AirlineOfficeId;
- TrafficDocument IDs;
- old pricing assumptions.

No current consumer was found.

Required disposition:

```text
ORDER_CANCELLED_V1 = DEFERRED_LEGACY_INCOMPATIBLE
```

Do not emit fake data.
Do not invent V2 without consumer.

Cancellation truth is exposed through Order/OrderChange/Service query state.

---

# 29. Query/read model

Add:

## OrderItem
```text
EndedByChangeId
```

## OrderService
```text
EndedByChangeId
```

## OrderChange read history
Expose:

```text
Id
ChangeType
CommercialVersion
ActorId
Channel
ContextType
PrincipalType
SourceReference
SourceSystem
ReasonCode
IsInvoluntary
WaiverCode
CommittedAt
```

## ETKT
Expose:

```text
VoidDeadline
VoidRecord:
  VoidFulfillmentTaskId
  ReasonCode
  ReasonText
  ProviderReference
  ActorId
  VoidedAt
```

Coupon `FinancialStatus=Void` must project.

Backoffice Order detail must allow reconstruction of:

```text
sale -> reservation -> issue -> void -> cancellation
```

without command DB inspection.

---

# 30. Persistence/migrations

Command migration:
- OrderItems.EndedByChangeId
- OrderServices.EndedByChangeId
- Stage-5 OrderChange fields
- ETKT VoidRecord fields
- any needed ETKT VoidDeadline mapping change

Query migration:
- corresponding read fields/tables;
- OrderChange read model if required.

Backfill only truthful defaults:

Existing item/service:
```text
EndedByChangeId = null
```

Existing OrderChange:
```text
SourceSystem = null
ReasonCode = null
IsInvoluntary = false
WaiverCode = null
```

Existing Stage-4 ticket:
```text
preserve VoidDeadline as-is
```

No historical deadline inference.

Apply command/query migrations to configured local DB and verify history.

FlightFlow should normally need no schema migration.

---

# 31. Cancellation exact algorithm

## A. Lock/load

1. acquire `reservation:{orderId}`;
2. load Order;
3. authorize;
4. resolve requested active scope;
5. dependency closure;
6. load tickets;
7. block active document coverage;
8. if requested scope already fully cancelled -> no-op.

## B. Safety

9. load reservations;
10. load overlapping unresolved tasks;
11. block unresolved competing effects;
12. classify resource obligations.

## C. Provider effects

Per reservation:

```text
None -> no mutation
Held whole-operation -> Release
Held partial with retained scope -> reject
Confirmed target units -> CancelConfirmed
terminal negative -> no mutation
Unknown/Mixed/Pending/Waitlisted -> stop
```

## D. Commercial commit

Only after all resource obligations settled:

13. append one OrderChange(Cancel);
14. increment CommercialVersion once;
15. cancel selected/dependency-closed active services;
16. close Items with no active services;
17. derive Order summary;
18. project query;
19. SaveChanges.

No pricing delta without source.

---

# 32. CancelConfirmed recovery exact algorithm

For resumable task:

1. load task including targets/attempts/interactions;
2. get original CancelConfirmed request;
3. missing -> dedicated 500 / zero provider call;
4. StartAttempt;
5. RecordRequest with exact same persisted payload;
6. persist request/attempt before external call according to current pattern;
7. replay exact request;
8. success -> targeted units Cancelled;
9. indeterminate -> targeted units Unknown, task Unknown;
10. coded state -> apply observed status, task Failed;
11. generic no-evidence recovery refusal -> remain Unknown/resumable.

Never rebuild SeatHoldReferences from current state on recovery.

---

# 33. Errors

Allocate next free Ordering codes after Stage 4.

Cover at least:

```text
service not found
service not active
document servicing required
seat-only cancellation deferred
partial Held cancellation unsupported
resource unresolved
cancel recovery request missing
ticket not found in Order
document version conflict
VoidDeadline missing
void window closed
issuing office missing/mismatch
ticket status not voidable
coupon status not voidable
coupon control not local
external Void unsupported
```

Use:
```text
404 missing entity
409 conflict/stale/authority/resource
422 unsupported business/source completeness
500 impossible persisted invariant
```

Already-Voided replay is no-op, not error.

---

# 34. Mandatory domain tests

Commercial:

```text
D01 cancel after resource settled
D02 one OrderChange(Cancel)
D03 CommercialVersion once
D04 Service EndedByChangeId
D05 Item closes only when all services ended
D06 partial Item stays Active
D07 rows/history preserved
D08 Pricing unchanged without source quote
D09 CustomerTotal not zeroed
D10 repeated cancellation no new change/version
```

Dependencies:

```text
D11 Air includes Seat
D12 adult includes dependent INF
D13 INF-only does not cancel parent Air
D14 Seat-only rejected
```

Void:

```text
D15 exact DocumentVoidRecord
D16 local Issued/Open/Local within deadline can void
D17 coupons -> Void
D18 ETKT -> Voided
D19 DocumentVersion +1
D20 repeat no increment
D21 Used blocks
D22 Exchanged blocks
D23 Refunded blocks
D24 Suspended blocks
D25 non-Local control blocks
D26 External authority blocks
D27 null deadline blocks
D28 exact deadline blocks
D29 office authority required
D30 Void does not cancel service
```

---

# 35. Mandatory acceptance tests

Cancellation:

```text
C01 Created/Held full cancel -> Release -> Cancelled
C02 Confirmed full cancel -> CancelConfirmed -> Cancelled
C03 targeted confirmed segment -> selected unit Cancelled, retained active
C04 partial Held shared hold -> rejected, no mutation
C05 full Held -> Release
C06 Released resource -> commercial cancel without new provider call
C07 Expired resource -> same
C08 Unknown Confirm task blocks
C09 Unknown CancelConfirmed leaves service Active/recoverable
C10 recovery replays exact request once
C11 missing original recovery request -> no provider call
C12 generic no-evidence recovery refusal stays Unknown
C13 multi-reservation partial provider success + Unknown -> no commercial commit
C14 active ETKT blocks cancellation
C15 Void then Cancel full lifecycle works
C16 repeated cancel -> no new version/task
C17 partial cancel Item status correct
C18 RecordLocator preserved
C19 no Payment/JetPay
C20 no invented cancellation PricingLine
```

Local Void:

```text
V01 new ticket gets next-local-midnight deadline
V02 not IssuedAt+24h
V03 near-midnight short window
V04 timezone/DST conversion from source TimeZoneId
V05 no issuing office -> null deadline
V06 missing office timezone -> null deadline
V07 null deadline -> refund-required
V08 matching airline office -> success
V09 mismatch -> denied
V10 before deadline success
V11 exact deadline denied
V12 after deadline denied
V13 coupon/ticket/record/version exact
V14 multi-ticket atomic
V15 one ineligible -> none newly voided
V16 all already voided -> no new task/version
V17 mix old/new -> only new mutate
V18 version mismatch -> zero mutation
V19 Void task Succeeded / zero interactions
V20 V1 OperationId == VoidFulfillmentTaskId
V21 no fabricated OrderCancelled V1
V22 full Void + confirmed resource -> Order Confirmed
V23 Void alone keeps services Active
```

---

# 36. Concurrency tests

```text
O01 Cancel vs Confirm serialized by order lock
O02 Cancel vs Issue conflict-safe
O03 Cancel blocked while active ticket exists
O04 concurrent Void -> one mutation/task
O05 concurrent Cancel -> one commercial change
O06 FlightFlow CancelConfirmed vs Confirm coherent
O07 CancelConfirmed vs Release/Expire coherent
O08 multi-flight CancelConfirmed common lock set
```

---

# 37. Sabotage tests

FlightFlow:

```text
SFF01 remove reload-under-lock
SFF02 use flight.ToString lock
SFF03 permit partial mixed cancel
SFF04 unknown ref returns success
SFF05 replay decrements capacity again
SFF06 undocumented 2xx accepted
```

Ordering:

```text
SO01 cancel ticketed scope without Void
SO02 commercial commit before provider settlement
SO03 Unknown -> Failed
SO04 rebuild recovery request
SO05 partial Held releases full hold
SO06 seat-only silently cancelled
SO07 delete historical service
SO08 zero CustomerTotal
SO09 ignore void office
SO10 IssuedAt+24h
SO11 null deadline allowed
SO12 Used coupon allowed
SO13 Void auto-cancels service
SO14 fake external local Void
SO15 add Payment/JetPay gate
```

Restore production code after sabotage.

---

# 38. Regression

Run all Stage 1–4 relevant suites:

```text
Create
pricing/fare topology
Reserve
Release
Confirm
recovery
deadline
provider contract
Issue eligibility
IssueOrder
DocumentStock
ETKT persistence
FulfillmentTask target migration
Stage-4 conformance
query projection
```

Do not weaken closed assertions.

---

# 39. Build / DB

Run build per repo guidance.

Ordering:
- command migration applied;
- query migration applied;
- migration history verified.

FlightFlow:
- focused domain/provider/concurrency tests;
- no schema migration unless genuinely required.

If blocked:

```text
ENVIRONMENT_BLOCKED: <suite> — <exact reason>
```

No false pass claims.

---

# 40. Scope guardrails

Must remain absent:

```text
Payment/JetPay
Refund implementation
Exchange/Reissue
EMD execution
external document void provider
DCS
Stage 6 work
```

Keep Stage-4 ETKT/price/stock/issue semantics unchanged except additive VoidDeadline computation for new local issues.

---

# 41. Donor dispositions

Historical donor may be inspected.

KEEP AS EVIDENCE:
- DocumentVoidRecord concept;
- resource-first cancellation then commercial commit;
- recovery-before-finalization pattern.

REDESIGN:
- generic Operation -> FulfillmentTask;
- OperationId -> VoidFulfillmentTaskId;
- generic document target -> ElectronicTicket;
- cancellation quote dependency -> source-gated.

DO NOT PORT:
- deterministic/unconfigured fake void providers;
- Payment/Funding behavior;
- donor EMD;
- TrafficDocument;
- fake cancellation quote;
- payment void.

---

# 42. Definition of done

Stage 5 is complete only if:

> Unissued active commercial scope can be cancelled only after truthful provider-resource settlement; Held capacity uses safe Release, Confirmed capacity uses corrected recoverable CancelConfirmed, Unknown never becomes guessed success/failure, and only then durable OrderChange/EndedByChangeId history is committed. A locally issued ETKT can be voided only under same-day issuing-office authority with open/local coupons, producing immutable DocumentVoidRecord/coupon/ticket history. Cancel, provider cancellation and document Void remain separate truths, with no invented payment/refund/EMD/external-provider semantics.

---

# 43. Mandatory completion report

Return:

```text
STAGE 5 IMPLEMENTATION REPORT

SOURCE
Ordering before HEAD:
Ordering after HEAD:
FlightFlow before HEAD:
FlightFlow after HEAD:
unexpected source drift:

AUTHORITY
Master:
Stage-5 authority:
CLAUDE updated:

FLIGHTFLOW CANCEL-CONFIRMED
lock identity:
reload-under-lock:
exact selected-reference validation:
success response:
same-effect replay:
error codes:
atomicity:
capability matrix:
concurrency tests:

ORDERING CANCEL-CONFIRMED
provider request persistence:
provider idempotency key:
recovery:
unknown behavior:
coded state behavior:
reservation unit/root mapping:

COMMERCIAL CANCELLATION
endpoint:
scope/dependency closure:
Held handling:
Confirmed handling:
terminal-negative handling:
partial Held disposition:
ticketed-scope guard:
OrderChange:
CommercialVersion:
EndedByChangeId:
item status:
OrderStatus:
PNR/history:
pricing decision disposition:

LOCAL VOID POLICY
source policy:
issuing office timezone:
new Issue VoidDeadline:
historical null deadline:
same-office guard:
deadline boundary:

ETKT VOID
endpoint:
DocumentVoidRecord:
coupon rules:
ticket rules:
DocumentVersion:
Void task:
ProviderInteractions:
Order summary after Void:
commercial service mutation from Void:

WIRE / QUERY
ElectronicTicketVoided V1:
OrderCancelled V1 disposition:
OrderChange read history:
void read history:

SOURCE-GATED
CANCELLATION_PRICING_DECISION:
EXTERNAL_ETKT_VOID:
EMD_VOID:
Refund:

MIGRATIONS / DB
Ordering command migration:
Ordering query migration:
DB applied:
history verified:
FlightFlow migration:

TESTS
FlightFlow focused:
Ordering domain:
Ordering cancellation acceptance:
Void acceptance:
concurrency:
Stage1-4 regression:
sabotage:
environment-blocked:

SCOPE CHECK
Payment/JetPay added: NO
Refund implemented: NO
Exchange/Reissue added: NO
EMD execution added: NO
external document provider invented: NO
DCS added: NO
Stage 6 started: NO

FINAL
STAGE5_READY_FOR_FINAL_REVIEW
```

If a true implementation-critical source contradiction exists:

```text
BLOCKED_SOURCE: <exact evidence>
```

Do not start Stage 6.
