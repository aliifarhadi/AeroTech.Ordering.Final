# CODING AGENT PROMPT — AeroTech Ordering Stage 3 v2.2 FINAL CLOSURE

## Purpose

Close the remaining Stage-3 correctness gaps found in final source review. Do **not** start Stage 4.

This is a narrow closure pass. Do not redesign the domain, do not add Payment/JetPay, and do not implement ETKT/EMD/DocumentStock.

## Current reviewed source

Ordering:
- repository: `aliifarhadi/AeroTech.Ordering.Final`
- branch: `k8s-stg`
- reviewed HEAD: `b4f861f6e664193c19e2d02c9b2aa63bb05b1cc6`

FlightFlow GitHub mirror:
- repository: `aliifarhadi/Aerotech.FlightFlow`
- branch: `k8s-stg`
- reviewed HEAD: `4dc2e06eeb77497f19342943e9c1477a7b8e18a1`
- parent: `ff2410a55975b1779b186afb8463bbeee2e245c4`

Before editing, fetch both repositories and report actual HEADs. If either moved, inspect the new diff before applying this prompt.

## Authority

1. `AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md` — canonical project/domain authority.
2. This Stage-3 v2.2 closure prompt — supersedes Stage-3 v2.1 and the R1 Final Decisions document only where explicitly different below.
3. Current AeroTech provider source/contracts.
4. IATA/ATPCO/public Amadeus/Sabre benchmarks.
5. Historical Pack/donor material is evidence only and cannot override the Master.

Master v2.0 itself is **not** version-bumped by this closure pass.

---

# 1. BLOCKER A — FlightFlow cross-operation resource concurrency is still unsafe

The Confirm handler was corrected to:

- resolve Flight IDs;
- acquire deterministic Flight-ID locks;
- reload authoritative Flight state inside the lock;
- evaluate confirmability inside the lock;
- mutate once.

However the current FlightFlow source still contains the old pattern in:

`ReleaseHeldFlightSeatsCommandHandler.cs`

and

`ExpireHeldFlightSeatsCommandHandler.cs`

Both currently load Flight aggregates **before** acquiring the lock.

They also currently derive lock keys with:

```csharp
flightsToRelease.Select(f => f.ToString())
```

or the equivalent expiry expression.

`Flight` has no source-backed `ToString()` override establishing this as the same resource key used by Confirm. Confirm currently uses Flight ID strings.

This means the Stage-3 provider guarantee has only been proven for Confirm-vs-Confirm, not for:

- Confirm vs Release;
- Confirm vs automatic Expire;
- Release vs Expire on the same capacity.

That is not sufficient for truthful PSS reservation state.

## Required correction

For every FlightFlow mutation that can race with Stage-3 Confirm on the same held capacity — at minimum:

- Confirm;
- Release Held;
- Expire Held;

use the **same deterministic lock identity for the same Flight aggregate**.

Follow existing FlightFlow infrastructure; do not invent a new locking architecture.

Required pattern:

1. Resolve only the affected Flight IDs before locking.
2. Acquire `ExecuteWithLocksAsync` using those Flight IDs as the same key format used by Confirm.
3. The locker already sorts distinct keys; preserve deterministic multi-flight acquisition.
4. Reload authoritative Flight aggregates **inside** the acquired lock.
5. Re-evaluate current hold/status/expiry from the reloaded state.
6. Only then mutate.
7. Commit/persist under the same protected operation according to existing UoW conventions.

A stale aggregate loaded before the lock must never authorize or execute the mutation after the lock.

### Required concurrency tests

Add focused provider tests that fail on the current implementation:

#### FF-X01 — Confirm races with Expire before provider expiry

- Hold is still valid.
- Confirm obtains the resource lock first.
- Confirm commits.
- Expire subsequently reloads inside the same resource lock.
- Expire must observe `Confirmed` and perform zero expiry effect.
- capacity accounting changes exactly once.

#### FF-X02 — Expire races with Confirm at/after provider expiry

