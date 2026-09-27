# Coding Agent Prompt — AeroTech Ordering Stage 3 v2.0 FINAL

## Stage 3 — Confirm Reserved Capacity (Payment-Neutral)

### Target repository

`https://github.com/aliifarhadi/AeroTech.Ordering.Final`

Branch:

`k8s-stg`

Baseline reviewed HEAD:

`85f48bb652d96ce155c96ee07045223656db1ecb` — `Stage 2- Final`

### Domain authority

Use, in this order:

1. `AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`
2. Current Stage-1/Stage-2 merged domain behavior on `k8s-stg`
3. Actual current AirPrice reservation-validation contract
4. Actual current FlightFlow ConfirmHold contract

Historical v1/v2/v3 repositories are evidence only and MUST NOT override this prompt.

---


## Mandatory pre-start evidence report

Before changing code, produce a short report containing:

1. exact FlightFlow source/contract files inspected for ConfirmHold;
2. the five capability facts from section 6 with evidence;
3. whether any capability remains `Unknown`;
4. the recovery branch selected by this prompt;
5. confirmation that no provider capability was inferred from the Ordering adapter alone.

If actual FlightFlow source is unavailable, do not invent the answer. Implement only capability-independent success/rejection behavior and mark ambiguous-recovery acceptance scenarios `BLOCKED_SOURCE`, then stop that branch.

---
# 1. Stage goal

Materialize one working PSS capability:

```text
Held provider reservation
    ↓
check hard ticketing deadline
    ↓
refresh AirPrice reservation validation if stale
    ↓
confirm HoldThenConfirm provider reservation
    ↓
record exact success / rejection / uncertainty
    ↓
reservation becomes Confirmed when authoritative success is known
```

There is **NO Payment / JetPay dependency in Stage 3**.

Do not create:

- PaymentIntent
- PaymentSession
- OrderPaymentCoverage
- IPaymentProvider
- mock payment success
- payment feature flag
- funding/guarantee gate
- ETKT/EMD
- DocumentStock

Stage 3 ends at confirmed reservation capacity.

---

# 2. Commands / use cases

Implement these semantic use cases:

## 2.1 `ConfirmReservedCapacity`

Input:

```text
OrderId: long
```

Behavior:

- resolve all current, non-terminal `FulfillmentReservation`s belonging to the Order;
- only `ReservationMode.HoldThenConfirm` reservations in `Held` state require a provider confirmation effect;
- `ImmediateConfirm` reservations already in `Confirmed` are satisfied and are not called again;
- `ReservationMode.None` creates no confirmation work;
- already `Confirmed` reservations are idempotently skipped;
- terminal `Rejected/Released/Expired/Cancelled` reservations are not confirmed;
- an existing unresolved Confirm effect (`Unknown`) must not be blindly replayed.

## 2.2 `ConfirmReservations`

Targeted variant:

```text
OrderId: long
ReservationIds: IReadOnlyCollection<long>
```

It uses exactly the same planner, invariants and transition rules as `ConfirmReservedCapacity`.

This exists so later ancillary/servicing flows do not need a new confirmation model.

Do not create a second confirmation implementation path.

---

# 3. Intrinsic preconditions

For each target Held reservation:

1. Order belongs to the requested scope.
2. Reservation belongs to the Order.
3. `Mode == HoldThenConfirm`.
4. `Status == Held`.
5. `ProviderOperationRef` exists.
6. provider hold has not definitively expired.
7. `Order.LastTicketingDate` has not passed.
8. AirPrice reservation-validation evidence for that reservation scope is current; otherwise revalidate before Confirm.
9. no existing unresolved Confirm external effect conflicts with the new call.

**Payment is not a precondition.**

---

# 4. Reservation validation refresh

Current Stage 2 persists:

```text
FulfillmentReservation.ReservationValidationTimeLimit
```

Final Stage-3 semantics:

> It is the validity of the latest accepted AirPrice reservation-validation evidence for that reservation scope.

If:

```text
ReservationValidationTimeLimit > now
```

use it.

If stale or absent:

```text
ReservationValidationTimeLimit <= now
or null
```

then before any provider Confirm call:

1. reconstruct the exact current AIR service scope covered by this reservation's units;
2. call the existing authoritative AirPrice reservation validation using the accepted fare topology already preserved on the Order;
3. if validation succeeds, replace `ReservationValidationTimeLimit` with the new returned `TimeLimit`;
4. if the returned TimeLimit is already expired at decision time, do not Confirm;
5. if validation rejects/reprices/is unsupported, do not call FlightFlow ConfirmHold;
6. the reservation remains Held unless another authoritative provider event changes it.

Do not create a new reservation just because validation became stale.

