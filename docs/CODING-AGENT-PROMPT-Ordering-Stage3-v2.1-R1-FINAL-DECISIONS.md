# CODING AGENT PROMPT — Ordering Stage 3 v2.1 — R1 FINAL DECISIONS

Implement the final Stage-3 R1 closure corrections against Ordering `k8s-stg@c88918455037b5029dbfb0b13422a3ef41f20a9a`.

This prompt supersedes the previous R1 prompt wherever different.

Do not start Stage 4.

## Final decisions

### 1. Separate new Confirm from Unknown recovery

A new Confirm must enforce current:

- LastTicketingDate
- provider hold expiry
- AirPrice validation freshness

An existing Unknown Confirm task represents an already-dispatched logical effect.

Recovery therefore:

- does not check current LTD;
- does not block on local historical ExpiresAt;
- does not revalidate with AirPrice;
- does not create a new Confirm task;
- replays only the exact persisted original ConfirmHold request when safe replay is verified.

### 2. Exact persisted request is mandatory

For recovery, remove any fallback equivalent to:

`OriginalRequest(...) ?? ConfirmRequestFor(...)`

The original persisted ConfirmHold request must exist.

If missing:

- zero provider calls;
- no reconstructed request;
- no new task;
- throw internal invariant `2773 ConfirmationRecoveryRequestIsMissing`, HTTP 500.

Do not reuse 2763 or 2767.

### 3. Generic rejection during recovery does not close the logical effect

For an Unknown Confirm recovery, a replay response such as:

- 401
- 403
- generic 400
- generic permanent 4xx

that carries no authoritative reservation/resource-state evidence does **not** resolve what happened to the original Confirm.

Persist the replay interaction, but keep:

- task = Unknown/resumable;
- reservation = Unknown;
- units = Unknown.

A later exact replay remains allowed.

For a **new Confirm**, the same generic definitive rejection may:

- task = Failed;
- reservation/units remain Held.

### 4. State-bearing rejection

Keep:

- 1176 → Expired
- 1177 → Released
- 1178 → Cancelled

Add:

- 1179 → provider reports mixed/inconsistent hold state
- 1180 → provider authoritatively reports HoldId absent

For 1179:

- task = Failed;
- reservation = Unknown;
- units = Unknown;
- Order = ReservationUnconfirmed.

For coded 1180:

- task = Failed;
- reservation = Unknown;
- units = Unknown;
- Order = ReservationUnconfirmed.

Do not infer Expired, Released, Cancelled or Rejected from absence.

### 5. Plain 404 is NOT authoritative unknown-HoldId evidence

Do not treat every HTTP 404 as missing HoldId.

FlightFlow must return a structured error for the domain case:

`Confirm requested for unknown HoldId`

Use stable FlightFlow error code:

`1180 = SeatHoldNotFoundForConfirmation`

Only coded 1180 is authoritative.

An empty/unclassified 404 may represent a wrong route/version/base URL and therefore maps to:

- ProviderOperationOutcome = Unknown
- no authoritative observed reservation status

During recovery it leaves the task Unknown/resumable.

Add provider and Ordering ACL tests proving this distinction.

### 6. Minimal outcome mapping

Use the existing provider-neutral outcome model; do not invent a new provider status enum.

Recommended mapping:

- 1176 → Rejected + ObservedStatus.Expired
- 1177 → Rejected + ObservedStatus.Released
- 1178 → Rejected + ObservedStatus.Cancelled
- 1179 → Rejected + ObservedStatus.Unknown
- 1180 → Rejected + ObservedStatus.Unknown
- plain 404 → Unknown + no authoritative ObservedStatus
- generic permanent rejection → Rejected + no ObservedStatus

Do not globally say `Rejected => task Failed`.

Task resolution depends on whether the logical effect was actually resolved.

### 7. Initial command planning remains fail-fast

Before the first FlightFlow Confirm mutation, validate/plan all targets.

If any target fails initial planning:

- zero FlightFlow Confirm mutations for all targets;
- reject the command as today.

This covers 2770, 2771, 2772 and AirPrice rejection/unsupported cases.

Add a multi-target test proving an earlier target is not dispatched when another target fails planning.

### 8. JIT race after execution starts

Every target still requires an immediate pre-dispatch re-check.

However, once any previous Confirm provider mutation has been dispatched, a later target failing that JIT check must not make the whole API call throw and hide previous provider effects.

For that later target:

- zero FlightFlow Confirm calls;
- do not create a Confirm task if no provider mutation is dispatched;
- return a per-reservation failure in `ConfirmResult`;
- preserve its existing reservation state;
- preserve all earlier target outcomes;
- continue with remaining independent targets;
- summarize Order truthfully.

