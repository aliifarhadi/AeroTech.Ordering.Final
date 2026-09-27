# CODING AGENT PROMPT — Ordering Stage 5 R2 + END-TO-END FLOW CONFORMANCE FINAL
## This prompt SUPERSEDES the previous Stage-5 R2 coding prompt

## 0. Goal

Do not optimize for commands that individually pass tests.

The goal is a real airline Ordering service whose implemented lifecycle behaves coherently end-to-end.

Do not invent new airline concepts.
Do not create a generic Workflow aggregate.
Do not create a Saga/ProcessManager just because flows span commands.
Do not start Stage 6.

Use the current domain owners:
- Order = commercial truth
- FulfillmentReservation = resource/reservation truth
- FulfillmentTask = external-effect execution/recovery
- ElectronicTicket = ETKT/document truth
- DocumentStock = local accountable-document authority
- AirOffer/AirPrice/FlightFlow = external authorities according to their actual contracts

Internal commands may remain separate. What must be closed is the business flow contract between them.

---

# 1. Baselines

Ordering:
- repo: aliifarhadi/AeroTech.Ordering.Final
- branch: k8s-stg
- reviewed HEAD: 94a84a0a435b6394bffbd2713d709992be37f195

FlightFlow:
- repo: aliifarhadi/Aerotech.FlightFlow
- branch: k8s-stg
- verified HEAD: d2180b2e4c07789abde75e712e870ece8d2c776b

Do not change FlightFlow unless a new source-proven defect is found.

Stage 1–4 remain CLOSED historically, but their cross-stage behavior may be corrected additively where Stage 5 exposed a real lifecycle contradiction.

---

# 2. Authority order

1. actual AeroTech source/provider contracts
2. Master v2.0
3. this prompt
4. current IATA Offers & Orders / ONE Order / Reservations / Ticketing guidance
5. public Amadeus Altéa / Sabre behavior
6. historical donor repos only as evidence, never authority

Update the existing:
docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md
in place.

Do not create Master v2.1/v2.2.

---

# 3. Preserve these current architecture decisions

Do not redesign:
- Order separated from reservation/provider execution.
- ReservationUnit is the target/resource grain.
- PNR/RecordLocator is separate from provider HoldId/operation ref.
- Unknown external outcome is not failure.
- exact persisted request is recovery authority.
- provider read-back/idempotency capability is explicit.
- ETKT is separate from Order commercial status.
- Void is not Cancel.
- Cancel is not Refund.
- Payment/JetPay is not intrinsic to Reservation/ETKT.
- FarePricingUnit/FareComponent preserve accepted source topology.
- OrderStatus/FulfillmentReservation.Status are summaries, not universal eligibility authorities.

Do not replace these with one giant Order state machine.

---

# 4. R2-A — OrderChange reason history

Add:
OrderChange.ReasonText : string?

Rules:
- max length = 500
- Trim()
- whitespace-only => null

Current cancellation mapping:
request.Reason -> ReasonCode
request.ReasonDetail -> ReasonText

Persist and expose it end-to-end:
REST -> command -> application service -> Order.Cancel -> OrderChange -> command DB -> query DB -> Backoffice order history

Do not map it to Remark, SourceReference or ReasonCode.
Historical values remain null.

---

# 5. R2-B — Issue is service/unit scoped, not reservation-root scoped

Remove the root-status gate:
coverage.Reservation.Status == Confirmed

Required Issue eligibility for every outstanding active Air service:
- latest covering ReservationUnit exists
- ReservationUnit.Status == Confirmed
- provider reference required by provider exists
- commercial service is Active
- service is not already covered by a surviving ETKT coupon
- hard ticketing deadline open
- no overlapping unresolved external effect
- pricing topology valid for current issue scope
- current validation evidence valid for current commercial version/scope
- document stock/issue data complete

A root reservation may truthfully be Mixed while target unit is Confirmed.
Do not rewrite Mixed to Confirmed.

---

# 6. R2-C — pricing topology after partial cancellation

Implement a derived pricing-scope rule. Do NOT persist a new entity.

## FarePricingAtom

### OneWay PricingUnit
Each entire FareComponent is one atom:
atom = all Air OrderServiceIds covered by that FareComponent

