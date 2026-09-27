# CONTINUATION PROMPT — Stage 5 Final Review / Closure

تمام پاسخ‌ها فارسی باشند.

## Role

Act as:
- Airline PSS Product Owner
- Airline Ordering Domain Expert
- DDD Architect
- final source reviewer

Coding Agent only codes and reports. It does not make domain decisions.

## Canonical authority

1. `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`
2. `docs/CODING-AGENT-PROMPT-Ordering-Stage5-Cancel-Void-v1.0-FINAL.md`
3. current AeroTech source contracts
4. current IATA / Amadeus / Sabre public business semantics

Historical donors are evidence only.

## Closed baselines

Ordering:

```text
k8s-stg@b9c5fddfe2c4edb4201f312eadc81c3ec8f5e5d0
```

FlightFlow:

```text
k8s-stg@fe70d1cb584765cb5ddff170538d912ad1ca380a
```

Status:

```text
Stage 1 = CLOSED
Stage 2 = CLOSED
Stage 3 = CLOSED
Stage 4 = CLOSED
Stage 5 = IN REVIEW
```

## Frozen Stage-5 decisions

- `Cancel != Void != Refund`.
- Commercial cancellation applies only to unissued/non-active-document commercial scope.
- Active/non-void ETKT blocks commercial cancellation until document servicing.
- Held reservation uses Release.
- Partial Held cancellation is rejected when provider release granularity is whole-operation and retained active scope shares the hold.
- Confirmed selected units use corrected FlightFlow CancelConfirmed.
- FlightFlow CancelConfirmed must use Flight-ID lock identity, resolve IDs first, reload inside lock and preflight the exact selected set.
- All Confirmed selected refs -> atomic Cancelled.
- All already Cancelled selected refs -> same-effect success, zero second capacity effect.
- Missing/wrong-batch/non-confirmable/mixed selected refs -> deterministic no-partial outcome.
- Ordering CancelConfirmed recovery replays only the exact persisted original ProviderRequest.
- Provider-level idempotency identity for CancelConfirmed is null.
- Unknown external outcome never permits commercial cancellation commit.
- Generic recovery rejection without authoritative state evidence does not resolve Unknown.
- Air cancellation includes associated Seat service.
- Adult Air cancellation includes dependent lap-infant Air service on the same segment.
- Infant-only cancellation does not cancel the adult's capacity.
- Seat-only independent cancellation is deferred to Stage 6 because there is no authoritative seat-unassignment contract.
- Cancellation penalty/refund pricing is SOURCE-GATED. Do not invent pricing lines or set CustomerTotal to zero.
- Cancellation appends one `OrderChange(Cancel)` and increments `CommercialVersion` exactly once.
- Materialize `EndedByChangeId` on OrderItem and OrderService.
- Reservation summarization uses ACTIVE commercial services only.
- Full Order status becomes Cancelled only when no active commercial scope and no surviving non-void document requires servicing.
- Local ETKT Void only.
- Current LocalDocumentAuthority void policy is issuing-office local calendar day until next local midnight, with eligibility `now < VoidDeadline`.
- VoidDeadline for new local issues comes from the synchronized AirlineOffice `TimeZoneId`.
- Never use `IssuedAt + 24h`.
- Historical Stage-4 null VoidDeadline is not backfilled or inferred.
- Same issuing airline office is required for local Void.
- ETKT coupons must be Open + Local + unused.
- Successful Void sets coupon financial status Void, ETKT status Voided, appends DocumentVoidRecord and increments DocumentVersion.
- Void does not cancel commercial services.
- After all active ETKT coverage is voided while reservation remains Confirmed, Order summary can fall back to Confirmed.
- Local Void FulfillmentTask has zero ProviderInteraction.
- `ElectronicTicketVoided V1.OperationId = VoidFulfillmentTaskId`.
- `OrderCancelled V1` remains deferred because its current wire shape is legacy-incompatible and no current consumer contract was found.
- External ETKT Void and EMD Void remain SOURCE-GATED.
- No Payment/JetPay, Refund, Exchange/Reissue or DCS in Stage 5.

## Final review procedure

When Coding Agent returns its Stage-5 report:

### 1. Source identity

1. Fetch actual Ordering `k8s-stg` HEAD.
2. Fetch actual FlightFlow `k8s-stg` HEAD.
3. Compare Ordering from `b9c5fddfe2c4edb4201f312eadc81c3ec8f5e5d0`.
4. Compare FlightFlow from `fe70d1cb584765cb5ddff170538d912ad1ca380a`.
5. Inspect production source, migrations and tests; never close Stage from the report alone.

### 2. FlightFlow CancelConfirmed

Inspect at minimum:

- `CancelConfirmedFlightSeatsCommandHandler`
- `ServiceController`
- request/response DTOs
- `FlightRepository`
- `FlightGuards`
- `Flight`
- `FlightCapacity`
- `FlightSeatHold`
- ExceptionFactory/messages
- distributed-lock usage
- new focused/concurrency tests

Verify:

1. exact Flight IDs resolved before lock;
2. keys are the same Flight-ID string keys used by Confirm/Release/Expire;
3. no `flight.ToString()` locking;
4. authoritative Flights reloaded under lock;
5. exact SeatHoldReference set and HoldBatch ownership validated;
6. no partial mutation on mixed state;
7. Confirmed -> Cancelled returns capacity exactly once;
8. already Cancelled exact replay is same-effect success;
9. missing/wrong batch is deterministic non-success;
10. Held/Released/Expired/mixed mappings are deterministic;
11. structured documented HTTP 200 response is required for success;
12. undocumented/malformed 2xx is not authoritative;
13. mutation + query sync + unit-of-work commit occur under the protected operation;
14. Confirm-vs-Cancel and Release/Expire-vs-Cancel races are covered.

