# CODING AGENT PROMPT — AeroTech Ordering Stage 4 v1.0 FINAL
## Issue / Electronic Ticket / Document Stock / Document-Oriented Fulfillment

### Execution mode

Implement this Stage end-to-end in one pass.

Do **not** stop for design decisions. All domain/product decisions required by this Stage are frozen below.

Only stop with:

```text
BLOCKED_SOURCE: <exact missing or contradictory authoritative fact>
```

when the current source genuinely contradicts the frozen decisions or an implementation-critical fact cannot be obtained without inventing provider/business truth.

The following are **NOT blockers** and must not stop this Stage:

- no external ETKT issuer contract exists today;
- no current EMD issuance profile exists today;
- no Payment/JetPay contract exists today;
- no DCS document-control contract exists today.

Those are explicitly source-gated/deferred by this Stage.

Do not begin Stage 5.

---

# 0. Reviewed baselines

## Ordering

Repository:

```text
https://github.com/aliifarhadi/AeroTech.Ordering.Final
```

Branch:

```text
k8s-stg
```

Reviewed Stage-3-closed HEAD:

```text
ff51dd04cf7254f97f570744a06368599d8b2a80
```

## FlightFlow

Repository:

```text
https://github.com/aliifarhadi/Aerotech.FlightFlow
```

Branch:

```text
k8s-stg
```

Reviewed HEAD:

```text
fe70d1cb584765cb5ddff170538d912ad1ca380a
```

Before modifying code:

1. fetch both repositories;
2. report the actual HEADs;
3. if either differs from the reviewed HEAD, inspect the changed source first;
4. preserve all Stage 1–3 closed behavior;
5. do not mechanically apply this prompt to stale source.

---

# 1. Authority

Authority order:

1. `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`
2. this Stage-4 document
3. current AeroTech source contracts
4. current public IATA / ATPCO / Amadeus / Sabre business semantics
5. historical donor code only as implementation evidence

Historical code/specs must not override Master v2.0.

In particular:

- `aliifarhadi/Ordering@k8s-stg` is a donor only;
- old `TrafficDocument` design is not authority;
- old Funding/Payment gates are not authority;
- old generic `OperationId` business-operation aggregate is not authority;
- old ticket-number random generation is not authority;
- old EMD implementations may be inspected for patterns but not copied as design authority.

Add this exact Stage document to active repo authority as:

```text
docs/CODING-AGENT-PROMPT-Ordering-Stage4-Issue-v1.0-FINAL.md
```

Update the Stage authority paragraph in `CLAUDE.md` so Stage 4 is the active materialization authority after Master v2.0. Do not demote Stage-3 documents from historical traceability; Stage 3 remains CLOSED.

---

# 2. Stage status entering this work

```text
STAGE_1_CLOSED
STAGE_2_CLOSED
STAGE_3_CLOSED
STAGE_4_IN_PROGRESS
```

Stage 4 ends only when the local-authority issuance path below is working and tested.

---

# 3. Product meaning of Stage 4

Stage 4 converts confirmed commercial air entitlement into accountable travel documents.

Current executable path:

```text
Accepted Order
    ↓
Reserved Capacity
    ↓
Confirmed Capacity
    ↓
Fresh issue-time fare/reservation validation
    ↓
Local accountable document number allocation
    ↓
ElectronicTicket
    ↓
TicketCoupon(s)
    ↓
TicketPriceLink(s)
    ↓
OrderStatus.Ticketed
```

This Stage must produce truthful ETKT/coupon history suitable for later:

- Void
- Refund
- Exchange/Reissue
- Revalidation
- DCS/control observations
- Revenue Accounting facts

without redesigning the Stage-4 document model later.

---

# 4. Benchmark conclusions — frozen decisions

These conclusions are business-semantics guardrails, not instructions to reproduce proprietary vendor internals.

## 4.1 Reservation and ticketing remain distinct

Amadeus publicly separates Reservation, Inventory, Ticketing and DCS capabilities.

ATPCO Category 5 separately models reservation and ticketing restrictions.

Therefore:

- a Confirmed reservation is not itself an ETKT;
- ticket issuance does not rewrite reservation history;
- `FulfillmentReservation` remains the capacity/resource truth;
- `ElectronicTicket` becomes document truth;
- `OrderStatus` remains a summary only.

## 4.2 Coupon-level ticket truth is mandatory

Sabre electronic-document behavior exposes coupon-level status such as Open, Used, Void, Exchanged, Refunded and Suspended.

Therefore TicketCoupon is a real entity with its own immutable issuance facts and future financial/control lifecycle.

Do not reduce a ticket to:

```text
TicketNumber + bool IsUsed
```

## 4.3 EMD is distinct from ETKT

IATA defines EMD as an accountable electronic document for optional/ancillary services, with:

```text
EMD-A = Associated to ET
EMD-S = Standalone
```

and real source semantics such as RFIC/RFISC/reason-for-issuance information.

Therefore:

- do not merge EMD into ElectronicTicket;
- do not treat every ancillary or SeatAssignment as an EMD automatically;
- do not synthesize RFIC/RFISC from service type.

## 4.4 Document number authority may be local or external

The Master explicitly supports:

```text
DocumentAuthority.Local
DocumentAuthority.External
```

IATA accountable-document semantics and Sabre/Amadeus behavior both support externally authoritative document numbers.

Therefore:

- local document stock is valid;
- external issuer numbers are valid;
- local stock must never be forced on an external issuer;
- but no fake external issuer may be created when no current contract exists.

## 4.5 Current Stage-4 executable authority is Local only

Current AeroTech source contains no authoritative current external ETKT issuance API/port contract with verified:

```text
idempotency identity
duplicate semantics
read-back
safe replay
result granularity
document-number authority
```

Therefore Stage 4 implements:

```text
DocumentAuthority.Local
```

as the working issuance path.

