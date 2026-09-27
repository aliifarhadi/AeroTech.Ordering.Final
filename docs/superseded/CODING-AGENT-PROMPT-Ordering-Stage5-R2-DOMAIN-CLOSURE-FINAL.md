# CODING AGENT PROMPT — Ordering Stage 5 R2 DOMAIN CLOSURE FINAL
## Implement canonical OrderChange reason history + scope-correct Issue after servicing

### Baseline

Ordering:

```text
aliifarhadi/AeroTech.Ordering.Final
branch: k8s-stg
reviewed HEAD: 94a84a0a435b6394bffbd2713d709992be37f195
```

FlightFlow:

```text
aliifarhadi/Aerotech.FlightFlow
branch: k8s-stg
verified HEAD: d2180b2e4c07789abde75e712e870ece8d2c776b
```

Do not change FlightFlow.

Stage 1–4 remain CLOSED.
Stage 5 is IN FINAL DOMAIN CLOSURE.

---

# 1. Authority and objective

Read first:

1. `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`
2. `docs/CODING-AGENT-PROMPT-Ordering-Stage5-Cancel-Void-v1.0-FINAL.md`
3. `docs/CODING-AGENT-PROMPT-Ordering-Stage5-R1-FINAL-CLOSURE.md` if present
4. this R2 prompt

Then merge the R2 canonical decisions into **Master v2.0 in place**.

Do not create Master v2.1/v2.2.

The Master remains the single authority after this work.

This is not a narrow `if` patch. Correct the domain semantics described below.

Do not start Stage 6.

---

# 2. Master v2.0 corrections

Patch the existing Master in place.

## 2.1 OrderChange

Add:

```text
ReasonText : string?
```

Meaning:

- human-readable business reason detail;
- max 500;
- trimmed;
- whitespace-only null;
- never used as eligibility authority.

Keep:

```text
ReasonCode
SourceSystem
SourceReference
IsInvoluntary
WaiverCode
```

as separate facts.

## 2.2 FulfillmentReservation validation evidence

Add canonical owned evidence:

```text
ReservationValidationEvidence
{
    CommercialVersion : int
    ValidUntil : DateTimeOffset
    ValidatedAt : DateTimeOffset
    ValidatedOrderServiceIds : IReadOnlyCollection<long>
}
```

Update Master semantics so the old bare `ReservationValidationTimeLimit` is not sufficient authority by itself.

It may remain as compatibility projection/storage during migration, but business rules must use full evidence.

## 2.3 Issue eligibility

Change the Stage-4 wording explicitly:

```text
required issue-scope ReservationUnit(s) must be Confirmed
```

NOT:

```text
the whole FulfillmentReservation root must be Confirmed
```

State clearly:

```text
FulfillmentReservation.Status is summary only.
Mixed may be issueable for a confirmed target subset.
```

## 2.4 Pricing integrity after servicing

Add the derived `FarePricingAtom` rule from this prompt.

Do not add a persisted FarePricingAtom entity.

---

# 3. OrderChange ReasonText implementation

Current API already exposes:

```text
CancelOrderRequest.ReasonDetail
BackofficeCancelOrderCommand.ReasonDetail
```

but the handler/service drops it.

Required end-to-end path:

```text
REST ReasonDetail
-> command
-> handler
-> ICancelOrderService
-> Order.Cancel
-> OrderChange.ReasonText
-> command DB
-> query DB
-> Backoffice order-detail change history
```

Normalization:

```text
null -> null
whitespace -> null
otherwise Trim()
max 500
```

Use the existing API validator max 500.

Do not store ReasonDetail in Remark.
Do not put it in SourceReference.
Do not concatenate it into ReasonCode.

### Persistence

Add narrow command/query migrations.

Current historical OrderChanges:

```text
ReasonText = null
```

No fabricated backfill.

---

# 4. Derived FarePricingAtom — exact algorithm

Implement as a domain/application derived rule using existing accepted fare topology.

No new aggregate/entity/table.

## 4.1 OneWay

For each FareComponent:

```text
atom = all Air OrderService IDs in FareComponent.CoveredOrderServiceIds
```

A FareComponent covering multiple segments is one indivisible atom.