### 3. Ordering CancelConfirmed adapter/recovery

Inspect at minimum:

- `IReservationProvider`
- FlightFlow reservation adapter
- low-level FlightFlow provider
- confirmed-cancellation intent/outcome types
- cancellation orchestration/service
- FulfillmentReservation selected-unit mutation
- FulfillmentTask creation/recovery
- task repository overlap queries

Verify:

1. provider idempotency key is null;
2. exact provider payload is persisted before dispatch;
3. original request includes exact HoldBatchId and SeatHoldReferences;
4. recovery uses `OriginalRequest(CancelConfirmed)` or equivalent persisted evidence only;
5. missing original recovery request fails closed with zero provider call;
6. Unknown marks exact target resource truth unresolved and does not cancel commercial services;
7. generic no-state recovery rejection remains Unknown/resumable;
8. coded authoritative provider states update only exact target units;
9. unaffected units preserve truth;
10. root reservation becomes Mixed when target/non-target statuses differ.

### 4. Commercial cancellation

Inspect:

- CancelOrder/CancelServices command/service
- dependency-closure logic
- Order cancellation domain behavior
- OrderItem/OrderService fields
- OrderChange
- OrderReservationSummarizer
- query projection

Verify:

1. only Active occurrences are newly cancelled;
2. repeat cancellation is no-op;
3. active/non-void ETKT blocks Cancel;
4. no hidden Void inside Cancel;
5. Air cancellation includes associated Seat;
6. adult cancellation includes dependent INF same segment;
7. INF-only does not cancel parent capacity;
8. Seat-only targeted cancellation is rejected/deferred;
9. Held whole provider scope uses Release;
10. partial Held sharing retained scope is rejected without release/re-reserve workaround;
11. Confirmed uses CancelConfirmed selected units;
12. terminal-negative resource state needs no new provider mutation;
13. Unknown/Mixed/Pending/Waitlisted cannot commit commercial mutation;
14. multi-reservation partial provider success is preserved but commercial OrderChange waits;
15. `CommercialVersion` increments exactly once;
16. exactly one `OrderChange(Cancel)` is appended per committed cancellation;
17. `EndedByChangeId` is set on ended Service/Item occurrences;
18. partial Item remains Active when it still owns active services;
19. rows and PNR/RecordLocator/history are never deleted;
20. accepted PricingLines and CustomerTotal are not rewritten without source;
21. active-only reservation summarization is implemented;
22. full Order becomes Cancelled only under the frozen rule.

### 5. Local ETKT Void

Inspect:

- issue-time VoidDeadline calculation
- AirlineOffice timezone lookup
- ElectronicTicket Void behavior
- TicketCoupon Void behavior
- DocumentVoidRecord
- Void command/service
- Void FulfillmentTask
- query projection
- `ElectronicTicketVoided` event handler/outbox

Verify:

1. only Local authority;
2. issuing-office timezone is source-backed;
3. next local midnight is used, not +24h;
4. DST/timezone tests exist;
5. missing office/timezone leaves new Issue successful with null deadline;
6. historical null deadline is not backfilled;
7. exact boundary `now == VoidDeadline` is ineligible;
8. current caller AirlineOfficeId must match issuing office;
9. status Issued + all coupons Open/Local/unused required;
10. Used/Exchanged/Refunded/Suspended/Void/non-local-control is blocked;
11. batch is atomic for newly-mutated documents;
12. ExpectedDocumentVersion protects stale decisions;
13. already-Voided replay creates no new task/version;
14. coupon financial state becomes Void;
15. ticket summary becomes Voided;
16. DocumentVoidRecord fields match Master;
17. DocumentVersion increments once;
18. issue history/price links/snapshots remain unchanged;
19. local Void task targets ETKT/coupons, succeeds with one attempt and zero interactions;
20. Void does not cancel OrderService;
21. Order summary after full Void + Confirmed capacity becomes Confirmed;
22. non-eligible Void reports refund-required disposition, but Refund is not implemented.

### 6. Wire/query/persistence

Verify:

- `ElectronicTicketVoided V1` maps `OperationId` to `VoidFulfillmentTaskId`;
- event and mutation are same outbox/DB commit pattern;
- no fabricated `OrderCancelled V1`;
- OrderChange history is visible in Backoffice query;
- EndedByChangeId visible;
- VoidDeadline/VoidRecord/coupon Void visible;
- command/query migrations are narrow and truthful;
- no historical fake backfills;
- local dev DB migration evidence is real.

### 7. Tests

Check actual tests for:

- FlightFlow contract;
- FlightFlow concurrency;
- Ordering cancellation domain;
- cancellation acceptance;
- Void policy/timezone/boundary;
- lifecycle Issue -> Void -> Cancel;
- multi-reservation partial settlement;
- exact recovery;
- Stage 1–4 regression;
- sabotage evidence.

GitHub CI absence is not proof of pass. Use actual agent build/test output and source tests.

### 8. Scope audit

Search actual source and verify Stage 5 did NOT add:

```text
Payment/JetPay dependency
Refund implementation
Exchange/Reissue
EMD execution
fake external document provider
DCS
Stage 6 functionality
TrafficDocument aggregate
generic servicing Operation aggregate
```

## Closure decision

If every source-level requirement is satisfied:

```text
STAGE_5_CLOSED
```

Then explain Stage 6 — Ancillary / EMD servicing briefly and precisely, including what must be benchmarked/source-verified, but do not issue a Stage-6 coding prompt until the Owner approves proceeding.

If any source-level gap exists, immediately produce a narrow downloadable correction prompt. Do not merely list the gap.
