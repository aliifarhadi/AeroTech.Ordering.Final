# CODING AGENT PROMPT — Ordering Stage 3 v2.1 — FlightFlow Correction

## Authority

Current project authority remains:

```text
AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md
```

Stage authority:

```text
CODING-AGENT-PROMPT-Ordering-Stage3-Confirm-Reserved-Capacity-v2.1-FINAL.md
```

This prompt is a clarification/correction to Stage 3 v2.1.  
It does **not** create Master v2.1.

---

# 1. Current baselines

Ordering:

```text
repo: aliifarhadi/AeroTech.Ordering.Final
branch: k8s-stg
HEAD: 85f48bb652d96ce155c96ee07045223656db1ecb
```

Do not modify Ordering yet.

FlightFlow:

```text
repo: aliifarhadi/Aerotech.FlightFlow
base HEAD: 7d4e9ce3
working branch: stage3-confirm-hold
```

The two commits after the previously reviewed `6e936dbf` concern identity/JWT only and do not alter Hold/Confirm/expiry behavior.

You already created `stage3-confirm-hold` from `7d4e9ce3`.

Continue from that branch.

Do not reset it.

---

# 2. IMPORTANT CORRECTION — DO NOT ADD TicketingTimeLimit TO FLIGHTFLOW

Do **not** add any of the following to FlightFlow:

```text
TicketingTimeLimit
LastTicketingDate
AirPrice validation TimeLimit
Ordering commercial deadline
```

Do not add them to:

```text
Confirm command
REST request
FlightSeatHold
Flight
FlightCapacity
database schema
events
DTOs
```

Reason:

```text
Ordering LastTicketingDate
    = commercial/order eligibility deadline

FlightFlow ExpiresAt
    = capacity-resource deadline
```

They are independent clocks.

Ordering owns the LTD check before dispatching Confirm.

FlightFlow owns only the validity/state of its resource.

A Confirm request reaching FlightFlow must therefore be decided only from FlightFlow-owned truth.

---

# 3. FlightFlow Confirm semantic contract — FINAL

Current endpoint may remain:

```text
POST Service/v1/Flights/Seat-Holds/{holdId}/Confirmations
```

No new request body and no idempotency key are required.

The corrected semantic contract must be:

## Case A — Hold exists and is fully Held

If all applicable seats for this HoldId are:

```text
Status == Held
AND
ExpiresAt > now
```

then:

```text
confirm exactly once
return success
```

Expected HTTP may remain:

```text
204 No Content
```

---

## Case B — Hold is already fully Confirmed

This is an idempotent successful replay.

```text
no capacity mutation
no duplicate confirmation effect
return success
```

Expected:

```text
204 No Content
```

This behavior is essential for safe recovery after an ambiguous first call.

---

## Case C — unknown HoldId

Must **not** return success.

Return the deterministic existing FlightFlow-style not-found/business failure.

Do not pretend confirmation occurred.

---

## Case D — Released / Expired / Cancelled / otherwise non-confirmable Hold

Must **not** return success.

No mutation.

Return a deterministic non-success according to existing FlightFlow exception/API conventions.

Do not convert a terminal resource back to Confirmed.

---

## Case E — Held but ExpiresAt <= now

Must not confirm.

The result must be deterministic non-success.

Do not rely only on the expiry background job.

The Confirm operation itself must protect this invariant because expiry processing and Confirm can race.

---

## Case F — inconsistent/mixed state for the same HoldId

Example:

```text
some seats Confirmed
some seats Held
```

or:

```text
Held + terminal state
```

Do not silently treat this as successful full confirmation.

Do not partially mutate unless an already-existing FlightFlow contract explicitly defines such behavior.

Fail deterministically and preserve the current truth.

Stage 3 requires an atomic operation-level confirmation semantic.

---

# 4. Concurrency bug — MUST FIX

You found a real concurrency defect:

Current Confirm flow effectively does:

```text
load flights
↓
derive lock keys
↓
take lock
↓
mutate previously loaded aggregate instances
```

Therefore two concurrent calls can both load:

```text
Held
```

before either takes the lock.

The second caller can later operate on stale state and apply capacity effects twice.

This violates Stage 3's required atomic semantics.

## Required invariant

For the same HoldId:

```text
two concurrent Confirm requests
```

must result in:

```text
one logical confirmation effect
```

Never:

```text
Confirmed capacity += twice
Held capacity -= twice
duplicate effective confirmation
```

## Required implementation behavior

The authoritative state used for the mutation must be loaded/reloaded **after the relevant lock has been acquired**.

A valid pattern is:

```text
1. discover only enough identity to determine deterministic lock keys
2. acquire deterministic locks for all affected Flight aggregates/resources
3. reload authoritative aggregate state inside the lock
4. re-evaluate:
   - HoldId existence
   - statuses
   - ExpiresAt
5. perform mutation only if still Held and valid
6. persist/update query model
7. release lock
```

Important:

- Do not use stale pre-lock aggregate instances as mutation authority.
- Calls for the same HoldId must contend on the same deterministic resource keys.
- If a HoldId spans multiple Flight aggregates, acquire all relevant keys deterministically to avoid deadlocks.
- Do not use a constant/global shared lock merely to hide this problem.
- Do not invent a second reservation store or distributed transaction.

Use FlightFlow's existing locking/repository patterns wherever possible.

---

# 5. Safe replay conclusion after correction

After Sections 3 and 4 are implemented and proven by tests, FlightFlow Confirm capability becomes:

```text
AcceptsIdempotencyIdentity = VerifiedNo

DuplicateMutationSemantics = SameEffect

SupportsAuthoritativeReadBack = VerifiedNo

SupportsSafeReplayAfterAmbiguousOutcome = VerifiedYes

ResultGranularity = AtomicOperation
```

Why replay becomes safe:

```text
first request committed
    ↓
second call sees fully Confirmed
    ↓
204 success, no mutation
```

or:

```text
first request never committed
    ↓
resource is still valid Held
    ↓
second call performs the original Confirm
    ↓
204 success
```

or:

```text
first request never committed
resource is no longer confirmable
    ↓
second call returns deterministic non-success
```

Therefore the replay itself establishes a conclusive outcome.

Do not introduce an idempotency-key parameter merely to achieve this.

---

# 6. Expiry bug — MUST FIX

Current repository predicate is wrong:

```csharp
seatHold.ExpiresAt > clock.GetDateTime()
&& seatHold.Status == FlightSeatHoldStatus.Held
```

It must select expired Holds:

```text
ExpiresAt <= now
AND
Status == Held
```

Correct:

```csharp
seatHold.ExpiresAt <= clock.GetDateTime()
```

Fix the repository query.

Also keep the independent Confirm-time expiry check from Section 3.

These protect different races:

```text
background expiry
+
synchronous Confirm eligibility
```

Both are required.

---

# 7. Do not broaden FlightFlow scope

Do not implement:

```text
TicketingTimeLimit
LastTicketingDate
Payment
JetPay
AirPrice calls
PNR
Ordering status
ETKT
EMD
new generic idempotency framework
Confirm read-back API
manual reconciliation API
```

This FlightFlow change is limited to:

```text
correct Confirm semantics
atomic concurrency
safe same-effect replay
correct expiry selection
```

---

# 8. Required FlightFlow tests

Add focused tests following the existing FlightFlow test style.

At minimum prove:

## FF-C01 — normal Confirm

```text
Held + not expired
→ Confirmed
→ Held capacity decremented once
→ Confirmed capacity incremented once
→ success
```

## FF-C02 — duplicate Confirm

```text
first call:
Held → Confirmed

second call:
Confirmed → Confirmed
```

Second call:

```text
success
zero additional capacity mutation
```

## FF-C03 — unknown HoldId

```text
unknown HoldId
→ deterministic non-success
→ zero mutation
```

## FF-C04 — Released Hold

```text
Released
→ deterministic non-success
→ remains Released
```

## FF-C05 — Expired Hold

```text
Expired
→ deterministic non-success
→ remains Expired
```

## FF-C06 — logically expired Held

```text
Status = Held
ExpiresAt <= now
```

Confirm must:

```text
not mutate
not return success
```

even if the expiry job has not run yet.

## FF-C07 — concurrent duplicate Confirm

Execute two concurrent confirmation operations against the same HoldId.

Prove final capacity is exactly equivalent to one Confirm:

```text
initial:
Held = N
Confirmed = M

final:
Held = N - X
Confirmed = M + X
```

not:

```text
Held = N - 2X
Confirmed = M + 2X
```

Both callers may receive logical success if one performs the mutation and the other observes already-Confirmed state.

This test must specifically fail against the old load-before-lock implementation.

## FF-C08 — expiry selector

Given:

```text
Held A: ExpiresAt < now
Held B: ExpiresAt == now
Held C: ExpiresAt > now
Confirmed D: ExpiresAt < now
```

expiry selection must include:

```text
A
B
```

and exclude:

```text
C
D
```

## FF-C09 — mixed/non-confirmable batch

If source persistence can represent a HoldId with inconsistent status members, prove it is rejected without partial mutation.

If the domain makes this state structurally impossible, document that proof instead of inventing a synthetic production path.

---

# 9. Test environment limitation

You already established:

```text
FlightFlow build = PASS
```

The full acceptance test suite currently attempts to start SQL Server using Testcontainers/Docker and hung while obtaining the container image.

Do not spend another uncontrolled period waiting on Docker.

Proceed with implementation.

Run:

```text
dotnet build
```

and all relevant tests that can execute reliably in the current environment.

For tests requiring the SQL Server Testcontainer:

- run them if the image/environment becomes available;
- otherwise report them explicitly as `ENVIRONMENT_BLOCKED`;
- do not report them as passed;
- do not weaken/delete those tests merely to obtain green output.

Do not modify unrelated Docker containers such as:

```text
dotair-rabbit
flightflow-redis
```

---

# 10. Required pre-Ordering checkpoint

After FlightFlow changes, stop before modifying Ordering and report:

```text
FLIGHTFLOW HEAD
changed files
exact semantic changes

CONFIRM CAPABILITY MATRIX
AcceptsIdempotencyIdentity
DuplicateMutationSemantics
SupportsAuthoritativeReadBack
SupportsSafeReplayAfterAmbiguousOutcome
ResultGranularity
ExpiryAuthority

CONCURRENCY
how stale pre-lock state was eliminated
which deterministic lock keys are used
proof double capacity mutation is impossible

EXPIRY
repository predicate correction
Confirm-time expiry guard

TESTS
build result
tests passed
tests environment-blocked
no false green claims
```

Also explicitly state:

```text
TicketingTimeLimit was NOT added to FlightFlow.
```

Then stop.

Do **not** begin Ordering Stage 3 until this FlightFlow checkpoint is reviewed and approved.