External authority remains a supported domain concept but has no executable provider adapter in Stage 4.

Do not create:

- DeterministicDocumentIssuer;
- MockTicketIssuer;
- AlwaysSuccessIssuer;
- UnconfiguredIssuer that pretends issuance occurred;
- fake read-back;
- fake external idempotency semantics.

## 4.6 Current EMD executable scope is empty

Current accepted Order source has:

- AirTransportation services;
- SeatAssignment services;
- seat number;
- associated air service;

but does **not** currently provide a complete authoritative EMD issuance profile such as:

- RFIC;
- RFISC / ReasonForIssuanceSubCode;
- ServiceSubCode;
- EMD-A/EMD-S choice;
- EMD document value attribution for the seat;
- current external/local EMD issuer contract.

Current SeatAssignment existence is not evidence that an EMD must be issued.

Therefore:

```text
EMD_SOURCE_GATED_DEFERRED
```

is an **approved Stage-4 result**, not a Stage blocker.

Do not materialize an executable EMD issuance flow in this Stage merely for completeness.

Keep existing EMD enums/wire contracts unchanged.

Stage 6 will materialize chargeable ancillary/EMD behavior when real source data exists.

---

# 5. Explicit Stage-4 scope

## Implement now

1. `ElectronicTicket` aggregate.
2. `TicketCoupon`.
3. `IssuedSegmentSnapshot`.
4. `TicketPriceLink`.
5. `DocumentIssuanceContext`.
6. `DocumentStock` aggregate.
7. `DocumentStockAllocation`.
8. local accountable ETKT number allocation.
9. `IssueOrder`.
10. Issue-time eligibility.
11. issue-time AirPrice validation refresh.
12. `FulfillmentTask` document/general target generalization.
13. local Issue task/evidence.
14. ETKT persistence.
15. DocumentStock persistence.
16. migrations.
17. local DB update.
18. ElectronicTicket read/query projection.
19. `ElectronicTicketIssued` integration event publication using existing V1 wire compatibility.
20. Backoffice Issue endpoint.
21. Backoffice DocumentStock administration needed to make the Stage usable.
22. comprehensive acceptance/conformance/sabotage tests.

## Do not implement now

- Payment
- JetPay
- FundingCoverage
- PaymentIntent
- PaymentSession
- wallet
- credit
- mock payment
- payment feature flag
- EMD issuance without source profile
- EMD-A association flow
- EMD-S fee/deposit/residual flow
- external ETKT issuer
- external ETKT recovery
- Void
- Cancel issued documents
- Refund
- Exchange/Reissue
- Revalidation
- Split
- Group
- Charter
- DCS/check-in/boarding
- revenue recognition
- interline settlement
- generic `TrafficDocument`
- generic business-operation aggregate
- random ticket number generation

---

# 6. Current-source facts that must remain true

## 6.1 Current FlightFlow capacity model

FlightFlow capacity has:

```text
Held
Confirmed
```

and currently treats these as sold capacity.

There is no authoritative current Stage-4 FlightFlow API that converts:

```text
Confirmed → Ticketed
```

The old `WhenOrderIssued` consumer is currently a no-op and is not evidence of a ticketed-capacity mutation contract.

Therefore:

> Stage 4 must make **zero FlightFlow mutations**.

Do not add a FlightFlow ticketing/commit endpoint.

Confirmed resource truth remains Confirmed after ticket issuance.

## 6.2 Current accepted pricing can support ticket attribution

Current Ordering pricing has service-level `PricingAllocation` for accepted Air pricing lines.

For initial air sale:

```text
PricingLine
  -> PricingAllocation
       -> OrderServiceId
       -> OrderSegmentId
       -> TravellerId
```

Use that evidence for ticket coupon value and typed `TicketPriceLink`.

Do not recalculate fare/tax amounts independently.

## 6.3 Current fare topology is source-preserved

Use persisted:

```text
OrderFarePricingUnit
OrderFareComponent
CoveredOrderServiceIds
```

for issue-time fare-component linkage and AirPrice validation.

Never reconstruct fare topology by:

- segment count;
- journey count;
- same AirFareId alone;
- booking class alone.

## 6.4 Current ticketing-related public enum values already exist

Preserve exact existing values.

Do not renumber or casually rename them.

---

# 7. Aggregate — ElectronicTicket

Create a real aggregate root named:

```text
ElectronicTicket
```

Do not introduce `TrafficDocument` as a replacement.

## 7.1 Root fields

