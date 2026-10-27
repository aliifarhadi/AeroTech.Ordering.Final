# AeroTech Ordering — Stage 5 Final Source Review

## Source reviewed

Ordering:
- repo: `aliifarhadi/AeroTech.Ordering.Final`
- branch: `k8s-stg`
- reviewed HEAD: `cd50a2a5fd18a372f32f2d4efac08dee0510f6ba`
- baseline: `94a84a0a435b6394bffbd2713d709992be37f195`
- delta: 4 commits

FlightFlow:
- repo: `aliifarhadi/Aerotech.FlightFlow`
- branch: `k8s-stg`
- verified unchanged HEAD: `d2180b2e4c07789abde75e712e870ece8d2c776b`

## Final verdict

`STAGE_5_CLOSED`

Stage 5 core/domain behavior is source-clean at the reviewed Ordering HEAD.

## What is now correct

- `OrderChange.ReasonText` is persisted, normalized, projected and exposed by Backoffice order detail.
- Issue eligibility is based on exact `ReservationUnit.Status == Confirmed`, not reservation-root `Confirmed`.
- `Mixed` reservation root can truthfully remain Mixed while a confirmed surviving subset is issued.
- `FarePricingAtom` is derived from accepted pricing topology and blocks fractured fares before AirPrice/document effects.
- OneWay uses whole FareComponent atoms.
- non-OneWay pricing units are conservatively whole-unit atoms.
- through-fare partial cancellation is blocked from unsafe issue.
- canonical `ReservationValidationEvidence` is commercial-version, time and service-scope aware.
- old timestamp-only validation is not authoritative.
- issue-time validation refresh is validation-only and no longer re-plans the historical reservation.
- cancellation makes old validation stale through CommercialVersion.
- FLOW-01..FLOW-16 exist as real application/domain lifecycle tests using the actual Create/Reserve/Confirm/Issue/Cancel/Void services with external providers stubbed at their boundaries.
- FlightFlow is unchanged from the previously accepted Stage-5 provider implementation.
- no generic workflow/saga aggregate was introduced.
- no Payment, Refund, EMD execution, DCS, Group/Charter or Stage 6 code was added.

## Remaining production gaps — not Stage-5 closure blockers

### 1. Offer acceptance authority — highest priority

`OFFER_CREATE_AUTHORITY = NOT_VERIFIED`

Current Ordering calls AirOffer `FlightOffers/Details` and snapshots the returned offer.

The Master records that the inspected AirAvail source re-prices on Details and does not prove:
- persisted offer identity;
- offer TTL / acceptance window;
- tamper-proof OfferId;
- guaranteed accepted amount between Shop/Create/Reserve.

This means the current Create -> Reserve sale path must not yet be described as production-price-safe.

This should be closed in parallel with future Ordering stages. Do not invent a local reprice mechanism in Ordering.

### 2. Channel orchestration

`CHANNEL_ORCHESTRATION_CONTRACT = SOURCE_GATED`

Current surfaces:
- Backoffice: complete Stage 1–5 granular lifecycle.
- OTA: Create/Get/Remarks.
- OTA Panel: Create/Get/Remarks.
- Internal: maintenance reservation subset.
- Service-to-Service: no sell flow.

The domain primitives are usable; the OTA/agency product flow is not complete until an authoritative channel/orchestrator contract exists.

Do not solve this by adding a Workflow aggregate.

### 3. Create idempotency / duplicate booking

Repeated Create currently produces a new Order.

This is acceptable as a documented core limitation, but a production channel must eventually supply an authoritative duplicate/idempotency policy. Keep this at the channel/application boundary unless a business contract proves otherwise.

### 4. Future airline capabilities

Still intentionally pending:
- Ancillary / EMD
- Split/divide
- Refund
- Exchange/Reissue/Revalidation
- Group/Charter
- DCS/delivery
- disruption
- external document issuer
- JetPay financial policy/orchestration

They are future capability work, not evidence that Stage 5 failed.

## Current quality assessment

For the implemented Stage 1–5 core:
- domain ownership/model: about 9/10
- external-effect/recovery discipline: about 9/10
- cross-command flow coherence: about 8.5/10
- practical airline Backoffice core: about 8.5/10
- benchmark alignment for implemented scope: about 85%
- OTA/agency product completeness: about 5/10 because orchestration/surfaces are intentionally incomplete
- whole airline Ordering capability horizon: roughly 60% because major later servicing capabilities remain unimplemented

The largest remaining risks are no longer basic aggregate design. They are:
1. external source/authority contracts;
2. channel orchestration;
3. later airline servicing capabilities.

## Process rule from now on

No future Stage closes because its commands work independently.

Closure requires:

`Domain shape correct`
+
`provider/source semantics verified`
+
`canonical end-to-end flows pass`

Before Stage 6 coding, close the Ancillary/EMD domain and its real lifecycle flows first.