- `ExpiresAt <= provider now`.
- Expire obtains the resource lock first, or Confirm reloads after the boundary.
- final truth must be Expired/no Confirm effect.
- Confirm must return deterministic non-success, never 204 success for an expired Held record.

#### FF-X03 — Confirm races with Release

If Release wins:
- Confirm reloads and observes Released;
- Confirm returns deterministic no-effect non-success.

If Confirm wins:
- Release reloads current state and must not decrement Held or create a second capacity effect.

The final counters must correspond to one coherent resource transition, never two stale transitions.

#### FF-X04 — multi-flight Hold uses the same sorted Flight-ID locks for Confirm/Release/Expire

Prove all affected flight IDs are locked using the same key convention.

---

# 2. BLOCKER B — Release-by-HoldId must not report false success for unknown/non-releasable resource state

Current `ReleaseHeldFlightSeats` behavior can reach a no-op and still return HTTP success because the domain release method only acts on Held members.

This is unsafe for a PSS resource contract: a transport-level 2xx must not cause Ordering to record `Released` when the provider resource is actually Confirmed, Expired, Cancelled, Mixed, or absent.

## Required FlightFlow release contract

For a HoldId:

### A. fully Held
Release the complete current target and return definitive success.

### B. fully already Released
Return deterministic same-effect success with zero additional capacity mutation.

This is required so an ambiguous Release response can be replayed safely.

### C. unknown HoldId
Return deterministic structured non-success distinguishable from success.

Do not return the same 2xx used for A/B.

### D. Confirmed / Expired / Cancelled
Return deterministic no-effect non-success carrying the actual state category when the provider can prove it.

### E. mixed/inconsistent target
Return deterministic no-effect non-success. Do not partially release.

Do not invent a client idempotency key for Release merely to achieve replay safety.

## Ordering ACL requirement

Ordering must only record `FulfillmentReservationStatus.Released` when FlightFlow has returned the provider contract's definitive release success.

A generic 2xx not defined by the endpoint contract must not be silently interpreted as authoritative release truth.

For state-bearing non-success:
- never mark Released;
- preserve/apply only provider truth that is actually exposed by the provider contract and already representable by the current domain;
- do not fabricate per-unit state when the provider did not return it.

If representing an exact terminal provider state would require a new generic domain concept not already present in Master v2.0, stop and report `BLOCKED_SOURCE` rather than inventing it.

## Required tests

- unknown HoldId does not produce release success;
- already Released replay is same-effect success;
- Expired/Confirmed/Cancelled do not produce release success;
- mixed target is not partially released;
- lost first release response + exact replay performs at most one logical release effect.

This is a narrow source-proven correction needed before Stage 4; do not redesign Stage 2.

---

# 3. BLOCKER C — preserve known Mixed truth instead of collapsing it to Unknown

Master v2.0 explicitly requires:

> Partial/mixed provider truth is retained. It is never collapsed to a boolean.

Current FlightFlow Confirm code `1179` proves one authoritative fact:

```text
the HoldId exists but its members have inconsistent statuses
```

The provider does **not** expose the exact per-seat composition in the current error contract.

Current Ordering maps `1179` to:

```text
reservation = Unknown
all units = Unknown
```

This loses the known reservation-level fact that the target is Mixed.

## Required minimal truthful mapping

For coded FlightFlow `1179`:

```text
Confirm task = Failed
FulfillmentReservation.Status = Mixed
ReservationUnit.Status = Unknown for each unit whose exact state was not returned
OrderStatus = ReservationUnconfirmed
```

Preserve the raw provider response/evidence.

Do not guess which unit is Confirmed/Released/Expired/Cancelled.

Do not create a new provider-specific domain status enum.

Use the smallest extension of the existing provider-neutral confirmation outcome plumbing needed to carry:

```text
authoritatively Mixed at reservation/root level
but exact unit states unknown
```

Do **not** misuse a status that would imply the Confirm call itself partially mutated the provider if the source only proves pre-existing mixed state.