## 4.2 RoundTrip / OpenJaw / CircleTrip

```text
atom = union of all Air service IDs of all FareComponents in the PricingUnit
```

## 4.3 Unspecified / Other

Conservatively:

```text
atom = entire PricingUnit Air-service coverage
```

Do not infer independence.

## 4.4 Fracture test

For every atom intersecting the intended Issue scope:

```text
active = atom Air services whose CommercialStatus == Active
ended  = atom Air services whose CommercialStatus != Active
```

If:

```text
active.Any && ended.Any
```

throw a dedicated business error:

```text
REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION
```

Use next free Ordering error code in 2000–2999.

Recommended HTTP:

```text
409 Conflict
```

Message must say current accepted pricing topology no longer authorizes issuance of the remaining partial fare scope and a source-authoritative repricing/servicing decision is required.

No AirPrice call after this failure.
No stock lock/allocation.
No Issue FulfillmentTask.
No document.

---

# 5. IssuancePlanner — scope correction

Current defect:

```csharp
if (coverage.Reservation.Status != Confirmed) block;
if (coverage.UnitStatus != Confirmed) block;
```

Remove root-status requirement.

Required eligibility per service:

```text
latest coverage exists
latest covering ReservationUnit.Status == Confirmed
```

The reservation root may be:

```text
Confirmed
Mixed
```

Do not allow target units in Held/Unknown/Cancelled/Released/Expired/Rejected/Waitlisted.

Do not mutate root state.

---

# 6. IssuanceScope — preserve target granularity

Replace the coarse:

```text
Services + Reservations
```

with semantics equivalent to:

```text
IssuanceScope
{
    Services
    ReservationScopes
}

ReservationIssueScope
{
    Reservation
    ReservationUnitIds
    OrderServiceIds
}
```

Names may follow repo style.

Every issue-scope service requiring reservation must map to exactly one latest target unit.

Do not infer by root state.

---

# 7. ReservationValidationEvidence implementation

Canonical evidence owned by `FulfillmentReservation`.

It must be persisted atomically as one logical snapshot.

Conceptual shape:

```text
CommercialVersion
ValidUntil
ValidatedAt
ValidatedOrderServiceIds
```

Persistence may be:

- owned value object + primitive collection; or
- flattened root fields + primitive collection

according to current EF conventions.

Do not create a new aggregate root.

## 7.1 Invariants

When evidence exists:

```text
CommercialVersion > 0
ValidatedOrderServiceIds non-empty
ValidatedOrderServiceIds distinct
ValidUntil > ValidatedAt is not required universally, but ValidUntil must be a valid absolute instant
```

Evidence covers Issue scope `S` only when:

```text
evidence.CommercialVersion == order.CommercialVersion
now < evidence.ValidUntil
S subset-of evidence.ValidatedOrderServiceIds
```

CommercialVersion mismatch means stale even if timestamp is future.

## 7.2 Historical migration

Do NOT guess exact scope/version for old rows.

Existing `ReservationValidationTimeLimit` alone is insufficient.

For historical reservations:

- preserve the old timestamp if needed for compatibility/history;
- new canonical evidence is null unless exact scope/version is mechanically proven from immutable stored facts without assumption;
- next Confirm/Issue refreshes evidence before proceeding.

Do not set current CommercialVersion merely to make old rows pass.

---

# 8. Initial Reserve / Confirm behavior

Do not break Stage 2/3.

## New reservations

When AirPrice validation is performed during reservation preparation, capture canonical evidence:

```text
CommercialVersion = current Order.CommercialVersion
ValidUntil = AirPrice returned TimeLimit
ValidatedAt = local observation time
ValidatedOrderServiceIds = exact Air services represented in the accepted validation request
```

## Held confirmation

If validation evidence is stale/not covering the required confirmation scope:

- refresh through existing AirPrice authority;
- persist new evidence;
- then confirm.

Existing safe provider Confirm replay semantics remain unchanged.

---

# 9. Issue-time validation after servicing

Current code wrongly does:

```text
provider.PlanUnits(current active covered services)
reservation.EnsurePlannedAs(units)
reservation.RenewIssueValidation(...)
```

That is not valid after partial cancellation because a validation refresh is not a reservation re-plan.

