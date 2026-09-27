# Coding Agent Prompt — AeroTech Ordering Stage 3 v2.1 FINAL

## Stage 3 — Confirm Reserved Capacity + Required Provider Corrections

### Target repositories

Ordering:
- `https://github.com/aliifarhadi/AeroTech.Ordering.Final`
- branch: `k8s-stg`
- reviewed HEAD: `85f48bb652d96ce155c96ee07045223656db1ecb` (`Stage 2- Final`)

FlightFlow:
- `https://github.com/aliifarhadi/Aerotech.FlightFlow`
- branch: `k8s-stg`
- reviewed HEAD: `6e936dbf9f9b9e4c9608c0082d2254bd17d3a3e1`

Before changing code, fetch both repositories again and report the actual current HEADs. If either HEAD changed, inspect the changed source before applying this prompt; do not mechanically patch stale code.

## Authority

1. `AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md` remains the canonical project authority.
2. This document is the Stage 3 v2.1 correction and supersedes Stage 3 v2.0 where they differ.
3. Stage 1 and Stage 2 remain closed. Corrections below may touch Stage-2 implementation only where a proven defect prevents correct Stage-3 behavior; do not redesign Stage-2 aggregates.
4. Historical v1.x Stage-3 prompts are not authority.
5. No provider capability may be inferred from the Ordering adapter alone.

## Frozen scope

Stage 3 remains Payment-Neutral. Do not add or call:
- JetPay
- PaymentIntent
- PaymentSession
- OrderPaymentCoverage
- mock payment
- payment feature flag
- funding gate
- ETKT
- EMD
- DocumentStock

The Stage ends with truthful confirmation of reserved provider capacity and deterministic recovery of an ambiguous Confirm effect.

---

# 1. Mandatory implementation order

Implement in this order:

1. Correct FlightFlow Confirm/expiry semantics and prove them with provider tests.
2. Only after those tests pass, implement Ordering Confirm recovery against the corrected contract.
3. Apply the narrow Ordering defects C/D/F and CabinClassId source-preservation correction.
4. Run Stage-3 acceptance/sabotage tests.

Do not make Ordering treat the current ambiguous FlightFlow `204` as authoritative success.

---

# 2. FlightFlow correction — ConfirmHold

Current source facts are already verified at the reviewed HEAD:
- `POST Service/v1/Flights/Seat-Holds/{id}/Confirmations` accepts only HoldId.
- unknown HoldId reaches an empty flight set and returns `204`.
- a known batch with no `Held` seats performs no mutation and also returns `204`.
- an actual confirmation also returns `204`.
- no authoritative public Confirm read-back exists.
- no Confirm idempotency identity is accepted.
- the current Confirm path does not check `ExpiresAt` before mutating Held seats.

Therefore the current contract is NOT sufficient for Stage 3.

## Required semantic contract

Make Confirm by HoldId a deterministic same-effect operation.

For one HoldId, the provider must distinguish these outcomes:

### A. Target exists and is already fully Confirmed
Return definitive success without a second mutation.

### B. Target exists, is fully Held, and every Held member is still provider-confirmable at provider clock
Atomically confirm the complete target scope and return definitive success.

### C. Target is unknown
Return a deterministic non-success outcome distinguishable from Confirmed. Do not return the same success response as case A/B.

### D. Target exists but is not confirmable
Released / Expired / Cancelled / otherwise terminal target must return a deterministic no-effect outcome distinguishable from Confirmed.

### E. Held target has crossed its provider expiry
Do not confirm it. Return deterministic no-effect outcome.

### F. Mixed/partially-confirmable batch
Do not silently partially confirm. Stage-3 FlightFlow Confirm semantics must be atomic for the addressed HoldId unless an existing source-backed contract already defines a per-unit result. If such a real contract exists, report it before changing the model.

## Recovery guarantee required

After this correction, exact replay of the same Confirm effect by the same HoldId must be safe after an ambiguous transport outcome:
- if the first call committed, replay observes already-Confirmed and returns definitive success;
- if the first call did not commit and the hold remains valid, replay performs the same intended confirmation;
- if the first call did not commit and the hold is no longer confirmable, replay returns a deterministic no-effect outcome;
- replay must never create a second/new capacity effect.

This is natural same-effect replay by HoldId. Do NOT add a fake provider IdempotencyKey/header merely to satisfy Ordering.

`AcceptsIdempotencyIdentity` for Confirm therefore remains `VerifiedNo` unless you discover an actual current FlightFlow contract proving otherwise.

## Required provider capability report after correction

