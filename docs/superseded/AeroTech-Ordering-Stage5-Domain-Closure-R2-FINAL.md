# AeroTech Ordering — Stage 5 Domain Closure R2
## Canonical correction before Stage 5 closure

**Status:** OWNER DECISION / DOMAIN CLOSURE  
**Repository baseline:** `k8s-stg@94a84a0a435b6394bffbd2713d709992be37f195`  
**Purpose:** Correct two coarse-grained domain assumptions before Stage 5 can close.  
**Single-authority rule:** These decisions must be merged into the existing Master v2.0 in place. Do not create Master v2.1/v2.2.

---

# 1. Why R2 exists

Two current gaps are not implementation bugs; they expose missing domain semantics:

1. `CancelOrderRequest.ReasonDetail` exists but `OrderChange` has no field capable of preserving it.
2. Issue eligibility is incorrectly coupled to `FulfillmentReservation.Status == Confirmed`, even though a reservation can legitimately become `Mixed` after a partial cancellation while the remaining issue scope is still authoritatively `Confirmed`.

A third consequence follows from the second:

3. The existing `ReservationValidationTimeLimit` is only a timestamp. After a commercial change, it does not prove **which service scope and commercial version** were validated. A timestamp alone is not sufficient issuance evidence.

These are corrected canonically below.

---

# 2. OrderChange — final correction

`OrderChange` is the durable commercial-history occurrence.

Final shape for the currently materialized provenance fields:

| Field | Type | Null | Meaning |
|---|---|---:|---|
| `Id` | `long` | No | Stable change identity |
| `OrderId` | `long` | No | Parent Order |
| `ChangeType` | `OrderChangeType` | No | Create/Cancel/etc. |
| `CommercialVersion` | `int` | No | Resulting commercial version |
| `ActorContext` | `SalesContext` | No | Committing actor/channel context |
| `SourceReference` | `string?` | Yes | Upstream decision/reference when supplied |
| `SourceSystem` | `string?` | Yes | Upstream authority when supplied |
| `ReasonCode` | `string?` | Yes | Machine/business reason code |
| **`ReasonText`** | **`string?`** | **Yes** | Human-readable reason detail/annotation preserved with the change |
| `IsInvoluntary` | `bool` | No | Voluntary/involuntary |
| `WaiverCode` | `string?` | Yes | Source-approved waiver |
| `CommittedAt` | `DateTimeOffset` | No | Commit instant |

## 2.1 ReasonText invariants

- max length: `500`
- trim leading/trailing whitespace
- whitespace-only -> `null`
- not a source policy code
- not a waiver
- not free-text replacement for `ReasonCode`
- never used to derive business eligibility

Current Backoffice cancellation mapping:

```text
request.Reason       -> OrderChange.ReasonCode
request.ReasonDetail -> OrderChange.ReasonText
```

`DocumentVoidRecord.ReasonText` remains unchanged.

---

# 3. Reservation root status is summary, not issue eligibility

`FulfillmentReservation.Status` is a summary of unit truth.

Therefore:

```text
Confirmed root
```

is sufficient but **not necessary** for Issue.

A legitimate post-servicing reservation may be:

```text
Unit A = Cancelled
Unit B = Confirmed
Root   = Mixed
```

Issuance of Service B is allowed if all target-specific invariants are met.

Never change a truthful `Mixed` root back to `Confirmed` merely to pass Issue.

---

# 4. Canonical Issue eligibility

For every outstanding active ticketable Air service selected by Issue:

1. service is commercially `Active`;
2. service is not already documented by a surviving non-void ETKT coupon;
3. latest reservation coverage for that service exists when its provider requires reservation;
4. **the exact latest ReservationUnit covering that service is `Confirmed`;**
5. required provider unit/reference facts exist;
6. no overlapping unresolved fulfillment effect exists for the Issue scope;
7. current accepted pricing topology remains issueable for that service scope;
8. current reservation-validation evidence covers the issue scope and current commercial version;
9. hard ticketing deadline remains open;
10. document authority/stock and ticket snapshot data are complete.