Implement the Stage-4 materialized root with these fields:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OriginalOrderId` | `long` | No |
| `CurrentServicingOrderId` | `long` | No |
| `TravellerId` | `long` | No |
| `TravellerProfileRevisionId` | `long` | No |
| `IssueFulfillmentTaskId` | `long` | No |
| `DocumentNumber` | `string` | No |
| `IssuanceContext` | `DocumentIssuanceContext` | No |
| `Authority` | `DocumentAuthority` | No |
| `IssuedAt` | `DateTimeOffset` | No |
| `VoidDeadline` | `DateTimeOffset?` | Yes |
| `IssuedTotal` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `ProviderReference` | `string?` | Yes |
| `StatusSummary` | `ElectronicTicketStatus` | No |
| `DocumentVersion` | `int` | No |
| `PredecessorElectronicTicketId` | `long?` | Yes |
| `PredecessorExchangeChangeId` | `long?` | Yes |
| `Coupons` | collection | No |
| `PriceLinks` | collection | No |

Do not materialize Stage-5+ servicing behavior merely as architecture decoration.

For initial local issue:

```text
OriginalOrderId = Order.Id
CurrentServicingOrderId = Order.Id
IssueFulfillmentTaskId = Issue task Id
Authority = Local
ProviderReference = null
StatusSummary = Issued
DocumentVersion = 1
PredecessorElectronicTicketId = null
PredecessorExchangeChangeId = null
VoidDeadline = null
```

`VoidDeadline` remains null until a real issuer/source provides it.

## 7.2 Invariants

- DocumentNumber required and immutable.
- IssueFulfillmentTaskId required.
- at least one coupon.
- coupon numbers unique within ticket.
- coupons ordered deterministically.
- all coupons belong to same traveller as ticket.
- all Stage-4 ticket coupons are current active AirTransportation service occurrences.
- IssuedTotal reconciles to ticket coupon/price-link value.
- all Stage-4 ticket currency values are in one document currency.
- Stage-4 local document currency = Order.CurrencyId.
- `DocumentVersion` starts at 1.
- issuance history is immutable after issue except through later explicit servicing Stages.

---

# 8. Value object — DocumentIssuanceContext

Implement:

| Field | Type | Null |
|---|---|---:|
| `IssuerCarrierId` | `int` | No |
| `ValidatingCarrierId` | `int?` | Yes |
| `IssuingOfficeId` | `long?` | Yes |
| `IssuedByActorId` | `long?` | Yes |
| `TravelAgencyId` | `long?` | Yes |
| `AgencyIataNumber` | `string?` | Yes |
| `Pcc` | `string?` | Yes |
| `SalesChannel` | `SalesChannel?` | Yes |
| `SourceFormOfPaymentCode` | `string?` | Yes |

## Current local-authority mapping

For Stage 4:

```text
IssuerCarrierId = selected DocumentStock.OwnerAirlineId
ValidatingCarrierId = null
IssuingOfficeId = selected DocumentStock.OfficeId
IssuedByActorId = null unless current authenticated issuance principal exposes an authoritative actor ID
TravelAgencyId = Order.SalesContext.TravelAgencyId
AgencyIataNumber = null
Pcc = null
SalesChannel = Order.SalesContext.Channel
SourceFormOfPaymentCode = null
```

Important:

- do not set `IssuedByActorId = Order.SalesContext.ActorId` merely because it is available; that actor is the accepted sale actor, not automatically the issue actor;
- do not synthesize Agency IATA number;
- do not synthesize PCC;
- do not synthesize form of payment;
- if the current Backoffice identity infrastructure exposes an authoritative issue-time actor in an existing pattern, it may be used after source verification; otherwise keep null.

---

# 9. Entity — TicketCoupon

Implement exactly:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `TicketId` | `long` | No |
| `CouponNumber` | `int` | No |
| `OriginalOrderServiceId` | `long` | No |
| `CurrentOrderServiceId` | `long` | No |
| `OrderSegmentId` | `long` | No |
| `OrderFareComponentId` | `long?` | Yes |
| `PredecessorTicketCouponId` | `long?` | Yes |
| `IssuedSegment` | `IssuedSegmentSnapshot` | No |
| `FareBasisSnapshot` | `string?` | Yes |
| `BookingClassSnapshot` | `string?` | Yes |
| `RbdIdSnapshot` | `long?` | Yes |
| `CabinClassIdSnapshot` | `long?` | Yes |
| `BaggageAllowanceSnapshot` | `BaggageAllowance?` | Yes |
| `IssuanceValue` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `FinancialStatus` | `TicketCouponFinancialStatus` | No |
| `ControlStatus` | `TicketCouponControlStatus` | No |
| `ProviderCouponStatusCode` | `string?` | Yes |
| `NotValidBefore` | `DateOnly?` | Yes |
| `NotValidAfter` | `DateOnly?` | Yes |
| `UsedAt` | `DateTimeOffset?` | Yes |
| `UsageReference` | `string?` | Yes |

Initial local issuance:

```text
OriginalOrderServiceId = airService.Id
CurrentOrderServiceId = airService.Id
OrderSegmentId = airService.SegmentId
PredecessorTicketCouponId = null
FinancialStatus = Open
ControlStatus = Local
ProviderCouponStatusCode = null
NotValidBefore = null
NotValidAfter = null
UsedAt = null
UsageReference = null
```

## 9.1 Fare component link

Resolve the unique accepted `OrderFareComponent` whose `CoveredOrderServiceIds` contains the Air service.

If exactly one exists:

```text
OrderFareComponentId = component.Id
```

If none exists where current accepted sale should have it:

```text
BLOCK ISSUE BEFORE STOCK ALLOCATION
```

If more than one exists:

```text
BLOCK ISSUE AS AMBIGUOUS ACCEPTED FARE TOPOLOGY
```

Do not choose first.

## 9.2 Fare snapshots

Copy accepted source-preserved facts:

```text
FareBasisSnapshot = airService.FareBasis
BookingClassSnapshot = airService.BookingClass
RbdIdSnapshot = airService.RbdId
CabinClassIdSnapshot = airService.CabinClassId
BaggageAllowanceSnapshot = airService.CheckedBaggageAllowance
```

Do not merge cabin baggage into checked baggage.

---

# 10. Value object — IssuedSegmentSnapshot

Implement:

| Field | Type | Null |
|---|---|---:|
| `MarketingAirlineId` | `int` | No |
| `OperatingAirlineId` | `int` | No |
| `FlightNumber` | `string?` | Yes |
| `OriginAirportId` | `int` | No |
| `DestinationAirportId` | `int` | No |
| `DepartureDateTime` | `DateTimeOffset` | No |
| `ArrivalDateTime` | `DateTimeOffset` | No |
| `BookingClass` | `string?` | Yes |
| `RbdId` | `long?` | Yes |
| `CabinClassId` | `long?` | Yes |
| `SourceSegmentReference` | `string?` | Yes |

Map from immutable accepted OrderSegment + OrderAirTransportService facts.

`SourceSegmentReference` remains null unless current accepted source supplies an authoritative value.

Snapshot is immutable. Operational schedule/DCS/disruption facts never rewrite it.

---

# 11. Entity — TicketPriceLink

Implement:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicTicketId` | `long` | No |
| `TicketCouponId` | `long?` | Yes |
| `PricingLineId` | `long` | No |
| `PricingAllocationId` | `long?` | Yes |
| `AttributedValue` | `decimal` | No |
| `CurrencyId` | `int` | No |