Report:
- `AcceptsIdempotencyIdentity = VerifiedNo` unless real source proves otherwise.
- `DuplicateMutationSemantics = SameEffect` only after tests prove the corrected behavior.
- `SupportsAuthoritativeReadBack = VerifiedNo` unless you add/use a real authoritative read contract.
- `SupportsSafeReplayAfterAmbiguousOutcome = VerifiedYes` only after replay tests prove the corrected semantics.
- `ResultGranularity = AtomicOperation` only after tests prove no partial mutation for one HoldId.
- `ExpiryAuthority = ProviderClockGuaranteed` only after expiry job/query and Confirm-side expiry checks are corrected and tested.

Do not add a speculative GET endpoint only to satisfy this Stage. If you choose to add read-back instead of safe replay, it must read authoritative provider state and must be justified in the completion report.

---

# 3. FlightFlow correction — expiry bug

Fix the proven selector defect in `FlightRepository.GetThatHasExpiredHoldsAsync`:

Current incorrect predicate:
`seatHold.ExpiresAt > now && seatHold.Status == Held`

Required predicate:
`seatHold.ExpiresAt <= now && seatHold.Status == Held`

Also enforce provider expiry inside Confirm immediately before the mutation so a stale Held record cannot be confirmed after its provider expiry because of scheduler delay/race.

Do not redesign Stage-2 Ordering expiry semantics. Once the provider bug is fixed and tested, FlightFlow may continue to be treated as an automatically expiring provider.

Required provider tests include at least:
1. expired Held rows are selected by expiry processing;
2. future Held rows are not selected;
3. expired Held target cannot be Confirmed;
4. already Confirmed target is not expired/released by the hold-expiry path;
5. duplicate Confirm after successful Confirm is same-effect success;
6. unknown HoldId is not reported as Confirmed success;
7. released/expired/cancelled HoldId is not reported as Confirmed success;
8. a HoldId is never partially confirmed under the Stage-3 atomic contract.

---

# 4. Ordering correction — Reserve recovery must be effect-specific

Current defect: `ReserveService.ResumeAsync` selects every `FulfillmentReservation.IsUnresolved`. After a later Confirm ambiguity, the Reservation may be `Unknown` even though `ReserveInventory` already succeeded, causing the code to fetch a successful Reserve task and throw `ReservationTaskIsNotResumable` / 2763.

Correct rule:

> Reserve recovery resumes only an unresolved Reserve external effect. Reservation-level `Unknown` caused by Confirm must never resume/replay CreateHold.

Change the selection/planning logic so a reservation participates in Reserve recovery only when the corresponding latest `ReserveInventory` task/effect itself is resumable/unresolved and its persisted original `CreateHold` request exists.

Required tests:
- Reserve succeeded -> Confirm becomes Unknown -> calling Reserve does not replay CreateHold and does not throw 2763.
- genuinely ambiguous Reserve remains resumable according to the already-verified Reserve capability.
- terminal negative reservation may still be eligible for a new business Reserve according to existing Stage-2 rules.

This is a narrow Stage-2 implementation correction; do not alter aggregate ownership.

---

# 5. Ordering correction — ProviderInteraction.IdempotencyKey

Remove the generic domain guard that requires an IdempotencyKey for every provider mutation.

Canonical rule:
- `FulfillmentTask.IdempotencyKey` is Ordering's logical-effect identity and remains required as currently designed.
- `ProviderInteraction.IdempotencyKey` is nullable.
- populate it only when that exact provider operation accepts an authoritative provider idempotency identity.
- for corrected FlightFlow Confirm by HoldId, leave `ProviderInteraction.IdempotencyKey = null` unless actual provider source defines a key.

Do not replace the removed generic guard with a different generic assumption.

---

# 6. Ordering Stage-3 confirmation use cases

Implement one confirmation planner/service with two surfaces:

## ConfirmReservedCapacity
Input:
- `OrderId`

Targets all applicable current reservations.

## ConfirmReservations
Input:
- `OrderId`
- `ReservationIds`

Targets only the requested subset.

Both use exactly the same planner, validation, provider execution and recovery logic.

### Confirmation rules

For each target:
- `Mode == HoldThenConfirm`
- reservation is Held for a new Confirm effect, or has an existing unresolved Confirm task eligible for recovery;
- ProviderOperationRef exists;
- hard `LastTicketingDate` is still open before a new dispatch;
- provider hold has not definitively expired;
- AirPrice `ReservationValidationTimeLimit` is current; if absent/stale, revalidate the same current reservation scope before Confirm;
- no Payment/JetPay check.

`ImmediateConfirm` reservations are already satisfied and receive no Confirm provider call.
Already `Confirmed` reservations are idempotent no-op.
Terminal negative reservations are not Confirmed.

---

# 7. AirPrice validation refresh