Remove that semantic coupling.

## Required flow

For each `ReservationIssueScope`:

1. exact target issue services are known;
2. exact persisted target ReservationUnits are known and Confirmed;
3. pricing atom integrity has already passed;
4. inspect current `ReservationValidationEvidence`;
5. if evidence covers current issue service scope/current commercial version and is time-current, reuse it;
6. otherwise perform a **validation-only refresh** against AirPrice/current pricing authority;
7. persist returned evidence;
8. do not change reservation units/provider refs/resource state;
9. do not call FlightFlow Reserve/Confirm/Cancel;
10. recheck evidence immediately before stock allocation/document creation.

Do not call `EnsurePlannedAs` for this Issue-time refresh.

---

# 10. AirPrice validation scope

Evolve `IAirFareReservationValidator` or equivalent current provider boundary so the result includes:

```text
ValidUntil
ValidatedOrderServiceIds
```

`ValidatedOrderServiceIds` must be the exact service IDs represented in the actual validation request, including any pricing-atom expansion required by accepted fare topology.

This is Ordering-known request evidence, not fabricated provider output.

For an intact RoundTrip atom, validation may include the whole atom.

For a fractured atom, Issue must already have failed before the validation call.

---

# 11. Current AirFareReservationValidator correction

Current `IsIndivisible` logic is insufficient because a single FareComponent can span multiple segments.

Do not use:

```text
pricingUnit.CoveredJourneyIds.Count > 1 || pricingUnit.FareComponents.Count > 1
```

as the canonical atomicity rule.

Use the R2 FarePricingAtom rules:

- OneWay -> each whole FareComponent
- RoundTrip/OpenJaw/CircleTrip -> whole PricingUnit
- Unspecified/Other -> whole PricingUnit

A OneWay FareComponent covering two connecting segments is indivisible even when it is the only FareComponent.

Update tests accordingly.

---

# 12. Unresolved-effect blocking

Keep Stage-5/Stage-4 unresolved-effect safety.

For Issue, scope decisions must be based on the actual issue ReservationUnit/OrderService targets.

Do not reintroduce a root-status shortcut.

At minimum, any unresolved Reserve/Confirm/Cancel/Release/Issue effect overlapping an issue target blocks it.

If current repository query can only operate reservation-wide, it may remain conservatively wider for this Stage **only if it cannot allow an unsafe issue**. Do not weaken safety to optimize concurrency.

Document any conservative false-positive blocking in the completion report.

---

# 13. Order status

No change to historical reservation root.

After allowed issue:

```text
Order.MarkTicketed(...)
```

continues to inspect only Active ticketable Air services.

If every remaining Active Air service has surviving document coverage:

```text
OrderStatus = Ticketed
```

Even if historical reservation root is `Mixed`.

---

# 14. Mandatory acceptance scenarios

## Reason history

R2-R01 cancellation ReasonDetail `"  customer changed plan  "` persists as:

```text
OrderChange.ReasonText = "customer changed plan"
```

R2-R02 whitespace ReasonDetail persists null.

R2-R03 query detail returns ReasonText.

R2-R04 restart/new DbContext preserves it.

## Partial cancellation + issue — allowed

R2-I01 two independent OneWay PricingUnits:
- both Confirmed;
- cancel complete inbound pricing unit;
- reservation root Mixed;
- outbound unit Confirmed;
- issue outbound succeeds;
- no root-status mutation;
- Order becomes Ticketed for remaining active scope.

R2-I02 OneWay PricingUnit with two independent FareComponents:
- cancel complete FareComponent B scope;
- FareComponent A remains intact;
- issue A succeeds.

R2-I03 stale-by-commercial-version:
- validation ValidUntil still future;
- cancellation increments CommercialVersion;
- issue forces new AirPrice validation before issuance.

R2-I04 post-cancel refreshed evidence:
- evidence stores current CommercialVersion;
- exact validated service IDs;
- returned ValidUntil;
- issue succeeds.

R2-I05 fresh evidence that does not cover target service IDs forces refresh.

## Partial cancellation + issue — blocked

R2-I06 RoundTrip PricingUnit:
- cancel one bound only;
- remaining bound active/Confirmed;
- Issue fails REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION;
- zero AirPrice call after fracture detection;
- zero stock allocation/task/ticket.