## 11.1 Source mapping

For one Air service, select accepted PricingLine allocations where:

```text
allocation.OrderServiceId == airService.Id
AND
pricingLine.Treatment == CustomerPrice
```

For each allocation:

```text
PricingLineId = line.Id
PricingAllocationId = allocation.Id
CurrencyId = allocation.EquivalentCurrencyId
AttributedValue =
    +allocation.EquivalentAmount when line.Direction == Credit
    -allocation.EquivalentAmount when line.Direction == Debit
```

Require:

```text
CurrencyId == Order.CurrencyId
```

for Stage-4 local issue.

The TicketCoupon `IssuanceValue` is the signed sum of its links.

Require:

```text
IssuanceValue >= 0
```

Do not silently clamp negatives.

`ElectronicTicket.IssuedTotal` is the sum of coupon issuance values.

Do not force Order-scoped fees/commissions with no service allocation into a ticket coupon.

Do not invent allocation of an Order-scoped line merely so `TicketTotal == Order.CustomerTotal`.

---

# 12. Ticket planning

## 12.1 Current Stage-4 document plan

Create one local ElectronicTicket per traveller for all currently outstanding ticketable Air services of that traveller.

This is a **current capability profile**, not a permanent uniqueness invariant.

Do not create a database/domain rule that says one traveller can only ever have one ticket.

Master scenario S86 explicitly allows multiple ETKTs for one traveller when later issuer/document capability requires split scope.

## 12.2 Coupon ordering

Within a ticket, sort coupons by:

1. `OrderJourney.Sequence`
2. `OrderSegment.Sequence`
3. deterministic service identity as final tie-breaker

CouponNumber is `1..N` in that order.

Do not infer order from database insertion order.

## 12.3 Lap infant

A lap infant has its own commercial AirTransportation service occurrences even though it consumes no independent seat.

If its accepted Air scope is confirmed through shared reservation coverage, it receives its own ETKT/coupons under current local plan.

Do not omit INF merely because no independent FlightFlow seat was consumed.

---

# 13. Aggregate — DocumentStock