Preserve Stage-3 v2.0 semantics:
- reconstruct the exact current AIR service scope covered by the ReservationUnits;
- use the accepted persisted FarePricingUnit/FareComponent topology;
- if validation is current, do not call AirPrice again;
- if stale/null, call authoritative AirPrice reservation validation;
- on success, update only `ReservationValidationTimeLimit` with the new TimeLimit;
- do not create a new reservation/hold;
- do not change RequestedExpiresAt, ProviderOperationRef, ExpiresAt or RecordLocator;
- validation reject/reprice/unsupported => zero FlightFlow Confirm calls.

Immediately before provider dispatch recheck hard deadline, provider hold expiry, and validation freshness.

---

# 8. Confirm execution evidence and recovery

Use existing `FulfillmentTask`.

Task type:
- `OrderFulfillmentTaskType.ConfirmInventory`

Interaction type:
- `ProviderInteractionType.ConfirmHold`

Persist the exact Confirm request (`HoldId`) before dispatch, plus current request/response hashes and bounded evidence according to existing conventions.

For FlightFlow Confirm:
- Task.IdempotencyKey remains local Ordering effect identity.
- ProviderInteraction.IdempotencyKey is null.
- no invented provider idempotency key is sent.

## Definitive success

Only the corrected FlightFlow contract's definitive Confirmed success may produce:
- Confirm task -> Succeeded
- reservation -> Confirmed
- all covered units -> Confirmed

Preserve ProviderOperationRef, ProviderUnitRef, RequestedExpiresAt, ExpiresAt and ReservationValidationTimeLimit as history.

## Definitive no-effect rejection

When the corrected provider proves the Confirm effect did not occur:
- Confirm task -> Failed
- do not pretend the reservation was Confirmed.
- for a fresh Held attempt, reservation/units remain Held unless the response itself is authoritative evidence of another provider resource state already modeled by the domain.
- do not auto-release another provider or create compensation.

## Ambiguous transport outcome

First record truthful uncertainty:
- task -> Unknown
- reservation -> Unknown
- covered units -> Unknown

Then recover using the verified corrected FlightFlow capability:
- replay the exact persisted Confirm request by the same HoldId;
- do not create a second Confirm task/effect identity for recovery;
- do not alter request semantics;
- definitive replay success resolves to Confirmed;
- definitive replay no-effect resolves the Confirm task as failed/no-effect and restores/applies only state actually proven by provider evidence;
- if replay is still ambiguous, remain Unknown. Never turn Unknown into Failed merely because a retry budget elapsed.

If FlightFlow safe replay cannot be proven by tests after the provider correction, STOP the Ordering recovery branch and report `BLOCKED_SOURCE`; do not blind replay.

---

# 9. OrderStatus semantic correction

Stage 3 v2.1 supersedes the Stage-2 coarse summary meaning.

Canonical summary from Stage 3 onward:
- every reservation-required active scope conclusively Confirmed / ImmediateConfirm satisfied -> `OrderStatus.Confirmed`;
- any required scope Held / Waitlisted / Pending / Unknown / Mixed / otherwise not conclusively confirmed -> `OrderStatus.ReservationUnconfirmed` where applicable;
- terminal reserve failures continue existing failure semantics.

Therefore an all-Held Order after Reserve is NOT `OrderStatus.Confirmed` anymore.

`OrderStatus` remains only a summary. Later Issue eligibility must inspect FulfillmentReservation/document truth directly.

Do not use Paying/Paid/PaymentFailed/PaymentUnconfirmed.

Required tests:
- all Held => ReservationUnconfirmed;
- all Confirmed/ImmediateConfirm => Confirmed;
- Confirmed + Held => ReservationUnconfirmed;
- Confirmed + Unknown => ReservationUnconfirmed.

---

# 10. Deadline processor correction — forward progress without falsifying provider truth

Current defect: past-LTD selector returns oldest reservable Orders, while `ReservationDeadlineService` expires the Order only when every reservation `IsSettled`. Confirmed/Unknown are not `IsSettled`, so the same oldest rows can occupy every batch indefinitely.

Canonical Stage-3 rule:

1. `LastTicketingDate` is the hard commercial deadline for the current unissued Order.
2. When it conclusively passes, the unissued Order may transition to `OrderStatus.Expired` independently of whether an external Reservation is Confirmed or Unknown.
3. Do NOT mutate a Confirmed reservation to Released/Expired merely to make the Order expire.
4. Do NOT destructively release/cancel an Unknown Confirm effect before reconciliation/recovery.
5. Once the Order is commercially Expired, it must not remain in the generic past-LTD candidate set forever.
6. Provider cleanup/reconciliation work must be selected separately only while there is actionable work that can make progress.
7. A Held reservation past LTD still requires provider cleanup according to verified release/expiry semantics.
8. An irreconcilable Unknown must remain truthful Unknown; it must not starve unrelated newer deadline work.

Refactor query/orchestration mechanics as needed to satisfy the invariant:

> Every deadline polling batch must make forward progress; non-actionable old Orders cannot monopolize the batch.

Do not reopen the Order/Reservation aggregate design.

Required tests:
- past-LTD + Confirmed reservation => Order expires commercially and is not repeatedly selected only because of LTD;
- past-LTD + Unknown Confirm => no destructive release, Order can expire commercially, Unknown remains truthful;
- past-LTD + Held => cleanup remains actionable;
- more than batch-size old non-actionable Orders cannot starve newer actionable Held cleanup.

---

# 11. CabinClassId source-preservation correction

Current source is verified:
- Offer wire `OfferFlight.CabinClassId` exists.
- domain `OfferDetail.OfferFlight` drops it.
- `OrderAirTransportService` does not persist it.

Add nullable `CabinClassId` through the accepted Offer -> domain mapping -> OrderAirTransportService -> persistence/read model paths needed by current conventions.

Rules:
- preserve source value exactly for newly accepted Orders;
- existing rows remain null;
- no backfill;
- never infer cabin from RBD/BookingClass.

---

# 12. API surface

Expose one business endpoint now:

`POST Backoffice/v1/Orders/{orderId}/Reservations/Confirmations`

Request:
- optional `ReservationIds` collection.
- omitted/null/empty => full `ConfirmReservedCapacity`.
- non-empty => targeted `ConfirmReservations`.

Use one implementation path.

Do NOT add an Internal mirror in this Stage. No concrete current internal caller has been established. Add one later only when a real caller requires it.

---

# 13. Live/staging mutation policy

Do not run Confirm against real customer/staging inventory merely as a proof test.

A live provider mutation is allowed only after the provider corrections are deployed and only when all are true:
- test-owned hold;
- disposable;
- test inventory;
- deterministic cleanup;
- no real customer reservation/seat impact.

Otherwise rely on automated/integration tests and report that live destructive test was not executed.

---

# 14. Mandatory acceptance tests

Retain Stage-3 v2.0 C01-C18 and adapt C10/C11 to the now-verified safe-replay branch.

Additionally prove:

P01 current FlightFlow unknown HoldId can no longer produce Confirmed success.
P02 duplicate Confirm after successful Confirm is deterministic same-effect success.
P03 expired Held target cannot be Confirmed.
P04 release/expired/cancelled target cannot be reported as Confirmed.
P05 safe replay after simulated lost first response resolves to Confirmed when first mutation committed.
P06 safe replay after simulated non-commit performs at most one logical Confirm effect.
P07 Reserve succeeded + Confirm Unknown never resumes CreateHold.
P08 ProviderInteraction Confirm can persist null IdempotencyKey.
P09 all-Held Order summary is ReservationUnconfirmed.
P10 deadline batch cannot be monopolized by old non-actionable Confirmed/Unknown Orders.
P11 CabinClassId is preserved for new Orders and old null rows are not inferred.
P12 no Payment/JetPay call or domain object exists in this Stage.

Sabotage tests must fail if someone:
- treats pre-dispatch guards + raw current 204 as sufficient proof of confirmation;
- auto-confirms on timeout;
- blindly replays against an uncorrected provider contract;
- replays CreateHold because Confirm made the Reservation Unknown;
- reintroduces generic ProviderInteraction mutation idempotency requirement;
- makes all-Held OrderStatus Confirmed;
- requires all reservations to be terminal before the commercial Order can expire after hard LTD;
- releases/cancels Unknown before recovery;
- invents CabinClassId from RBD;
- adds a Payment prerequisite.

---

# 15. Completion report

Return one concise report containing:
1. actual final Ordering HEAD and FlightFlow HEAD;
2. exact FlightFlow Confirm contract semantics implemented;
3. final capability matrix with source/test evidence;
4. expiry selector/Confirm expiry fixes;
5. Ordering Confirm full/targeted behavior;
6. exact Unknown recovery behavior;
7. Reserve recovery correction;
8. ProviderInteraction nullable-idempotency correction;
9. OrderStatus summary correction;
10. deadline forward-progress correction;
11. CabinClassId preservation path;
12. endpoint added and confirmation that no speculative Internal mirror was added;
13. test totals and named acceptance/sabotage coverage;
14. confirmation that no Payment/JetPay/ETKT/EMD/DocumentStock work was added;
15. any remaining real `BLOCKED_SOURCE` only if source/tests genuinely cannot establish a required capability.

## Final acceptance statement

Stage 3 is complete only if this statement is true:

> A real Held airline reservation can be confirmed with fresh AirPrice validation, FlightFlow can distinguish Confirmed from non-confirmable/unknown provider state, an ambiguous Confirm can be safely reconciled by proven same-effect replay, and Ordering preserves truthful reservation/commercial state without Payment, JetPay, ETKT or EMD assumptions.