Required tests:

- coded 1179 -> root Mixed;
- every unit remains Unknown unless exact per-unit state was supplied;
- task Failed;
- Order ReservationUnconfirmed;
- no new Reserve is allowed merely because the state is Mixed/Unknown;
- no automatic destructive compensation is triggered.

Coded `1180` remains:

```text
reservation Unknown
units Unknown
task Failed
```

because provider absence does not prove a terminal resource disposition.

---

# 4. HARDENING — only the documented FlightFlow success response is authoritative

Current Ordering `ConfirmHoldAsync` treats every `2xx` as Confirmed.

The current FlightFlow endpoint contract is:

```text
POST Service/v1/Flights/Seat-Holds/{id}/Confirmations
success = 204 NoContent
```

For this Stage, treat only the documented definitive provider success contract as Confirmed.

Unexpected undocumented `2xx` must not silently become Confirmed. Treat it as provider contract violation / unresolved outcome according to existing failure vocabulary.

Apply the same principle to Release: only the provider contract's documented definitive success response(s) may produce Released truth.

This is a no-guess hardening rule, not a new business feature.

Required tests:
- documented 204 Confirm => Confirmed;
- unexpected 200/201/202 Confirm => not authoritative Confirmed;
- documented release success => Released;
- unexpected undocumented 2xx release => not authoritative Released.

If current FlightFlow middleware intentionally normalizes more than one 2xx as the official contract, prove that from source and document the exact allowed set instead of guessing.

---

# 5. GOVERNANCE BLOCKER — remove repository authority drift before Stage 4

Current repository instructions contradict the canonical authority.

`CLAUDE.md` currently states that:

```text
ORDERING-IMPLEMENTATION-PACK-v4.6-FINAL.md
is the authority for domain shape and behavior
```

and the Pack itself still begins with:

```text
FINAL DOMAIN & BEHAVIOR AUTHORITY
```

That is stale.

Canonical project authority is now:

```text
AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md
```

Stage-specific approved documents derive from the Master.

## Required correction

Update `CLAUDE.md` so future Coding Agent sessions cannot accidentally revert to Pack v4.6.

It must state clearly:

1. Master v2.0 is the full-horizon domain/roadmap authority.
2. Current approved Stage PRD/prompt governs current materialization where consistent with the Master.
3. Pack v4.6, Stage-2 specs, donor code and previous Stage prompts are historical evidence/traceability only.
4. No historical file can override Master v2.0.
5. Payment/JetPay is not an intrinsic Order/Reservation prerequisite; Stage P is additive according to Master v2.0.
6. Stage 4 is Issue, not Payment.

For `docs/ORDERING-IMPLEMENTATION-PACK-v4.6-FINAL.md`, choose one unambiguous option:
- move it under a `superseded/` historical location; or
- add a top-of-file superseded banner that explicitly points to Master v2.0 and says it cannot override it.

Do not delete useful historical traceability.

Also check the active `docs/` root for any other file that still claims to be the project-wide final domain authority. Fix labels/placement only; do not rewrite historical content unnecessarily.

---

# 6. Reconfirm the already-correct Stage-3 behavior — do not regress it

Keep all of the following exactly:

- Payment-neutral Stage 3.
- no JetPay / PaymentIntent / PaymentSession / OrderPaymentCoverage.
- no ETKT / EMD / DocumentStock yet.
- `TicketingTimeLimit` / `LastTicketingDate` not added to FlightFlow.
- FlightFlow checks provider `ExpiresAt`, not commercial LTD.
- Ordering checks LTD before a **new** Confirm dispatch.
- Unknown Confirm recovery ignores later LTD/local ExpiresAt/stale AirPrice and replays only the exact persisted original Confirm request when safe replay is verified.
- no request reconstruction.
- no generic provider idempotency key for Confirm.
- ProviderInteraction idempotency remains nullable.
- plain/unclassified 404 remains ambiguous.
- coded 1180 remains authoritative HoldId absence but not a fabricated terminal resource state.
- all Held => `OrderStatus.ReservationUnconfirmed`.
- all required Confirmed/ImmediateConfirm => `OrderStatus.Confirmed`.
- deadline expiry of the commercial Order does not falsify Confirmed/Unknown provider truth.
- AirPrice revalidation uses persisted `OrderFarePricingUnit` / `OrderFareComponent` topology.
- `CabinClassId` remains source-preserved, nullable for old rows, never inferred.
- one Backoffice confirmations endpoint; no speculative Internal mirror.