A FareComponent covering several connecting segments is indivisible.

### RoundTrip / OpenJaw / CircleTrip
The entire PricingUnit is one atom:
atom = union(all Air services covered by every FareComponent)

### Unspecified / Other
Conservatively treat the whole PricingUnit as one atom.

Do not invent independence.

## Fractured pricing

If one atom contains both:
- Active Air service(s)
- ended/non-Active Air service(s)

Issue must fail before external validation or stock allocation with:
REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION
HTTP 409

No new fare.
No partial reuse of a through fare.
No synthetic price.
No ETKT.

Examples:

Allowed:
OUT OneWay + IN OneWay, IN fully cancelled -> OUT may be issued if its reservation unit and validation are valid.

Blocked:
one RoundTrip PricingUnit covers OUT+IN, IN cancelled -> old fare cannot issue OUT alone.

Blocked:
one through FareComponent covers Segment1+Segment2, one cancelled -> old through fare cannot issue the other alone.

---

# 7. R2-D — reservation validation evidence must be scope/version aware

A timestamp alone is not sufficient authority.

Add canonical owned evidence to FulfillmentReservation:

ReservationValidationEvidence
- CommercialVersion : int
- ValidUntil : DateTimeOffset
- ValidatedAt : DateTimeOffset
- ValidatedOrderServiceIds : IReadOnlyCollection<long>

This is not an aggregate root.

Persistence may be flattened according to current EF conventions.

Evidence is current for scope S only when:
- evidence != null
- evidence.CommercialVersion == order.CommercialVersion
- now < evidence.ValidUntil
- S subset-of evidence.ValidatedOrderServiceIds

A cancellation increments CommercialVersion, so pre-cancellation validation is stale even if time is still in the future.

Historical data:
Do not infer/backfill scope/version from a timestamp.
Legacy rows without canonical evidence must revalidate before next Confirm/Issue.

---

# 8. R2-E — validation refresh is not reservation replanning

Current Issue logic rebuilds provider units and calls reservation.EnsurePlannedAs(units). This is wrong after servicing.

For Issue-time refresh:
- derive exact target issue Air service scope
- prove exact persisted target ReservationUnits are Confirmed
- validate pricing atom integrity
- call AirPrice validation only
- persist new validation evidence
- do not change ReservationUnit state
- do not change provider refs
- do not call FlightFlow Reserve/Confirm/Cancel
- do not require shrunken active plan == original reservation plan
- do not call EnsurePlannedAs for Issue refresh

---

# 9. AirPrice validation contract

Inspect the actual current AirPrice request/response source available to the repo.

Do not invent provider fields.

Ordering may derive from its own actual validation request:
- ValidatedOrderServiceIds
- ValidatedAt
- CommercialVersion

and from the real AirPrice response:
- ValidUntil / TimeLimit

Stored service IDs must be the exact effective Air services represented in the request.

Canonical pricing scope:
- OneWay => whole FareComponent
- RoundTrip/OpenJaw/CircleTrip => whole PricingUnit
- Unspecified/Other => whole PricingUnit

Do not use the current IsIndivisible shortcut as canonical authority.

---

# 10. Create-from-Offer authority check

Current source:
CreateOrderFromOfferService -> IOfferProvider.GetByOfferIdAsync -> Order.Create(snapshot)

Inspect the actual AirOffer contract/source available to the Coding Agent.

Answer exactly:
Does FlightOffers/Details return an offer authoritative and valid for acceptance at Order creation, including accepted price/time-limit semantics?

Return:
OFFER_CREATE_AUTHORITY = VERIFIED
with exact source evidence,

or:
OFFER_CREATE_AUTHORITY = NOT_VERIFIED

If NOT_VERIFIED:
- do not invent a reprice endpoint
- do not redesign Stage 1
- add an explicit source-gated production gap to Master/report
- keep reserve-time AirPrice validation
- do not claim Create->Reserve is production-price-safe until contract is clarified

If VERIFIED:
- document exact guarantee in Master
- keep current Create flow if it matches

---

# 11. Add canonical implemented lifecycle flows to Master

Add section:
Implemented Flow Contracts — Stage 1 through Stage 5