R2-I07 through fare:
- one FareComponent covers two connecting segments;
- cancel one segment;
- issue other segment blocked as fractured fare.

R2-I08 OpenJaw/CircleTrip partial atom cancellation blocked.

R2-I09 Unspecified/Other partial pricing unit blocked conservatively.

## Root summary

R2-I10 Mixed root + target Confirmed unit is not rejected merely because root is Mixed.

R2-I11 target unit Unknown/Cancelled/Released/Expired still blocks Issue.

## Validation-only behavior

R2-I12 Issue refresh does not call FlightFlow reservation mutation.

R2-I13 Issue refresh does not change ReservationUnit state/ref.

R2-I14 Issue refresh does not require active plan to equal original historical reservation plan.

R2-I15 historical reservation with legacy timestamp but no canonical scope evidence revalidates before Issue.

---

# 15. Domain tests

Add tests for:

- FarePricingAtom derivation for OneWay single component/multi-segment
- OneWay multiple FareComponents
- RoundTrip
- OpenJaw
- CircleTrip
- Unspecified/Other conservative atom
- atom fracture detection
- validation evidence coverage/version/time
- Mixed root with Confirmed target unit
- ReasonText normalization

Do not rely only on controller/application tests.

---

# 16. Persistence / migrations

Expected narrow changes:

Command DB:
- OrderChanges.ReasonText
- canonical reservation validation evidence persistence

Query DB:
- OrderChanges.ReasonText

If evidence scope uses primitive collection, follow existing repository conventions used by `ReservationUnit.OrderServiceIds`.

No destructive rewrite.

Apply migrations to configured local DB and verify history.

---

# 17. Master / authority consistency tests

Update conformance tests so they prove:

1. Master v2.0 contains `OrderChange.ReasonText`.
2. Master states reservation root status is summary only for Issue.
3. Master defines canonical validation evidence scope/version.
4. Master defines partial-cancellation pricing atom rule.
5. Stage 5 source matches these decisions.

Do not leave the R2 prompt as a competing permanent authority.

After implementation, Master v2.0 is the canonical truth.

---

# 18. Regression

Run:

- Stage 1 create/fare topology
- Stage 2 reserve/release
- Stage 3 confirm/recovery
- Stage 4 all Issue/DocumentStock/ETKT tests
- Stage 5 cancellation/void/R1 recovery tests
- Pricing validator tests
- conformance

FlightFlow unchanged.

No Payment/JetPay.
No Refund.
No EMD execution.
No Stage 6.

---

# 19. Mandatory completion report

```text
STAGE 5 R2 DOMAIN CLOSURE REPORT

SOURCE
before HEAD: 94a84a0a435b6394bffbd2713d709992be37f195
after HEAD:

MASTER
OrderChange ReasonText:
validation evidence:
Issue unit-scoped rule:
FarePricingAtom rule:
single authority preserved:

ORDER CHANGE
ReasonDetail path:
ReasonText normalization:
command persistence:
query persistence:
migration:

PRICING ATOMS
OneWay:
through FareComponent:
RoundTrip:
OpenJaw/CircleTrip:
Unspecified/Other:
fracture error code:

ISSUE SCOPE
root status used as gate: NO
target unit status:
Mixed-root allowed:
IssuanceScope granularity:

VALIDATION EVIDENCE
shape:
commercial-version guard:
service-scope guard:
time guard:
historical migration:
AirPrice result scope:
Issue refresh mutates reservation resource: NO
EnsurePlannedAs used for Issue refresh: NO

SCENARIOS
independent one-way after partial cancel:
independent one-way fare component:
roundtrip fracture:
through-fare fracture:
Mixed root + Confirmed unit:
legacy validation evidence refresh:

TESTS
R2 focused:
Stage 5:
Stage 4:
Stage 1-3:
pricing:
conformance:
DB migration/history:
environment blocked:

SCOPE
FlightFlow changed: NO
Payment/JetPay added: NO
Refund added: NO
EMD execution added: NO
Stage 6 started: NO

FINAL
STAGE5_READY_FOR_FINAL_REVIEW
```

Do not start Stage 6.