---

# 7. PSS benchmark guardrail for this closure

Do not imitate proprietary internal Amadeus/Sabre aggregates.

The behavior to preserve is the public PSS/airline semantics:

- reservation/booking state is explicit and distinct from ticket/document issuance;
- PNR/booking resource truth must not be inferred from transport success alone;
- confirmed/waitlisted/cancelled/other reservation states are meaningful state, not one boolean;
- reservation restrictions, provider hold expiry and ticketing deadlines are distinct clocks;
- ticket issuance belongs to the next stage and requires confirmed/committed capacity and an open ticketing deadline;
- Order/PNR status and document issuance remain separable during the industry's hybrid PNR/Order transition.

Sources for the product review include:
- IATA ONE Order / Offers & Orders public material;
- IATA Reservations Handbook / Shop-Order-Pay scope;
- ATPCO Category 5 Advance Reservations and Ticketing;
- Amadeus Altéa Reservation / Reservation & Ticketing public material;
- Sabre PNR APIs / SabreMosaic public material.

Do not add a field merely because a vendor has it. Use Master v2.0 ownership.

---

# 8. Required final verification

Run and report:

## Ordering
- build;
- Stage-3 confirmation tests;
- recovery tests;
- multi-target tests;
- deadline tests;
- release tests;
- CabinClass preservation tests;
- AirFareReservationValidator tests;
- all Stage-3 sabotage/negative tests.

## FlightFlow
- build;
- Confirm unit tests;
- expiry-selection tests;
- new Confirm-vs-Expire tests;
- new Confirm-vs-Release tests;
- release same-effect/negative tests;
- multi-flight lock tests.

If infrastructure-dependent suites are blocked, report exactly:

```text
ENVIRONMENT_BLOCKED: <suite> — <reason>
```

Do not report an unexecuted test as passed.

GitHub currently exposes no CI/status checks for the reviewed HEADs, so local/agent test evidence is required in the completion report.

---

# 9. Final report format

Return:

```text
PROJECT AUTHORITY
Master v2.0

STAGE AUTHORITY
Stage 3 v2.2 FINAL CLOSURE

ORDERING
before HEAD:
after HEAD:

FLIGHTFLOW
before HEAD:
after HEAD:

A — CROSS-OPERATION LOCKING
files changed:
same Flight-ID lock convention:
reload under lock:
Confirm vs Expire:
Confirm vs Release:

B — RELEASE CONTRACT
unknown HoldId:
already Released replay:
Confirmed:
Expired:
Cancelled:
Mixed:
safe replay:

C — MIXED TRUTH
1179 root:
unit states:
task:
OrderStatus:

D — SUCCESS CONTRACT
Confirm allowed success HTTP:
Release allowed success HTTP:
unexpected 2xx behavior:

E — AUTHORITY CLEANUP
CLAUDE.md:
Pack v4.6 disposition:
other stale authority labels:

REGRESSION CHECK
Payment/JetPay added: NO
ETKT/EMD/DocumentStock added: NO
TicketingTimeLimit added to FlightFlow: NO
AirPrice topology preserved: YES
CabinClassId inference: NO

TESTS
Ordering:
FlightFlow:
environment-blocked:
sabotage/negative:

FINAL
STAGE3_READY_FOR_FINAL_REVIEW
```

Do not start Stage 4.

If any required source fact is still unverifiable, finish with:

```text
BLOCKED_SOURCE: <exact unresolved fact>
```