Materialize exactly:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OwnerAirlineId` | `int` | No |
| `OfficeId` | `long?` | Yes |
| `DocumentKind` | `AccountableDocumentKind` | No |
| `Prefix` | `string` | No |
| `SerialWidth` | `int` | No |
| `CheckDigitProfile` | `string` | No |
| `RangeFrom` | `long` | No |
| `RangeTo` | `long` | No |
| `NextNumber` | `long` | No |
| `Status` | `DocumentStockStatus` | No |
| `Allocations` | collection | No |

Use exact existing `AccountableDocumentKind` and `DocumentStockStatus` values.

## 13.1 Stage-4 supported stock profile

Support only:

```text
CheckDigitProfile = "None"
```

Any other profile must fail explicitly as unsupported.

IATA accountable document numbers may include airline code, form code, serial and sometimes check digit, but exact carrier/document check-digit profile is not established in current AeroTech source.

Do not invent a generic BSP/IATA modulus algorithm and call it compliant.

## 13.2 Number formatting

Current local format:

```text
DocumentNumber = Prefix + Serial left-padded to SerialWidth
```

`Prefix` is configuration authority.

Do not hardcode airline-code length, form-code length, 13 digits, or check digit into domain rules until a real AeroTech stock contract requires them.

---

# 14. Entity — DocumentStockAllocation

Implement:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `DocumentStockId` | `long` | No |
| `IssueFulfillmentTaskId` | `long` | No |
| `DocumentRole` | `string` | No |
| `Serial` | `long` | No |
| `DocumentNumber` | `string` | No |
| `State` | `StockNumberState` | No |
| `AllocatedAt` | `DateTimeOffset` | No |
| `SettledAt` | `DateTimeOffset?` | Yes |

## 14.1 Idempotent allocation

For one stock:

```text
(IssueFulfillmentTaskId, DocumentRole)
```

identifies one logical allocation. Calling Allocate again with same pair returns same non-retired allocation.

## 14.2 Current document role

For ETKT:

```text
ETKT:{TravellerId}:1
```

The final `:1` is a current-plan discriminator and deliberately avoids baking “one ticket forever” into identity.

## 14.3 Retirement

Issued/retired numbers are never returned to stock. Never decrement NextNumber. Never recycle after errors.

## 14.4 Range safety

Enforce:

- `RangeFrom > 0`
- `RangeTo >= RangeFrom`
- `SerialWidth > 0`
- Prefix required
- no duplicate non-retired `(IssueFulfillmentTaskId, DocumentRole)`
- persisted document-number uniqueness
- overlapping active ranges for same owner + document kind + prefix rejected at stock-definition time

---

# 15. Local Document Authority profile

Add internal fulfillment provider key:

```text
LocalDocumentAuthority
```

Exact string:

```text
"LocalDocumentAuthority"
```

This is not a fake external provider; it identifies local accountable-document issuance owned by Ordering.

It produces no network ProviderInteraction.

## 15.1 Local capability semantics

```text
ExternalMutation = No
DocumentNumberAuthority = Local
ExecutionAtomicity = Ordering DB transaction
AuthoritativeReadBack = Ordering DB
SafeRetryAfterLostHTTPResponse = Yes
DuplicateDocumentEffect = prevented by IssueFulfillmentTaskId + DocumentRole + persisted documents
ProviderInteraction = None
```

## 15.2 External issuer capability report

Report:

```text
AcceptsIdempotencyIdentity = NotEstablished
DuplicateMutationSemantics = NotEstablished
SupportsAuthoritativeReadBack = NotEstablished
SupportsSafeReplayAfterAmbiguousOutcome = NotEstablished
ResultGranularity = NotEstablished
DocumentNumberAuthority = External supported by domain, execution not configured
```

Do not block Stage 4 on this. Do not implement external provider.

---

# 16. Generalize FulfillmentTask for documents

## 16.1 FulfillmentReservationId becomes nullable

Change to `long?`. Existing reservation tasks still set it. Issue tasks set null. Preserve Stage 2/3 queries.

## 16.2 New canonical target kind

Do not repurpose old public `OrderFulfillmentTargetType` blindly.

Introduce:

```text
FulfillmentTargetKind
```

with exact names:

```text
ReservationUnit = 1
OrderService = 2
ElectronicTicket = 3
TicketCoupon = 4
ElectronicMiscDocument = 5
EmdCoupon = 6
DocumentStockAllocation = 7
ReservationGroupSpace = 8
```

Do not include Payment/Refund generic targets.

## 16.3 FulfillmentTaskTarget final shape

```text
Id : long
FulfillmentTaskId : long
TargetKind : FulfillmentTargetKind
TargetId : long
Action : OrderFulfillmentTargetAction
```

Remove domain dependence on `ReservationUnitId`.

## 16.4 Existing-data migration

For every current target:

```text
TargetKind = ReservationUnit
TargetId = old ReservationUnitId
```

Backfill before dropping old column. No `TargetId=0` defaults may remain.

## 16.5 Issue task

```text
TaskType = IssueTicket
FulfillmentProviderKey = "LocalDocumentAuthority"
IdempotencyKey = $"issue-ticket:{task.Id}"
CorrelationReference = $"order:{order.Id}:issue:{task.Id}"
FulfillmentReservationId = null
```

Initial targets: every planned Air OrderService, kind `OrderService`, action `Issue`.

After local document objects exist, add `DocumentStockAllocation`, `ElectronicTicket`, and `TicketCoupon` targets with action `Issue`.

Prevent duplicate targets.

## 16.6 Local attempt

Start one attempt; create zero ProviderInteractions; on atomic success complete attempt/task Succeeded.

Do not invent an IssueTicket ProviderInteraction for a local-only effect.

---

# 17. Stage-4 IssueOrder API

Implement:

```text
POST Backoffice/v1/Orders/{orderId}/Issuance
```

Request:

```text
TicketDocumentStockId : long
```

No client idempotency key. No Payment fields. No EMD stock input.

Create `BackofficeIssueOrderCommand(OrderId, TicketDocumentStockId)` using existing Backoffice authorization.

No Internal/OTA mirror unless a real current caller exists.

## 17.1 Result

Return:

```text
OrderId
OrderStatus
IssueFulfillmentTaskId
Tickets[]
```

Ticket summary:

```text
ElectronicTicketId
TravellerId
DocumentNumber
Status
IssuedAt
IssuedTotal
CurrencyId
Coupons[]
```

Coupon summary:

```text
TicketCouponId
CouponNumber
OrderServiceId
OrderSegmentId
FinancialStatus
ControlStatus
IssuanceValue
```

---

# 18. Backoffice DocumentStock administration

Implement:

```text
POST Backoffice/v1/DocumentStocks
```

Request:

```text
OwnerAirlineId
OfficeId?
DocumentKind
Prefix
SerialWidth
CheckDigitProfile
RangeFrom
RangeTo
```

Implement:

```text
GET Backoffice/v1/DocumentStocks/{documentStockId}
```

Return root stock facts. Do not implement stock-range edit/rewrite in Stage 4.

A stock range is accountable history.

---

# 19. Issue eligibility — facts, not OrderStatus alone

Implement one Issue planning/eligibility service.

## 19.1 Commercial scope

Current ticketable scope is active `OrderAirTransportService`.

SeatAssignment is not an ETKT coupon and is not auto-EMD.

## 19.2 Already documented scope

Load existing non-void ETKT coupons and exclude already documented Air services.

Repeat Issue after full success returns existing docs with:

```text
zero new task
zero new stock allocation
zero new ticket
zero new coupon
zero AirPrice call
```

This is lost-HTTP-response/client-retry protection.

## 19.3 Capacity

For every outstanding Air service whose provider profile requires reservation, latest resource truth must be `Confirmed`.

Held/Pending/Waitlisted/Unknown/Mixed/Released/Expired/Cancelled/Rejected block.

For `ReservationMode.None`, do not fabricate reservation requirement.

For ImmediateConfirm use actual recorded confirmed truth.

## 19.4 Conflicting execution effects

Block if an overlapping current task is Pending/InProgress/Unknown for at least:

```text
ReserveInventory
ConfirmInventory
ReleaseReserved
CancelConfirmed
IssueTicket
```

Extend `IFulfillmentTaskRepository` with typed queries; do not scan all rows in memory.

## 19.5 Hard ticketing deadline

If `LastTicketingDate != null && LastTicketingDate <= now`, new Issue forbidden.

If null, no current hard deadline fact exists; do not invent one.

Recheck before allocation/commit.

## 19.6 Issue-time AirPrice validation

For confirmed reservation scopes covering Air services being issued, stale/null `ReservationValidationTimeLimit` must be refreshed with existing AirPrice reservation validation and persisted accepted fare topology.

Do not create new hold; do not alter ProviderOperationRef/ExpiresAt/Confirmed status.

AirPrice reject/reprice/unsupported => zero stock/doc/task effect.

Extend current domain validation recording narrowly so Confirmed reservation can record fresh Issue validation evidence. Do not loosen terminal states.

Recheck freshness before commit.

---

# 20. Stock selection and locking

Issue request explicitly supplies `TicketDocumentStockId` to avoid guessing issue-time stock from sale context.

Require stock exists, kind ETKT, Active, profile None, and enough serials for all planned tickets.

Do not switch to another stock automatically.

## 20.1 Lock order

Use deterministic order:

1. acquire current Order fulfillment/reservation lock for orderId;
2. acquire `document-stock:{stockId}` lock;
3. reload authoritative Order/reservation/task truth as required;
4. reload stock under stock lock;
5. JIT checks;
6. allocate/persist atomically.

Never stock-lock then order-lock elsewhere.

Issue must not commit against concurrently changed reservation truth.

---

# 21. Atomic local issuance

Use one Ordering DB transaction/UoW commit for:

- Issue FulfillmentTask + attempt
- DocumentStock allocations
- ElectronicTicket roots
- TicketCoupon entities
- TicketPriceLinks
- allocation Issued state
- Order Ticketed transition
- outbox/projection according to existing framework

Do not SaveChanges merely to reserve stock before a nonexistent external call.

If process fails before commit, nothing issued.

If commit succeeds but HTTP response is lost, retry returns same documents and allocates no new number.

There is no legitimate Unknown external issuer state in local profile.

---

# 22. Order status semantics

Successful local issuance of all required current active Air ticket scope => `OrderStatus.Ticketed`.

Do not produce Paying/Paid/PaymentFailed/PaymentUnconfirmed.

Do not use Ticketing/TicketingUnconfirmed for atomic local path.

Preflight rejection leaves prior truthful Order summary unchanged; do not force TicketingFailed for AirPrice/deadline/stock/reservation ineligibility before effect.

Late Stage-3 deadline enforcement must not Expire Ticketed Order.

---

# 23. ElectronicTicketIssued V1 event

Publish existing `ElectronicTicketIssued` V1 truthfully.

Legacy wire member:

```text
OperationId
```

maps to:

```text
IssueFulfillmentTaskId
```

Do not rename/reversion existing V1 casually.

## 23.1 OrderIssued V1

Current `OrderIssued` V1 has legacy assumptions including non-null airline office and generic TrafficDocument/DocumentCoupon pricing references; no current consumer contract proves a universal truthful mapping.

Therefore:

```text
ORDER_ISSUED_WIRE_DEFERRED_NO_CURRENT_CONSUMER_CONTRACT
```

Do not emit misleading V1 and do not invent V2 without consumer contract. Not a Stage blocker.

---

# 24. Persistence

Create mappings/repositories for:

```text
ElectronicTicket
TicketCoupon
TicketPriceLink
DocumentStock
DocumentStockAllocation
```

Use current conventions.

At minimum:

ElectronicTicket: unique DocumentNumber; indexes OriginalOrderId, CurrentServicingOrderId, TravellerId, IssueFulfillmentTaskId.

TicketCoupon: unique `(TicketId,CouponNumber)`; indexes CurrentOrderServiceId, OrderSegmentId.

TicketPriceLink: indexes PricingLineId, PricingAllocationId, TicketCouponId.

DocumentStockAllocation: unique DocumentNumber; unique `(DocumentStockId,IssueFulfillmentTaskId,DocumentRole)`; index IssueFulfillmentTaskId.

Use established monetary precision for IssuedTotal/IssuanceValue/AttributedValue; no EF default money precision.

Preserve aggregate RowVersion concurrency.

---

# 25. Database migration

Create named Stage-4 migration containing:

1. ETKT tables.
2. coupon table.
3. ticket price link table.
4. DocumentStock.
5. DocumentStockAllocation.
6. nullable FulfillmentTask.FulfillmentReservationId.
7. generalized target columns.
8. exact reservation-target backfill.
9. indexes/uniqueness.
10. no Payment tables.
11. no EMD tables unless a newly discovered current authoritative source makes EMD documentable and is explicitly reported.

Apply to local dev DB per CLAUDE.md and verify migration history.

---

# 26. Query/read model

Backoffice Order detail must expose ETKT truth.

Ticket fields:

```text
Id
TravellerId
DocumentNumber
Authority
IssuedAt
IssuedTotal
CurrencyId
StatusSummary
DocumentVersion
```

Coupon fields:

```text
Id
CouponNumber
OriginalOrderServiceId
CurrentOrderServiceId
OrderSegmentId
FareBasisSnapshot
BookingClassSnapshot
RbdIdSnapshot
CabinClassIdSnapshot
IssuanceValue
CurrencyId
FinancialStatus
ControlStatus
```

Follow current query projection conventions. Read side is not write eligibility authority.

---

# 27. EMD source gate

Re-inspect current source for RFIC/RFISC/ReasonForIssuanceCode/ReasonForIssuanceSubCode/ServiceSubCode/EMD profile/value/authority.

If no complete new authoritative contract appeared:

```text
EMD_EXECUTION = DEFERRED_SOURCE_GATED
```

Continue. Do not create empty EMD persistence just for completeness. Do not use donor fields as source truth.

---

# 28. External issuer source gate

Re-inspect current accessible AeroTech contracts for a real issuer and its operation capability matrix.

If absent:

```text
EXTERNAL_ETKT_ISSUER = DEFERRED_SOURCE_GATED
```

Continue local profile. Do not copy donor ports as live contract.

---

# 29. Exact IssueOrder algorithm

## A — lock/load
1. acquire Order fulfillment/reservation lock;
2. load Order;
3. authorize;
4. load current ETKTs;
5. derive already-documented service IDs;
6. derive outstanding active Air services;
7. if none, return existing result with no new effect.

## B — preflight
8. deadline check;
9. reservations/latest service truth;
10. confirmed-capacity checks;
11. unresolved-overlap task checks;
12. unique FareComponent per service;
13. exact pricing links;
14. immutable ticket plans;
15. load selected stock identity / validate kind-profile-status.

No allocation yet.

## C — stale validation refresh
16. group scope by confirmed reservation;
17. stale/null validation -> AirPrice exact accepted topology;
18. failure => no issue effect.

## D — JIT
19. recheck deadline/validation/reservation Confirmed;
20. stock lock/reload;
21. verify enough numbers.

## E — one atomic local issue
22. create one IssueTicket FulfillmentTask;
23. start one attempt;
24. allocate one number per ticket plan;
25. create tickets;
26. create ordered coupons;
27. create exact price links;
28. add task targets allocations/tickets/coupons;
29. mark allocations Issued;
30. task attempt Succeeded;
31. Order Ticketed only when all required current active Air scope documented;
32. outbox/query projection;
33. single SaveChanges/commit.

## F — return
34. committed IssueOrder result.

---

# 30. No fake partial local issuance

Plan all current tickets before mutation and commit all local ETKTs atomically in one DB transaction.

Do not separately commit traveller 1 then traveller 2.

Future external issuer partial result semantics are deferred until a real contract exists.

---

# 31. Error behavior

Use ExceptionFactory and next unused Ordering codes; do not reuse retired codes.

Add precise errors for:

- deadline passed;
- no ticketable services;
- reservation missing/not Confirmed/Unknown/Mixed;
- unresolved conflicting effect;
- fare component missing/ambiguous;
- ticket pricing attribution missing/ambiguous;
- currency mismatch;
- stock not found/wrong kind/not active/exhausted/insufficient;
- unsupported check-digit profile;
- stock range overlap;
- duplicate document invariant;
- invalid stock range;
- negative coupon issue value.

404 = missing resource; 409 = state/conflict; 422 = business/source completeness; 500 = impossible persisted invariant. Do not turn normal business ineligibility into 500.

Report exact codes.

---

# 32. Backward compatibility

Do not remove/renumber existing public OrderStatus, task/interaction enums, ticket/coupon/document/EMD enums.

Do not activate Payment statuses. Do not delete EMD contracts. Do not rename existing V1 event fields.

---

# 33. Mandatory domain tests

ETKT:
D01 requires coupon; D02 document number required; D03 coupon numbers unique; D04 order preserved; D05 totals reconcile; D06 starts Issued; D07 version=1; D08 snapshot immutable; D09 coupon financial Open; D10 local control Local.

DocumentStock:
D11 valid definition; D12 invalid range; D13 unsupported profile; D14 first allocation RangeFrom; D15 next role next serial; D16 same task-role same allocation; D17 exhaustion; D18 Issued never recycled; D19 Retired never recycled; D20 overlap rejected.

FulfillmentTask:
D21 reservation compatibility; D22 document task null reservation id; D23 typed kind/id/action; D24 duplicate target rejected; D25 local issue succeeds with attempt and zero interactions.

---

# 34. Mandatory acceptance/application tests

A01 one ADT one flight end-to-end -> ETKT/coupon/Ticketed.
A02 one traveller two segments -> one ETKT ordered coupons.
A03 two travellers -> two ETKTs independent numbers.
A04 adult + lap INF -> both ETKTs, seat consumption unchanged.
A05 repeat Issue -> same docs, no new task/number.
A06 lost caller response retry -> no duplicate.
A07 Held blocks.
A08 Unknown blocks.
A09 Mixed blocks.
A10 Released/Expired/Cancelled block.
A11 generic ReservationMode.None eligibility path does not fabricate reservation (do not invent production provider behavior).
A12 stale validation calls AirPrice before allocation.
A13 AirPrice reject -> zero issue effect.
A14 unsupported topology -> zero issue effect.
A15 passed LTD -> zero effect.
A16 null LTD -> no invented deadline.
A17 stock missing -> zero effect.
A18 wrong kind -> zero effect.
A19 exhausted -> zero effect.
A20 insufficient stock for multi-traveller -> zero committed ticket.
A21 price links exact accepted allocations.
A22 tax/fee/surcharge code/reference retained via PricingLine linkage.
A23 order-scoped unallocated fee not guessed onto first ticket.
A24 fare/RBD/cabin snapshots exact.
A25 checked baggage exact, cabin baggage not merged.
A26 unique FareComponent linked.
A27 missing/ambiguous FareComponent blocks before allocation.
A28 unresolved Confirm blocks.
A29 unresolved Release blocks.
A30 completed task does not block.
A31 issue task type/provider/targets/succeeded/zero interactions.
A32 old reservation target migrated to ReservationUnit.
A33 late deadline cannot Expire Ticketed.
A34 zero FlightFlow Issue calls.
A35 zero Payment/JetPay calls.
A36 SeatAssignment does not create EMD.
A37 no RFIC/RFISC synthesis.
A38 Backoffice Issue response truthful.
A39 stock define/read works.
A40 overlap rejected.

---

# 35. Concurrency tests

C01 concurrent Issue same Order -> one document set/no duplicate.
C02 concurrent different Orders same stock -> distinct serials/correct NextNumber.
C03 Issue races Reserve/Release/Confirm -> shared lock prevents stale issue.
C04 stock exhausts between plan/lock -> no partial issue.
C05 optimistic concurrency failure -> no duplicate persisted number/document.

---

# 36. Persistence/integration tests

P01 target migration exact.
P02 Stage2/3 reservation suites pass.
P03 ETKT/coupon/link round-trip.
P04 stock/allocation round-trip.
P05 document number uniqueness.
P06 coupon number uniqueness.
P07 ticket in Backoffice Order detail.
P08 ElectronicTicketIssued outbox same transaction.
P09 V1 OperationId == IssueFulfillmentTaskId.
P10 no fabricated OrderIssued V1.

---

# 37. Sabotage tests

Temporarily sabotage then restore production code. Prove failure for at least:

SAB01 Issue on Held; SAB02 OrderStatus-only eligibility; SAB03 skip stale AirPrice refresh; SAB04 stock allocation before validation; SAB05 random ticket number; SAB06 recycle number; SAB07 duplicate number on retry; SAB08 omit fare topology; SAB09 guess order fee allocation; SAB10 synthesize RFIC/EMD; SAB11 FlightFlow call on Issue; SAB12 Payment gate; SAB13 wrong old target migration; SAB14 sale actor used as issue actor; SAB15 concurrent same number; SAB16 expire Ticketed after LTD.

Report sabotage totals separately.

---

# 38. Regression suite

Run relevant Stage1-3 tests including Create, pricing/fare topology, AirFareReservationValidator, Reserve, Confirm, recovery, Release, deadline, CabinClass, FulfillmentTask/ProviderInteraction, Order query/projection.

Do not weaken existing tests.

---

# 39. Build/database evidence

Run build and all supported tests. If infrastructure blocks:

```text
ENVIRONMENT_BLOCKED: <suite> — <exact reason>
```

Do not claim unexecuted tests passed.

Apply migration to configured dev DB and verify migration history.

---

# 40. Expected production areas

Expected additions/changes include:

```text
Contracts/AeroTech.Messages/Ordering/Enums/FulfillmentTargetKind.cs
src/AeroTech.Ordering.Domain/ElectronicTicketAggregate/
src/AeroTech.Ordering.Domain/DocumentStockAggregate/
src/AeroTech.Ordering.Domain/FulfillmentTaskAggregate/
src/AeroTech.Ordering.Application/OrderAggregate/Commands/IssueOrder/
src/AeroTech.Ordering.Application/OrderAggregate/Services/Issuance/
src/AeroTech.Ordering.Application/DocumentStockAggregate/
src/AeroTech.Ordering.Persistence/ElectronicTicketAggregate/
src/AeroTech.Ordering.Persistence/DocumentStockAggregate/
src/AeroTech.Ordering.Persistence/FulfillmentTaskAggregate/
src/AeroTech.Ordering.Persistence/Migrations/
src/AeroTech.Ordering.Query/...
src/AeroTech.Ordering.Synchronizer/... if required by current projection pattern
src/AeroTech.Ordering.RestApi/V1/...
tests/...
```

Adjust physical placement only to current conventions; do not change semantics.

---

# 41. Donor dispositions

If inspecting `aliifarhadi/Ordering@k8s-stg`:

KEEP AS IMPLEMENTATION EVIDENCE:
- separate ETKT/DocumentStock direction;
- per-traveller planning;
- ordered coupons;
- price links;
- sequential stock;
- persistence/query patterns.

REDESIGN TO MASTER:
- OperationId -> IssueFulfillmentTaskId;
- generic servicing Operation -> FulfillmentTask;
- target model -> typed FulfillmentTargetKind/TargetId;
- issuance context -> Master shape;
- current Final repo field names/topology.

DO NOT PORT:
- FundingCoverage/Payment gate;
- generic TrafficDocument;
- random ticket generator;
- client Issue idempotency key;
- fake deterministic external issuer;
- old generic Operation aggregate;
- EMD behavior without current source.

---

# 42. Definition of done

Stage 4 local issuance is complete only if:

> Given an accepted current Order with active Air services, truthful Confirmed capacity where reservation is required, an open applicable ticketing deadline, current AirPrice validation and configured local ETKT stock, Ordering atomically issues accountable ElectronicTickets with ordered immutable coupons and exact accepted pricing/fare/segment snapshots, persists a stable Issue FulfillmentTask and document-number allocation, exposes the documents on the read side and publishes truthful ticket-issued evidence; retries never create duplicate documents, and no Payment, external issuer, EMD, FlightFlow ticketing mutation, DCS, refund or exchange behavior is fabricated.

---

# 43. Mandatory completion report

Return exactly:

```text
STAGE 4 IMPLEMENTATION REPORT