`FulfillmentReservation.Status` alone is never an Issue gate.

---

# 5. Pricing topology after partial cancellation

A commercial partial cancellation does not automatically create a new fare.

Therefore Ordering must distinguish an **intact accepted pricing atom** from a **fractured pricing atom**.

`FarePricingAtom` is a derived concept. It is **not a persisted entity**.

## 5.1 Atom derivation

### `FarePricingUnit.SemanticType == OneWay`

Each `OrderFareComponent` is one pricing atom.

The atom contains **all Air service IDs covered by that FareComponent**.

A through fare component spanning multiple segments is therefore indivisible.

### `RoundTrip | OpenJaw | CircleTrip`

The entire `OrderFarePricingUnit` is one pricing atom:

```text
union(all FareComponent.CoveredOrderServiceIds)
```

### `Unspecified | Other`

Treat the entire PricingUnit as one conservative atom.

Do not infer partial-pricing independence.

## 5.2 Intact atom

An atom is intact for current accepted pricing when all of its relevant Air service occurrences are still commercially Active.

An atom that is entirely ended/cancelled is irrelevant to new issuance.

## 5.3 Fractured atom

If an atom contains both:

```text
at least one Active Air service
and
at least one ended/non-Active Air service
```

the original accepted pricing topology is fractured.

Current Stage 5 has no authoritative repricing/cancellation-pricing decision that replaces that topology.

Therefore:

```text
ISSUE_BLOCKED_REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION
```

No ticket stock allocation.
No Issue task.
No ETKT.
No invented new fare.
No reuse of an invalidated partial fare.

A future source-authoritative repricing/exchange/servicing decision may create successor pricing topology and then make the remaining scope issueable.

---

# 6. Consequences / examples

## 6.1 Two independent one-way PricingUnits

```text
OUT = OneWay PricingUnit A
IN  = OneWay PricingUnit B
```

Cancel IN completely.

OUT atom remains intact.

If OUT unit is Confirmed and validation is current:

```text
OUT may be issued
```

Reservation root may be Mixed.

## 6.2 Round-trip PricingUnit

One RoundTrip pricing unit covers OUT + IN.

Cancel IN only.

The pricing atom is fractured.

```text
OUT may NOT be issued from the old accepted pricing
```

A source-authoritative repricing/servicing decision is required.

## 6.3 Connecting through fare

One FareComponent covers Segment 1 + Segment 2.

Cancel Segment 2 only.

FareComponent atom is fractured.

```text
Segment 1 may NOT be issued from that through fare
```

## 6.4 One-way sector fares

One OneWay PricingUnit contains FareComponent A for Segment 1 and FareComponent B for Segment 2.

Cancel the complete FareComponent B scope.

FareComponent A remains intact.

```text
Segment 1 may be issued
```

---

# 7. Reservation validation evidence — final correction

A bare timestamp is insufficient.

Canonical latest evidence owned by `FulfillmentReservation`:

```text
ReservationValidationEvidence
{
    CommercialVersion: int
    ValidUntil: DateTimeOffset
    ValidatedAt: DateTimeOffset
    ValidatedOrderServiceIds: IReadOnlyCollection<long>
}
```

This is an owned value/evidence concept, not a new aggregate root.

Persistence may flatten it according to repository conventions.

## 7.1 Meaning

The evidence states:

> At `ValidatedAt`, the pricing/reservation authority accepted validation for the exact represented OrderService scope, against `CommercialVersion`, until `ValidUntil`.

`ValidatedOrderServiceIds` must represent the exact Air service scope included in the accepted validation request after any source-required pricing-scope expansion.

It is not guessed from the reservation after the fact.

## 7.2 Validity

Evidence is current for issue scope `S` only when:

```text
Evidence != null
Evidence.CommercialVersion == Order.CommercialVersion
now < Evidence.ValidUntil
S ⊆ Evidence.ValidatedOrderServiceIds
```

Any failure means validation must be refreshed before Issue.