For each flow specify:
- starting state
- command sequence
- business preconditions
- external authority
- local commit point
- resulting Order/Reservation/Document truth
- ambiguous outcome behavior
- retry/recovery behavior
- terminal outcome

Do not add new domain concepts just to document flows.

---

# 12. Mandatory END-TO-END flow conformance suite

FLOW-01 Create -> Reserve -> Confirm
- commercial snapshot preserved
- one host RecordLocator
- provider refs remain provider refs
- active target units Confirmed
- Order summary Confirmed

FLOW-02 Create -> Reserve -> Confirm -> Issue
- only active ticketable Air services issued
- exact coupon/service coverage
- stock consumed once
- Order Ticketed only when remaining active Air scope fully documented

FLOW-03 Reserve Unknown -> retry
- exact persisted request
- no rebuilt mutable request
- provider capability branch respected
- no duplicate resource

FLOW-04 Confirm Unknown -> retry/reconcile
- no Issue while unresolved

FLOW-05 Held -> Cancel all
- Release first
- commercial Cancel only after resource settled
- history preserved
- Order Cancelled

FLOW-06 Confirmed -> Cancel all
- exact confirmed resource cancellation
- commercial cancellation after settlement
- no Refund semantics

FLOW-07 scoped CancelConfirmed lost response
Use the R1 A/B-unit scenario and prove persisted target scope governs recovery.

FLOW-08 Create -> Reserve -> Confirm -> Issue -> Void -> Cancel
- Void does not cancel service
- capacity remains Confirmed after Void
- CancelConfirmed ends capacity
- final Order Cancelled
- all commercial/document/provider history preserved

FLOW-09 late provider expiry after Issue
Issued/ticketed service must not be rolled back or expired.

FLOW-10 partial cancellation, independent OneWay
- OUT OneWay, IN OneWay, both Confirmed
- cancel IN
- reservation root Mixed
- issue OUT
- OUT target unit Confirmed is sufficient
- refresh validation for current commercial version if needed
- OUT ETKT succeeds
- Order Ticketed for remaining active scope
- reservation root remains Mixed

FLOW-11 partial cancellation fractures RoundTrip
- one RoundTrip PricingUnit covers OUT+IN
- cancel IN
- Issue OUT => REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION
- zero AirPrice after fracture detection
- zero stock allocation/task/ticket

FLOW-12 partial cancellation fractures through fare
- one FareComponent spans two connecting segments
- cancel one
- issue remaining one blocked

FLOW-13 stale validation after commercial change
- old ValidUntil still future
- CommercialVersion changed
- Issue revalidates

FLOW-14 terminal reservation -> new reserve
- terminal reservation remains history
- new eligible reserve uses new logical reservation identity/request

FLOW-15 repeated local Issue safety
- no second document allocation for already-issued active scope

FLOW-16 repeat Cancel/Void safety
No duplicate:
- OrderChange
- CommercialVersion increment
- provider effect
- DocumentVoidRecord
- document task

---

# 13. Channel practicality audit

Inspect actual controllers.

Reviewed source currently shows:
- Backoffice exposes granular Create/Reserve/Confirm/Issue/Cancel/Void across controllers.
- OTA/OtaPanel expose Create/Get/Remarks only.
- Internal exposes reservation operations but no complete purchase/issue flow.
- Service-to-Service controller is effectively empty.

Do not invent public endpoints.

Produce matrix:
Surface | Create | Reserve | Confirm | Issue | Cancel | Void | Get

For every missing operation classify:
- INTENTIONAL_BOUNDARY
- FUTURE_STAGE
- MISSING_PRODUCT_FLOW

based on actual product/source authority.

If no authoritative channel/orchestrator contract exists:
CHANNEL_ORCHESTRATION_CONTRACT = SOURCE_GATED

This audit is mandatory because individually correct core commands do not make an OTA/agency product usable.

---

# 14. Do NOT create a generic Booking workflow aggregate

Sabre/Amadeus expose both granular capabilities and orchestrated workflows.
IATA separates capabilities while expecting one coherent order lifecycle.

For AeroTech:
- existing domain commands remain composable primitives
- flow correctness is enforced by domain invariants + flow tests
- channel orchestration belongs at application/channel boundary when an actual product contract exists
- no generic BookingWorkflow, OrderProcess, SagaState or BusinessOperation aggregate now