SOURCE
Ordering before HEAD:
Ordering after HEAD:
FlightFlow reviewed HEAD:
unexpected source drift:

AUTHORITY
Master:
Stage-4 authority path:
CLAUDE.md updated:

BENCHMARK DISPOSITIONS
Reservation vs ticket separation:
Coupon-level truth:
Local vs external document authority:
EMD source gate:
FlightFlow ticketing mutation:

FULFILLMENT TASK GENERALIZATION
FulfillmentReservationId nullable:
FulfillmentTargetKind values:
existing target migration:
Issue task identity:
Issue targets:

DOCUMENT STOCK
aggregate fields:
allocation fields:
check-digit profile:
range overlap protection:
concurrency:
migration/database result:

ETKT
root fields:
coupon fields:
segment snapshot:
price-link mapping:
fare-component linkage:
traveller profile linkage:
issuance context mapping:

ISSUE ELIGIBILITY
commercial scope:
reservation truth:
unresolved effects:
LastTicketingDate:
AirPrice refresh:
JIT checks:

ISSUE EXECUTION
endpoint:
local authority:
atomicity:
repeat/lost-response behavior:
OrderStatus result:
FlightFlow calls:
Payment/JetPay calls:

WIRE/QUERY
ElectronicTicketIssued V1:
OrderIssued V1 disposition:
Order detail projection:

SOURCE-GATED DEFERRALS
EMD_EXECUTION:
EXTERNAL_ETKT_ISSUER:
IssueServices targeted partial issuance:
other:

TESTS
domain:
acceptance:
concurrency:
persistence:
Stage1-3 regression:
sabotage:
environment-blocked:

SCOPE CHECK
Payment/JetPay added: NO
external issuer invented: NO
EMD invented: NO
FlightFlow Issue mutation added: NO
Void/Refund/Exchange added: NO
DCS added: NO

FINAL
STAGE4_READY_FOR_FINAL_REVIEW
```

If and only if the local-authority ETKT path is impossible because of an actual current source contradiction:

```text
BLOCKED_SOURCE: <exact contradiction>
```

Do not use BLOCKED_SOURCE for intentionally deferred external issuer or EMD profile.

Do not start Stage 5.