Do not recompute or mutate:

- `RequestedExpiresAt`
- `ProviderOperationRef`
- `ExpiresAt`
- PNR / `Order.RecordLocator`

The fresh validation only renews eligibility evidence.

---

# 5. Fare topology rule

AirPrice revalidation must use the accepted `OrderFarePricingUnit` / `OrderFareComponent` topology already persisted by Stage 1/2.

Do not reconstruct pricing-unit grouping from:

- one bound = one pricing unit;
- one AirFareId = one pricing unit;
- array order;
- current segment order alone.

The existing AirPrice validator behavior is the source-backed baseline.

If the current accepted PricingUnit semantic cannot be mapped to the AirPrice reservation-validation contract:

```text
BLOCKED_SOURCE
```

before FlightFlow Confirm.

Do not invent a JourneyType.

---

# 6. Provider capability verification gate — MANDATORY BEFORE IMPLEMENTATION

Do **not** infer FlightFlow capabilities from the current Ordering adapter. The reviewed Ordering code currently shows only that its Confirm call posts the HoldId; that is not authoritative evidence about server-side idempotency, duplicate semantics or read-back.

Before implementing Confirm recovery, inspect the actual current FlightFlow source/contract available to you and report the evidence for **ConfirmHold specifically**:

```text
AcceptsIdempotencyIdentity = VerifiedYes | VerifiedNo | Unknown
DuplicateMutationSemantics = SameEffect | Conflict | NewEffect | Unknown
SupportsAuthoritativeReadBack = VerifiedYes | VerifiedNo | Unknown
SupportsSafeReplayAfterAmbiguousOutcome = VerifiedYes | VerifiedNo | Unknown
ResultGranularity = AtomicOperation | PerUnit | Mixed | Unknown
```

Rules:

- `Unknown` is not equivalent to `No`.
- The existence of `FulfillmentTask.IdempotencyKey` or reservation `IdempotencyKey` does not prove FlightFlow accepts it.
- The absence of a key in current Ordering's `ConfirmHoldRequest` does not prove FlightFlow lacks duplicate protection or another read API.
- Do not change FlightFlow contracts just to satisfy this Stage.

Then implement the corresponding frozen Master branch:

```text
if authoritative read-back is verified:
    on ambiguous Confirm -> reconcile using the authoritative provider identity

if read-back is absent/inconclusive AND safe same-effect replay is verified:
    persist exact provider request/idempotency identity and replay that exact effect

if neither is verified:
    on ambiguous Confirm -> keep Unknown; no blind replay; no destructive release

if capability cannot be established from accessible source:
    BLOCKED_SOURCE for the recovery branch; do not guess
```

The normal success call uses only fields actually defined by the verified FlightFlow contract. Payment data is never part of Stage 3.

---

# 7. Confirmation execution ownership

Use the existing `FulfillmentTask` aggregate.

Stage-3 task type:

```text
OrderFulfillmentTaskType.ConfirmInventory
```

Provider interaction type:

```text
ProviderInteractionType.ConfirmHold
```

Current reservation-specific task target shape is sufficient for Stage 3.

Do **not** generalize FulfillmentTask targets for ETKT/EMD in Stage 3. That belongs to Issue Stage.

A confirmation task covers the ReservationUnit IDs belonging to that reservation.

---

# 8. Provider request evidence

Before dispatching ConfirmHold:

- persist exact semantic request evidence;
- bind it to Task + Attempt + ProviderInteraction;
- retain request/response hashes and raw bounded response evidence according to existing Stage-2 behavior.

`ProviderInteraction.IdempotencyKey` is populated only if the verified Confirm operation accepts an authoritative provider idempotency identity. Otherwise it remains null. Null means **unverified/not recorded**, not "provider is non-idempotent".

`FulfillmentTask.IdempotencyKey` remains Ordering's stable logical-effect identity. Send it (or any derived key) to FlightFlow only if the real Confirm contract explicitly defines the corresponding semantics.

---

# 9. Success transition

On definitive FlightFlow ConfirmHold success:

```text
FulfillmentTask -> Succeeded
FulfillmentReservation -> Confirmed
all covered ReservationUnits -> Confirmed
```

Preserve historical fields:

```text
ProviderOperationRef
ProviderUnitRef
RequestedExpiresAt
ExpiresAt
ReservationValidationTimeLimit
```

Do not clear the old hold expiry merely because the reservation is now Confirmed. It remains historical provider evidence; expiry processing must ignore confirmed resources.

No new PNR is generated.

---

# 10. Definitive provider rejection

If FlightFlow returns a definitive provider/business rejection and the response proves the Confirm effect did **not** occur:

```text
FulfillmentTask -> Failed
FulfillmentReservation remains Held
ReservationUnits remain Held
```

A failed confirmation is **not** evidence that the hold was released/rejected/expired.

Do not automatically release the hold.

Do not compensate another provider's successful confirmation.

If business later explicitly retries after a definitive no-effect rejection and the hold is still valid, it is a new Confirm task/attempt according to current command policy; it is never a replay of an uncertain provider effect.

---

# 11. Ambiguous/unknown Confirm outcome

For FlightFlow ConfirmHold, any outcome where external effect may have occurred but no authoritative result is known must be treated as **Unknown**, including at minimum:

- request timeout;
- connection lost after dispatch;
- response lost;
- ambiguous/malformed response;
- server/transport failure where the contract cannot prove no confirmation effect occurred.

Ambiguous Confirm always first records truthful uncertainty; the recovery action then depends on the verified capability matrix from section 6.

Initial transition on ambiguous outcome:

```text
FulfillmentTask -> Unknown
FulfillmentReservation -> Unknown
covered ReservationUnits -> Unknown
```

Preserve all provider references and previous evidence. Then:

- **Read-back verified:** reconcile before any mutation replay.
- **Safe same-effect replay verified:** replay only the exact persisted provider request under the same verified provider idempotency identity.
- **Neither verified:** no automatic replay and no destructive release; remain Unknown pending authoritative evidence/manual reconciliation.
- **Capability unknown:** report `BLOCKED_SOURCE`; do not choose a branch from convenience.

In every branch, do not mark the task Failed merely because a retry budget elapsed. The resource may already be Confirmed remotely.

---

# 12. Duplicate/idempotent command behavior

## Already Confirmed

Calling Confirm again on an already `Confirmed` reservation:

```text
no FlightFlow call
no new task
no new interaction
success/idempotent result
```

## Existing Unknown confirmation

Calling Confirm while a prior Confirm task is Unknown follows the verified recovery capability:

```text
read-back verified -> reconcile
safe same-effect replay verified -> replay exact persisted effect
neither/unknown -> no second mutation; unresolved / BLOCKED_SOURCE as applicable
```

Do not convert Unknown to Failed.

## ImmediateConfirm

No provider confirm call.

## No HoldThenConfirm scope

The full-order command is an idempotent no-op success when all applicable reservations are already satisfied.

---

# 13. Time races

## Hold expires before dispatch

No provider call.

If provider automatic-expiry semantics are certified, normal expiry behavior may settle it; otherwise keep provider truth conservative and reconcile according to Stage-2 rules.

## LastTicketingDate passes before dispatch

No AirPrice call and no FlightFlow Confirm call.

The command is rejected. Existing deadline/release behavior owns resource cleanup.

## Validation expires between pre-check and provider call

Re-check the validation/time constraints immediately before dispatch. Do not knowingly send Confirm with stale validation evidence.

## Deadline/validation passes after provider dispatch but provider later returns success

Record the authoritative provider success truth.

Do not falsify it as failure because local time advanced during the call.

Subsequent Issue eligibility re-checks the hard ticketing deadline/current authoritative rules independently.

---

# 14. Multi-provider behavior

Each `FulfillmentReservation` confirmation is independent.

Example:

```text
Provider A -> Confirmed
Provider B -> definitive rejection / Unknown
```

Rules:

- keep Provider A Confirmed;
- do not auto-cancel/compensate A;
- B retains its own truthful state;
- Order summary/read model reflects incomplete/unconfirmed reservation coverage;
- Issue in the later Stage must require every reservation-required target in the requested issue scope to be Confirmed/committed.

No distributed transaction.

---

# 15. Order summary

`OrderStatus` remains a summary and not eligibility authority.

Stage 3 may update summary behavior so:

- all reservation-required active scope conclusively Confirmed/ImmediateConfirm -> `OrderStatus.Confirmed`;
- any confirmation Unknown/mixed/unconfirmed -> `OrderStatus.ReservationUnconfirmed`;
- terminal reserve failures continue to follow existing Stage-2 semantics.

Do not use or transition to:

```text
Paying
Paid
PaymentFailed
PaymentUnconfirmed
```

Those are legacy/reserved values and are not part of payment-neutral Stage 3.

---

# 16. Domain changes authorized in Stage 3

Authorized:

1. add confirmation behavior to `FulfillmentReservation` and `ReservationUnit`;
2. allow `ReservationValidationTimeLimit` to be refreshed by a successful fresh AirPrice validation while reservation remains Held;
3. use existing `ConfirmInventory` task type;
4. use existing `ConfirmHold` interaction type;
5. extend the provider-neutral Reservation port only as required to express confirmation semantics and the **verified** recovery capability;
6. add Order confirmation-summary behavior if needed to represent Unknown/partial confirmation honestly;
7. narrow source-preservation correction: carry `CabinClassId` from the already-existing Offer wire into accepted Air Service history, because Master v2.0 identifies this as confirmed current-source data loss needed by later Issue/DCS/RA. Do not infer/backfill old rows.

Not authorized:

- new aggregate root;
- Payment-related domain;
- ETKT/EMD/DocumentStock;
- generalized document FulfillmentTask targets;
- new fare-construction hierarchy;
- new provider status vocabulary not sourced by FlightFlow;
- speculative read-back endpoint;
- speculative provider idempotency/read-back semantics not verified from actual FlightFlow source.

---

# 17. Acceptance scenarios — mandatory

At minimum, implement automated tests for all of these.

## C01 — fresh Held confirmation success

Given:

- Held FlightFlow reservation;
- fresh validation;
- hold not expired;
- ticketing deadline open.

Then:

- exactly one ConfirmHold provider effect;
- task Succeeded;
- reservation + units Confirmed;
- provider refs unchanged;
- no Payment/JetPay access.

## C02 — stale validation refreshed before Confirm

- Held remains physically valid;
- validation stale;
- AirPrice validation succeeds with new TimeLimit;
- exactly one AirPrice refresh before Confirm;
- persisted validation TimeLimit becomes new value;
- then Confirm succeeds.

## C03 — stale validation rejected

- AirPrice rejects / fare no longer valid;
- zero FlightFlow Confirm calls;
- reservation stays Held;
- no new reservation is synthesized.

## C04 — unsupported fare semantic

- AirPrice validation mapping is unsupported;
- fail closed before FlightFlow;
- zero Confirm calls;
- report existing `BLOCKED_SOURCE`/unsupported-contract behavior.

## C05 — hold already expired

- zero FlightFlow Confirm calls;
- no false Confirmed state.

## C06 — last ticketing date passed

- zero AirPrice calls;
- zero FlightFlow Confirm calls;
- no payment calls;
- command rejected before provider effect.

## C07 — already Confirmed

- duplicate command is no-op;
- zero provider calls;
- no new task.

## C08 — ImmediateConfirm provider

- no extra Confirm effect;
- treated as already satisfied.

## C09 — definitive provider rejection

- task Failed;
- reservation/units remain Held;
- hold is not auto-released.

## C10 — Confirm timeout / lost response

- task Unknown;
- reservation/units Unknown;
- no automatic second Confirm call;
- no automatic release;
- no auto-expiry destructive transition.

## C11 — command repeated after Unknown

- zero second provider mutation;
- result reports unresolved/reconciliation required.

## C12 — multi-provider success + rejection

- successful provider remains Confirmed;
- failing provider does not compensate successful provider;
- Order summary indicates incomplete confirmation.

## C13 — multi-provider success + Unknown

- successful provider remains Confirmed;
- Unknown retained honestly;
- no cross-provider compensation.

## C14 — validation refresh extends beyond hold expiry

- provider hold expiry still wins resource eligibility;
- fresh AirPrice TimeLimit does not resurrect an expired hold.

## C15 — provider returns success after local deadline crosses during call

- authoritative success is recorded;
- later Issue stage independently rejects if hard ticketing deadline is already closed.

## C16 — no HoldThenConfirm scope

- command is idempotent success/no-op.

## C17 — targeted confirmation

- target reservation subset only;
- untargeted Held reservations untouched;
- same validation/provider rules as full-order confirmation.

## C18 — sabotage guards

Break each rule one at a time and prove tests fail:

1. allow Confirm with stale validation;
2. mark reservation Confirmed on timeout;
3. retry unknown Confirm automatically;
4. release hold on definitive Confirm rejection;
5. add a Payment prerequisite;
6. use provider success to extend LastTicketingDate.

---

# 18. Completion report

Return a concise report with:

1. exact domain behaviors added;
2. exact existing fields whose semantics changed (if any);
3. no new fields unless authorized above;
4. command/use-case behavior;
5. provider confirmation mapping;
6. AirPrice stale-validation refresh behavior;
7. definitive rejection behavior;
8. Unknown behavior and why no replay is performed for FlightFlow;
9. Order summary behavior;
10. full test counts;
11. any real `BLOCKED_SOURCE` discovered.

Do not propose Stage 4 implementation in this report.

---

# Final guardrail

Stage 3 is accepted only if this statement is true:

> A real Held airline reservation can be safely confirmed end-to-end with fresh fare validation and truthful external-effect recovery, without Payment, JetPay, ETKT or EMD assumptions.