Suggested result mapping:

- hold lapsed → HoldExpired
- validation stale → ValidationFailed
- LTD crossed → BusinessRejected with explicit error text

If no provider Confirm has yet been dispatched, the command may still fail normally.

### 9. Accepted Stage-3 limitations

#### 1179 / coded 1180

Accepted fail-closed state:

- task Failed
- reservation Unknown
- units Unknown
- Order ReservationUnconfirmed

Do not invent a terminal resource status merely to permit a new Reserve.

No manual reconciliation command or poller is added in R1.

A plain 404 does not enter this state.

#### Recovery after LTD

Accepted:

`Order = Expired`

may coexist with:

`FulfillmentReservation = Confirmed`

when replay resolves the logical effect originally dispatched while eligible.

Do not cancel confirmed FlightFlow capacity at LTD in Stage 3.

### 10. Authority files

Active authority order:

1. `AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`
2. `CODING-AGENT-PROMPT-Ordering-Stage3-Confirm-Reserved-Capacity-v2.1-FINAL.md`
3. Stage-3 FlightFlow Correction prompt
4. this R1 Final Decisions prompt for conflicting R1 details

Move Stage3 v2.0 to:

`docs/superseded/`

Do not leave it in the active docs root.

Canonical authority docs are explicitly allowed in `docs/` for this Stage despite the generic `CLAUDE.md` artifact restriction.

Do not create completion reports or miscellaneous architecture analysis files in `docs/`.

### 11. FlightFlow source visibility

Operational FlightFlow source of truth is reported as:

- GitLab branch `stage3-confirm-hold@bb72fceb`
- merged `k8s-stg@ff2410a5`

Before Final Review, synchronize the GitHub mirror `aliifarhadi/Aerotech.FlightFlow` so those exact source changes are inspectable from GitHub.

Do not rewrite GitLab history.

If mirror synchronization is technically impossible, report the blocker and provide the exact GitLab SHA plus complete diff.

### 12. Required tests

Add/adjust at minimum:

1. Unknown replay after LTD dispatches exact persisted request and can resolve Confirmed.
2. Unknown replay after local ExpiresAt can resolve already-Confirmed truth.
3. Unknown replay after local ExpiresAt can resolve Expired.
4. stale validation does not trigger AirPrice during recovery.
5. missing persisted request fails with 2773 and zero provider calls.
6. generic 4xx replay keeps task/reservation Unknown and resumable.
7. new Confirm generic rejection produces Failed task + Held reservation.
8. 1179 produces Failed task + Unknown reservation/units.
9. FlightFlow unknown HoldId returns coded 1180.
10. coded 1180 produces Failed task + Unknown reservation/units.
11. empty/plain 404 remains Unknown, not authoritative HoldId absence.
12. initial multi-target planning failure produces zero Confirm mutations.
13. late JIT failure after an earlier dispatch returns partial per-target result rather than throwing away prior outcomes.
14. no recovery poller/manual reconciliation command exists.

### 13. Sabotage checks

Prove tests fail when:

- recovery is blocked by LTD;
- recovery is blocked by local ExpiresAt;
- recovery calls AirPrice;
- original request is reconstructed;
- generic replay 4xx closes the logical task;
- plain 404 is treated as unknown HoldId;
- 1179 or coded 1180 leaves reservation Held;
- an earlier target dispatches before initial all-target planning succeeds;
- a late JIT failure throws after an earlier provider mutation.

Restore production code afterward.

### 14. Keep removed

Do not restore:

- 2763
- 2767
- `FulfillmentReservation.IsSettled`

Do not reuse 2763 or 2767.

### 15. Final report

Return:

- Ordering HEAD before/after;
- FlightFlow GitLab branch + merged SHA;
- GitHub mirror SHA/ref;
- changed files;
- authority locations/precedence;
- v2.0 superseded location;
- new Confirm vs recovery behavior;
- generic replay rejection behavior;
- 1176/1177/1178/1179/1180 mappings;
- plain 404 behavior;
- exact persisted request proof;
- planning fail-fast proof;
- JIT partial-result proof;
- Ordering tests;
- FlightFlow tests;
- sabotage totals;
- environment-blocked tests;
- confirmation that no Payment, JetPay, ETKT, EMD or DocumentStock was added.

Finish with:

`STAGE3_READY_FOR_FINAL_REVIEW`

or

`BLOCKED_SOURCE: <exact reason>`

Do not start Stage 4.