A commercial cancellation increments `CommercialVersion`, so pre-cancellation evidence becomes stale even if its old timestamp has not yet expired.

## 7.3 Existing `ReservationValidationTimeLimit`

Do not keep treating the timestamp alone as canonical proof.

It may remain as a compatibility projection/storage column during migration:

```text
ReservationValidationTimeLimit == ValidationEvidence?.ValidUntil
```

but business eligibility must use the full evidence.

Historical rows for which exact scope/version cannot be proven must **not** be backfilled with guessed evidence.

They require fresh validation before the next Confirm/Issue operation.

---

# 8. Validation refresh after partial cancellation

Issue-time validation refresh is a **validation-only operation**.

It must not:

- recreate a reservation;
- mutate FlightFlow hold/capacity;
- require `FulfillmentReservation.Status == Confirmed`;
- require the current active service plan to equal the historical original reservation plan;
- call `EnsurePlannedAs` against a shrunken post-cancellation scope.

Required flow for each reservation in the issue scope:

1. identify exact issue Air services covered by that reservation;
2. prove every target service maps to a persisted ReservationUnit whose status is `Confirmed`;
3. derive intact pricing atom scope;
4. call current AirPrice reservation-validation authority for that issue/pricing scope;
5. persist returned evidence with:
   - current `CommercialVersion`;
   - exact validated service IDs;
   - `ValidUntil`;
   - `ValidatedAt`;
6. re-check evidence immediately before document allocation.

The validation call may expand a requested service to its whole intact fare/pricing atom. The persisted evidence records the exact effective validated service IDs.

---

# 9. Scope shape for issuance

Application `IssuanceScope` must retain target granularity.

Equivalent shape:

```text
IssuanceScope
  Services
  ReservationScopes[]

ReservationIssueScope
  Reservation
  ReservationUnitIds
  OrderServiceIds
```

Names may follow repository conventions; semantics may not change.

This prevents later code from falling back to reservation-root status.

---

# 10. Outstanding services after servicing

`Order.TicketableAirServices()` remains:

```text
CommercialStatus == Active
```

`OutstandingServices` remains:

```text
Active ticketable Air services
minus services already covered by surviving non-void ticket coupons
```

Cancelled Air services must never be reintroduced into Issue merely because they remain in historical fare/reservation structures.

---

# 11. Order status after successful Issue

`Order.MarkTicketed` continues to evaluate only active ticketable Air services.

Therefore after an allowed partial cancellation:

```text
Cancelled service -> ignored as ended commercial history
remaining Active services -> must all be documented
```

When all remaining active Air services are documented:

```text
OrderStatus = Ticketed
```

A historical reservation root may remain `Mixed`.

Do not rewrite reservation history to make OrderStatus Ticketed.

---

# 12. Benchmark basis

Current public benchmark supports the scope-based model:

- IATA reservation procedures require ticket issuance according to the reservation status of **each segment**, not a single PNR/root boolean.
- IATA servicing guidance explicitly recognizes full and partial cancellation.
- Amadeus ticketing supports segment selection and rejects invalid/non-active ticketing segments rather than requiring every historical PNR segment to have one common state.
- Amadeus servicing/reissue guidance requires pricing/segment scope coherence; changed itineraries are repriced/validated rather than silently reusing a fractured fare.

AeroTech therefore uses:

```text
commercial service scope
+ reservation unit truth
+ accepted pricing topology
+ current validation evidence
```

not root `OrderStatus`/`FulfillmentReservation.Status` as sole eligibility.

---

# 13. Stage-5 closure implications

Stage 5 cannot close until:

1. `ReasonText` is durable and queryable;
2. post-partial-cancel Issue works for intact independent pricing atoms;
3. post-partial-cancel Issue is blocked for fractured pricing atoms;
4. Mixed reservation root does not itself block confirmed target units;
5. validation evidence is scope/version-aware;
6. stale validation refresh does not re-plan/recreate the reservation;
7. all Stage 1–5 regressions remain green.

This is the final domain decision for these concerns.