---

# 15. No-guess gate for every external operation

Report:
- AcceptsIdempotencyIdentity
- DuplicateMutationSemantics
- SupportsAuthoritativeReadBack
- SupportsSafeReplayAfterAmbiguousOutcome
- ResultGranularity
- ExpiryAuthority

for:
- Offer retrieval/acceptance semantics
- AirPrice reservation validation
- FlightFlow Reserve
- Confirm
- Release
- CancelConfirmed

Unknown != failed.

---

# 16. Remaining-gap classification

At end classify exact gaps as:
- BLOCKS_CURRENT_STAGE_PRODUCTION
- BLOCKS_OTA_PRODUCT
- FUTURE_AIRLINE_CAPABILITY
- SOURCE_GATED

Evaluate actual source for:
- Offer acceptance guarantee
- incomplete OTA orchestration surface
- external issuer
- EMD/ancillary
- Refund/Exchange
- split
- DCS/delivery
- group/charter
- payment policy/orchestration

Do not mark a future Stage as a Stage-5 blocker unless it really prevents Stage-5 correctness.

---

# 17. Tests

Run/report:
- new R2 tests
- FLOW-01..FLOW-16
- Stage 1 acceptance
- Stage 2 reservation
- Stage 3 confirm/recovery
- Stage 4 issue/DocumentStock
- Stage 5 cancel/void/R1
- pricing validation
- conformance
- DB migration/history
- FlightFlow focused regression if integration contract touched

No fake pass counts.

---

# 18. Scope exclusions

Do not add:
- Payment/JetPay
- Refund
- Exchange/Reissue
- EMD execution
- generic ancillary catalog
- DCS
- Group/Charter implementation
- external issuer
- new workflow aggregate
- Stage 6

---

# 19. Completion report

STAGE 5 R2 + FLOW CONFORMANCE REPORT

SOURCE
before HEAD: 94a84a0a435b6394bffbd2713d709992be37f195
after HEAD:
FlightFlow HEAD:

MASTER
single authority:
ReasonText:
unit-scoped Issue:
pricing atom:
validation evidence:
flow-contract section:

CREATE/OFFER AUTHORITY
AirOffer source inspected:
exact source:
OFFER_CREATE_AUTHORITY:
guarantee:
remaining gap:

FLOW MODEL
commands remain separate primitives: YES
generic workflow aggregate added: NO
cross-command flow invariants:
channel orchestration authority:

ORDER CHANGE
ReasonDetail -> ReasonText:
normalization:
command/query persistence:

ISSUE AFTER SERVICING
root status gate removed:
target unit rule:
Mixed root:
pricing fracture rule:
validation evidence:
Issue refresh calls EnsurePlannedAs: NO
Issue refresh mutates FlightFlow resource: NO

FLOW CONFORMANCE
FLOW-01:
FLOW-02:
FLOW-03:
FLOW-04:
FLOW-05:
FLOW-06:
FLOW-07:
FLOW-08:
FLOW-09:
FLOW-10:
FLOW-11:
FLOW-12:
FLOW-13:
FLOW-14:
FLOW-15:
FLOW-16:

CHANNEL MATRIX
Backoffice:
OTA:
OTA Panel:
Internal:
Service-to-Service:
CHANNEL_ORCHESTRATION_CONTRACT:

PROVIDER CAPABILITY MATRIX
Offer:
AirPrice:
Reserve:
Confirm:
Release:
CancelConfirmed:

REMAINING GAPS
BLOCKS_CURRENT_STAGE_PRODUCTION:
BLOCKS_OTA_PRODUCT:
FUTURE_AIRLINE_CAPABILITY:
SOURCE_GATED:

REGRESSION
Stage1:
Stage2:
Stage3:
Stage4:
Stage5:
pricing:
conformance:
DB:
environment blocked:

SCOPE
Payment added: NO
Refund added: NO
EMD execution added: NO
DCS added: NO
Group/Charter added: NO
Stage6 started: NO

FINAL
STAGE5_READY_FOR_FINAL_REVIEW

Do not close Stage 5 yourself.
