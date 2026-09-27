# AeroTech Ordering — Master Airline Domain ADR/PRD v1.0 FINAL CANDIDATE

**Purpose:** Full-horizon domain authority for AeroTech Airline Ordering / PSS  
**Target:** `aliifarhadi/AeroTech.Ordering.Final`  
**Reviewed source HEAD:** `3e130e0cb215fb7dd12d327ada86064527787ff2` (`k8s-stg`)  
**Implementation policy:** Full domain is designed now; code is materialized stage-by-stage only.  
**Owner approval:** Required before this document supersedes conflicting domain decisions in Pack v4.6.

---

## 0. What this document is — and is not

This is the single full-horizon **domain ADR + PRD** for Airline Ordering. It defines:

- business ownership;
- aggregate/entity/value semantics;
- field-level canonical shape;
- lifecycle and invariants;
- commands and end-to-end flows;
- scenario coverage;
- source/benchmark provenance;
- what must remain source-gated;
- what each future Stage is allowed to materialize.

It deliberately does **not** define:

- folders or namespaces;
- EF configuration/table names/index names;
- DI/Mediator/controller structure;
- repository implementation;
- migration mechanics;
- framework architecture.

The Coding Agent may decide those details from the repository conventions. It may **not** decide missing domain fields, lifecycle semantics, ownership, cardinality, or business rules.

### Implementation rule

> Full shape is known in advance; only the fields/types needed by the current Stage are materialized in code. A future field may be added when its Stage arrives, but aggregate ownership and semantic role must not be reinvented.

### Source-gated rule

A future field is listed here because the airline domain requires a place for that fact. It must remain null/unmaterialized until an actual AeroTech source contract provides the fact. No donor repo or vendor behavior is permission to fabricate data.

---

# 1. Authority and benchmark hierarchy

For a Stage decision use this order:

1. Canonical AeroTech business contract / real provider contract.
2. This Master Domain ADR/PRD.
3. Stage-specific approved PRD/ADR derived from this Master.
4. IATA public standards: Offers & Orders / ONE Order / AIDM / Reservations / Ticketing / Revenue Accounting.
5. ATPCO public fare and Optional Services semantics.
6. Public Amadeus Altéa / Nevio business behavior.
7. Public SabreSonic / SabreMosaic business behavior.
8. v1/v2/v3 donor code only as implementation/domain evidence.

No proprietary Amadeus/Sabre internal aggregate shape is inferred.

## 1.1 Benchmark conclusions frozen by this document

- IATA ONE Order treats Order as the integrated customer retail record coordinating fulfillment, delivery and accounting; it does not require Payment, DCS or Revenue Accounting to become children of one giant aggregate.
- IATA AIDM defines an ordered Service as a concrete occurrence associated to a passenger and, for flight service, a segment. Offered compound services can split into passenger × segment service occurrences at Order creation.
- Service delivery is a lifecycle distinct from commercial status and reservation state.
- ATPCO fare construction and rule authority is not Ordering ownership. Pricing Unit / Fare Component topology must be preserved when supplied, while rule calculation remains AirPrice/ATPCO-pricing authority.
- ATPCO Category 5 distinguishes reservation restrictions from ticketing deadlines. A provider hold expiry, a pricing/validation validity and a ticketing deadline are not one timestamp.
- ATPCO Categories 31/33 are authoritative inputs for voluntary exchange/refund calculation; sale-time `IsRefundable` / `IsChangeable` are snapshots, not final servicing authority.
- IATA EMD-A and EMD-S remain separate document semantics; ancillary delivery/revenue history cannot be collapsed into ETKT.
- Amadeus and Sabre both publicly separate retail Order from Payment/Settlement and Delivery/DCS capabilities while integrating them end-to-end.
- Group booking is a first-class airline scenario with group identity, intended passenger quantity, block/capacity management, deadlines, names, payment and ticketing.
- Charter is a transport/contract context, not a reason to create a second parallel Ordering domain.

---

# 2. Canonical ownership map

| Business truth | Canonical owner in AeroTech | Ordering representation |
|---|---|---|
| Accepted commercial composition | Ordering | `Order`, `OrderItem`, `OrderService` |
| Accepted sold itinerary snapshot | Ordering | `OrderJourney`, `OrderSegment`, `OrderSegmentLeg` |
| Accepted price/history | Ordering | `PricingLine`, `PricingAllocation`, fare topology |
| Fare/rule calculation | AirPrice / pricing authority | accepted decision refs + immutable result facts only |
| Seat/capacity resource state | FlightFlow / supplier | `FulfillmentReservation` evidence |
| Provider external-effect execution/recovery | Ordering | `FulfillmentTask` + attempts/interactions |
| Payment transaction lifecycle | JetPay / checkout orchestrator | `OrderPaymentCoverage` evidence only |
| ETKT / coupon financial-control history | Ordering | `ElectronicTicket` |
| EMD / coupon association/value history | Ordering | `ElectronicMiscDocument` |
| Document number stock | Ordering / issuer | `DocumentStock` |
| Check-in/boarding/operational consumption | DCS / delivery provider | sourced delivery observations only |
| Flight disruption operational fact | Operations / DCS / disruption owner | `OrderDisruptionImpact` evidence only |
| Automated refund/exchange pricing | AirPrice servicing | quote/decision provenance + committed pricing/document history |
| Revenue recognition / accounting ledger | Revenue Accounting | facts emitted from Order/Documents/Payment/Delivery; no RA ledger in Ordering |
| Interline clearing/proration | Revenue Accounting / settlement authority | source-backed settlement/internal-value facts only |

---

# 3. Aggregate roots — final set

The final domain has exactly these business aggregate roots:

1. `Order`
2. `FulfillmentReservation`
3. `FulfillmentTask`
4. `ElectronicTicket`
5. `ElectronicMiscDocument`
6. `DocumentStock`

The following are **not** aggregate roots:

- Payment
- Traveller
- PassengerGroup
- OrderItem
- OrderService
- Pricing
- Refund
- Exchange
- Reissue
- FareConstruction
- FarePricingUnit
- ProviderInteraction
- DCSDelivery
- Disruption
- RevenueAccounting

This is intentionally simpler than v2/v3 while retaining the business truths they proved necessary.

---

# 4. End-to-end airline lifecycle

```text
Offer / Group Offer / Charter Contract
        ↓
Create Order
  commercial services + passengers/groups + sold itinerary + accepted pricing
        ↓
Reserve
  provider operations / PNR / capacity evidence
        ↓
Checkout / Payment Coverage
        ↓
Confirm held capacity (when provider requires HoldThenConfirm)
        ↓
Issue
  ETKT + EMD + stock / external issuer evidence
        ↓
Pre-trip servicing
  add ancillary / seat / baggage / correction / split
        ↓
Delivery / DCS
  check-in → boarding → delivery/consumption/no-show evidence
        ↓
Servicing at any eligible point
  cancel / void / refund / exchange / reissue / revalidate
        ↓
Disruption
  impact → protection/reaccommodation → document/financial servicing
        ↓
Revenue Accounting / settlement consumers
  immutable sale + document + payment + delivery facts
```

`OrderStatus` is a useful summary, not the sole source of eligibility. Eligibility is derived from commercial service state + reservation state + payment evidence + document state + delivery/control state + current authoritative servicing decision.

---

# 5. Field notation

- **NOW**: already implemented at reviewed HEAD.
- **FUTURE**: part of final domain, materialize only when its Stage arrives.
- **SOURCE-GATED**: final place is frozen, but value may only exist if a real source supplies it.
- `?` means nullable.
- Internal entity IDs use the current repository convention (`long`) unless reference data already uses `int`.

---

# 6. Aggregate: Order

## 6.1 `Order` root

| Field | Type | Null | Stage | Meaning / invariant |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Internal identity. |
| `OrderReference` | `Guid` | No | NOW | Stable commercial Order reference. Never reuse provider refs as this value. |
| `OwnerAirlineId` | `int` | No | FUTURE | Airline owning the retail Order. Required for multi-carrier/interline/accounting. Must come from airline/tenant context, never inferred from first segment. |
| `CustomerId` | `long` | No | NOW | Financial/commercial customer identity; not necessarily traveller. |
| `SalesContext` | `SalesContext` | No | NOW | Accepted seller/channel/actor context snapshot. |
| `CurrencyId` | `int` | No | NOW | Order customer-total currency. |
| `SourceOfferId` | `string?` | Yes | NOW | Accepted source Offer reference. Initial sale normally requires it; later non-offer creation models may differ. |
| `RecordLocator` | `string?` | Yes | NOW | AeroTech host PNR/RLOC. Generated once on first positive air reservation; immutable and never reused as supplier operation ref. |
| `LastTicketingDate` | `DateTimeOffset?` | Yes | NOW | Accepted ticketing-deadline fact currently supplied by sale/pricing. It is **not** reservation expiry or universal Order TTL. Later canonicalized through `OrderTimeLimit`. |
| `Status` | `OrderStatus` | No | NOW | Derived business summary. Partial eligibility must not be decided only from this field. |
| `CommercialVersion` | `int` | No | NOW | Increments once per committed commercial composition/pricing change; no increment for pure provider observations. |
| `CustomerTotal` | `decimal` | No | NOW | Reconciled customer-facing accepted total. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Order creation time. |

### Root collections — final

- `Items`
- `Services`
- `Travellers`
- `PassengerGroups`
- `Contacts`
- `Journeys`
- `Segments`
- `FarePricingUnits`
- `PricingLines`
- `Changes`
- `Remarks`
- `TimeLimits`
- `PaymentCoverages`
- `SpecialServiceRequests`
- `DeliveryObservations`
- `DisruptionImpacts`
- `OrderLineage`

Only Stage-relevant collections are materialized now.

### Order invariants

1. The Order preserves accepted commercial history; servicing does not rewrite old sale facts.
2. Every active named `OrderService` has exactly one traveller occurrence.
3. Provider reservation/document/payment/delivery truth is not stored as mutable flags on `OrderService`.
4. `CommercialVersion` changes only with accepted commercial changes.
5. `RecordLocator` is host PNR, not FlightFlow HoldId or supplier booking reference.
6. No provider expiry is stored as `Order.TimeToLive`; there is no universal Order TTL.

---

## 6.2 `SalesContext`

Current fields remain:

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Channel` | `SalesChannel` | No | NOW |
| `ContextType` | `CallerContextType` | No | NOW |
| `PrincipalType` | `CallerPrincipalType` | No | NOW |
| `ActorId` | `long` | No | NOW |
| `AirlineUserId` | `long?` | Yes | NOW |
| `TravelAgencyId` | `long?` | Yes | NOW |
| `TravelAgencyUserId` | `long?` | Yes | NOW |
| `IndividualId` | `long?` | Yes | NOW |
| `PartnerApiAccessProfileId` | `long?` | Yes | NOW |
| `OfficeKind` | `SellingOfficeKind?` | Yes | NOW |
| `OfficeId` | `long?` | Yes | NOW |
| `CorporateAccountId` | `long?` | Yes | FUTURE / SOURCE-GATED |
| `PartnerOrganizationId` | `long?` | Yes | FUTURE / SOURCE-GATED |
| `AgencyIataNumber` | `string?` | Yes | FUTURE / SOURCE-GATED |
| `Pcc` | `string?` | Yes | FUTURE / SOURCE-GATED |
| `PointOfSaleCountryId` | `int?` | Yes | FUTURE / SOURCE-GATED |

`AgencyIataNumber`/`PCC` are retained only when the selling/customer source supplies them. They must not be synthesized from OfficeId.

---

## 6.3 `OrderItem`

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Item identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `Kind` | `ProductType` | No | NOW | Commercial product kind. |
| `SourceOfferItemRef` | `string?` | Yes | FUTURE / SOURCE-GATED | IATA-style source OfferItem traceability when upstream exposes it. |
| `PassengerGroupId` | `long?` | Yes | FUTURE | Links group-priced/group-capacity item to PassengerGroup. |
| `AcceptedTotal` | `decimal` | No | NOW | Reconciled accepted customer value attributed to item. |
| `CommercialStatus` | `OrderItemCommercialState` | No | NOW | `Active/Replaced/Cancelled/Transferred`. |
| `CreatedByChangeId` | `long` | No | NOW | Commercial origin. |
| `EndedByChangeId` | `long?` | Yes | FUTURE | Change that replaced/cancelled/transferred this occurrence. |
| `PredecessorOrderItemId` | `long?` | Yes | FUTURE | Commercial lineage for exchange/reaccommodation/split. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Historical creation time. |

A group/closed-charter commercial item may exist before individual traveller services are materialized.

---

## 6.4 Abstract `OrderService`

IATA benchmark: at Order time a Service occurrence should be passenger-specific and, for air service, segment-specific. AeroTech keeps that grain.

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Stable service occurrence identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `OrderItemId` | `long` | No | NOW | Commercial item. |
| `TravellerId` | `long` | No | NOW | Exactly one beneficiary occurrence. Multi-beneficiary supplier units are represented by ReservationUnit covering multiple service IDs, not by a generic beneficiary graph. |
| `ServiceType` | `OrderServiceType` | No | NOW | Concrete service category. |
| `SourceServiceRef` | `string?` | Yes | FUTURE / SOURCE-GATED | Upstream Offer/Service identity if supplied. |
| `FulfillmentProviderKey` | `string` | No | NOW | Provider/adapter responsible for reservation/fulfillment orchestration of this occurrence. Immutable for this occurrence. |
| `ResponsibleAirlineId` | `int?` | Yes | FUTURE / SOURCE-GATED | IATA Responsible Airline when applicable. |
| `ValidatingCarrierId` | `int?` | Yes | FUTURE / SOURCE-GATED | IATA validating carrier when source provides it. |
| `DeliveryProviderReference` | `string?` | Yes | FUTURE / SOURCE-GATED | Business delivery-provider reference; distinct from technical provider key. |
| `AccountingSnapshot` | `ServiceAccountingSnapshot?` | Yes | FUTURE / SOURCE-GATED | Internal/settlement values needed by accounting, only if source provides. |
| `CommercialStatus` | `OrderServiceCommercialState` | No | NOW | Active/Replaced/Cancelled/Transferred. |
| `CreatedByChangeId` | `long` | No | NOW | Change creating this service occurrence. |
| `EndedByChangeId` | `long?` | Yes | FUTURE | Change closing/replacing it. |
| `PredecessorOrderServiceId` | `long?` | Yes | FUTURE | Successor lineage for exchange/reaccommodation/split. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Historical timestamp. |

`OrderService` never owns reservation state, payment state, ticket/EMD state or DCS delivery state.

---

## 6.5 `OrderAirTransportService`

| Field | Type | Null | Stage |
|---|---|---:|---|
| `SegmentId` | `long` | No | NOW |
| `FlightCapacityId` | `long` | No | NOW |
| `AirFareId` | `long?` | Yes | NOW |
| `BookingClass` | `string?` | Yes | NOW |
| `FareBasis` | `string?` | Yes | NOW |
| `FareFamily` | `string?` | Yes | NOW |
| `FareType` | `string?` | Yes | NOW |
| `RbdId` | `long?` | Yes | NOW |
| `CabinClassId` | `long?` | Yes | FUTURE / SOURCE-GATED |
| `CheckedBaggageAllowance` | `BaggageAllowance?` | Yes | NOW |
| `CabinBaggageAllowance` | `BaggageAllowance?` | Yes | NOW |
| `IsRefundable` | `bool` | No | NOW |
| `IsChangeable` | `bool` | No | NOW |
| `IsUpgradable` | `bool` | No | NOW |

The three boolean terms are accepted-sale snapshots only. Refund/exchange authority comes from the current AirPrice servicing decision.

`CabinClassId` is required in the final shape because current Offer wire already carries cabin class and ETKT/DCS/RA scenarios need the sold cabin snapshot. Do not infer it when absent.

---

## 6.6 `OrderSeatService`

| Field | Type | Null | Stage |
|---|---|---:|---|
| `SegmentId` | `long` | No | NOW |
| `AssociatedAirServiceId` | `long` | No | NOW |
| `SeatNumber` | `string` | No | NOW |

The sold/selected seat remains commercial history. A later DCS operational seat change is a delivery observation and does not overwrite `SeatNumber`.

---

## 6.7 `OrderAncillaryService` — future airline optional service

Materialize in Ancillary Stage, not now.

| Field | Type | Null | Meaning |
|---|---|---:|---|
| base `OrderService` fields | — | — | One passenger occurrence. |
| `ServiceDefinitionRef` | `string?` | Yes | Airline/offer service-definition identity. |
| `ServiceSubCode` | `string?` | Yes | ATPCO/airline optional-service sub-code when provided. |
| `ServiceTypeCode` | `string?` | Yes | ATPCO service type (flight/ticket/merchandise/reissue-refund/etc.) when supplied. |
| `GroupCode` | `string?` | Yes | Optional Services group code. |
| `SubGroupCode` | `string?` | Yes | Optional Services subgroup. |
| `CommercialName` | `string?` | Yes | Source commercial description. |
| `SsrCode` | `string?` | Yes | Associated SSR code if service definition supplies it. |
| `Quantity` | `decimal` | No | Purchased quantity; default source semantics, not guessed. |
| `UnitCode` | `string?` | Yes | Unit if quantity is not count. |
| `CoveredAirServiceIds` | `IReadOnlyCollection<long>` | No | 0..N air-service coverage; use only for service types that genuinely span more than one segment. |
| `Refundability` | `RefundabilityRule?` | Yes | Accepted optional-service term snapshot. |
| `Reusable` | `bool?` | Yes | ATPCO/source term when supplied. |
| `FormOfRefundCode` | `string?` | Yes | Source disposition rule, e.g. original payment/e-voucher. |
| `Commissionable` | `bool?` | Yes | Source Optional Services settlement term. |
| `InterlineSettlementAllowed` | `bool?` | Yes | Source Optional Services settlement term. |

No ATPCO field is populated from a catalog guess. Source data is mandatory.

---

## 6.8 `OrderBaggageService` — future specialization

Baggage allowance already exists on Air service. This entity is for purchased/chargeable baggage occurrence.

| Field | Type | Null |
|---|---|---:|
| ancillary fields | — | — |
| `Pieces` | `int?` | Yes |
| `Weight` | `decimal?` | Yes |
| `WeightUnit` | `WeightUnit?` | Yes |
| `BaggageCategoryCode` | `string?` | Yes |
| `PrepaidIndicator` | `bool?` | Yes |

At least one source-backed quantity descriptor must exist for a materialized baggage product.

---

## 6.9 `OrderPartnerService` — hotel/ground/insurance/third-party future service

| Field | Type | Null |
|---|---|---:|
| base `OrderService` fields | — | — |
| `SupplierProductReference` | `string` | No |
| `ServiceDefinitionRef` | `string?` | Yes |
| `StartAt` | `DateTimeOffset?` | Yes |
| `EndAt` | `DateTimeOffset?` | Yes |
| `StartLocationRef` | `string?` | Yes |
| `EndLocationRef` | `string?` | Yes |
| `Quantity` | `decimal` | No |
| `UnitCode` | `string?` | Yes |
| `AssociatedAirServiceIds` | `IReadOnlyCollection<long>` | No |

A hotel room covering two travellers is represented by passenger-specific service occurrences that may be covered by one supplier ReservationUnit; do not introduce a generic many-beneficiary service graph.

---

## 6.10 `OrderTraveller`

Current fields remain:

`Id, OrderId, Index, SourceTravellerRef?, PassengerType, AgeRange, InfantParentTravellerId?, CurrentProfileRevisionId, Status`.

Future additions:

| Field | Type | Null | Purpose |
|---|---|---:|---|
| `PassengerGroupId` | `long?` | Yes | Named member of a group. |
| `SourcePassengerRef` | existing `SourceTravellerRef` | Yes | Preserve upstream passenger identity. |

No ticket/document uses mutable current name as historical truth; issued documents snapshot the relevant profile revision/issuance facts.

### `TravellerProfileRevision`

Keep current immutable revision fields:
`TravellerId, GivenName, Surname?, NoSurname, DateOfBirth?, Gender?, NationalityId?, CountryOfResidenceId?, CreatedByChangeId, SupersededByChangeId?, CreatedAt`.

### `OrderTravellerDocument`

Current:
`OrderTravellerId, Type, Number, ExpiryDate?, IssuanceCountryId, Holder`.

Future/source-gated additions only when API source supplies them:
`IssueDate?`, `ApplicableCountryId?`, `SourceDocumentRef?`.

### `OrderTravellerLoyaltyAccount` — future

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderTravellerId` | `long` | No |
| `ProgramOwnerAirlineId` | `int?` | Yes |
| `ProgramCode` | `string` | No |
| `MemberNumber` | `string` | No |
| `TierCode` | `string?` | Yes |
| `SourceReference` | `string?` | Yes |

---

## 6.11 Contacts and remarks

Current `OrderContact` and `OrderContactPoint` shape is retained.

Current `OrderRemark` is retained as append/supersede history. Future document-scoped annotations should not overload OrderRemark; document servicing records carry document reasons/provenance.

---

## 6.12 `OrderJourney`, `OrderSegment`, `OrderSegmentLeg`

Current sold itinerary fields remain immutable.

### `OrderJourney`
`Id, OrderId, Sequence, BoundId, OriginAirportId, DestinationAirportId`.

### `OrderSegment`
Current:
`Id, OrderId, OrderJourneyId, Sequence, FlightId, FlightVersion, FlightNumber?, OriginAirportId, OriginAirportTerminalId?, DestinationAirportId, DestinationAirportTerminalId?, OperatingAirlineId, MarketingAirlineId, SoldDeparture, SoldArrival, Duration, AircraftId?`.

Future/source-gated:

| Field | Type | Null | Meaning |
|---|---|---:|---|
| `OperationType` | `FlightOperationType` | No | `Scheduled`, `OpenCharter`, `ClosedCharter`, `Other`; sourced from flight/offer operation context. |
| `SourceSegmentRef` | `string?` | Yes | External dated-segment identity if supplied. |

Do not overwrite `SoldDeparture`, carrier or flight facts after schedule disruption. Operational/current facts are delivery/disruption evidence; accepted reaccommodation creates successor service/segment facts.

### `FlightOperationType`

```text
Scheduled
OpenCharter
ClosedCharter
Other
```

Open/Closed charter semantics follow IATA charter definitions; `Other` requires source code retained separately if needed.

---

## 6.13 `OrderSpecialServiceRequest` — future SSR/OSI business record

SSR is not automatically a paid Service.

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `TravellerId` | `long?` | Yes |
| `CoveredSegmentIds` | `IReadOnlyCollection<long>` | No |
| `Code` | `string` | No |
| `Text` | `string?` | Yes |
| `Status` | `SpecialServiceRequestStatus` | No |
| `ProviderReference` | `string?` | Yes |
| `RawStatusCode` | `string?` | Yes |
| `CreatedByChangeId` | `long` | No |
| `CreatedAt` | `DateTimeOffset` | No |

Statuses: `Requested, Confirmed, Rejected, Cancelled, Unknown`. Provider-specific codes remain raw evidence.


---

# 7. Fare topology and accepted pricing

## 7.1 Why topology exists but `FareConstruction` does not

Ordering must distinguish:

```text
Pricing Unit #1 = RoundTrip
  Fare Component A -> outbound
  Fare Component B -> inbound
```

from:

```text
Pricing Unit #1 = OneWay -> outbound
Pricing Unit #2 = OneWay -> inbound
```

because those structures are not interchangeable for fare validation/servicing. `AirFareId` on `OrderAirTransportService` alone cannot preserve that grouping.

However Ordering is not a fare-construction engine. ATPCO/AirPrice owns fare rule calculation. Therefore there is no `FareConstruction` root/entity hierarchy.

## 7.2 `OrderFarePricingUnit`

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Local accepted occurrence identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `CreatedByChangeId` | `long` | No | NOW | Commercial change producing topology. |
| `Sequence` | `int` | No | NOW | Source occurrence order. |
| `SourceKind` | `string` | No | MASTER CORRECTION | Exact source vocabulary; never replace a real value with invented `ThroughOneWay/SectorSum` semantics. |
| `SemanticType` | `FarePricingUnitType` | No | MASTER CORRECTION | `Unspecified, OneWay, RoundTrip, OpenJaw, CircleTrip, Other`. Populate a non-Unspecified value only when source/contract semantics support it. |
| `SourcePricingUnitRef` | `string?` | Yes | FUTURE / SOURCE-GATED | Source identity if later supplied. |
| `CoveredJourneyIds` | `IReadOnlyCollection<long>` | No | NOW | Exact accepted journey coverage. |
| `FareComponents` | collection | No | NOW | Source fare-component occurrences. |

### Critical source rule

The current Ordering enum values `ThroughOneWay` and `SectorSum` are **not accepted as canonical industry semantics** merely because synthetic tests used them. Real AirOffer source vocabulary must be preserved verbatim. If a source term cannot be mapped to `FarePricingUnitType` with authority, set semantic type to `Unspecified`/`Other` and retain `SourceKind`.

## 7.3 `OrderFareComponent`

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | NOW |
| `OrderFarePricingUnitId` | `long` | No | NOW |
| `Sequence` | `int` | No | NOW |
| `SourceComponentRef` | `string?` | Yes | FUTURE / SOURCE-GATED |
| `AirFareId` | `long` | No | NOW |
| `BookingClass` | `string?` | Yes | NOW |
| `FareBasis` | `string?` | Yes | NOW |
| `FareFamily` | `string?` | Yes | NOW |
| `FareType` | `string?` | Yes | NOW |
| `CoveredOrderServiceIds` | `IReadOnlyCollection<long>` | No | NOW |

Fare Component is an occurrence, not the AirFare master identity. Two components using the same `AirFareId` remain distinct occurrences when source evidence distinguishes them.

No tariff/rule/routing field is invented. Add such fields later only when an actual accepted AirPrice/AirOffer contract supplies them.

---

# 8. Pricing ledger and Revenue-Accounting-preserving facts

## 8.1 `PricingLine`

Current fields are retained:

`Id, OrderId, CreatedByChangeId, Reason, Scope, Category, SubCategory, Direction, Treatment, Code, Description, Reference, Amount, CurrencyId, EquivalentAmount, EquivalentCurrencyId, ExchangeRateSnapshot?, Refundability?, CreatedAt, Allocations`.

Final source-backed additions:

| Field | Type | Null | Meaning |
|---|---|---:|---|
| `OwnerCarrierId` | `int?` | Yes | Carrier owning fare/surcharge/fee when source explicitly supplies it. |
| `TaxJurisdiction` | `TaxJurisdictionSnapshot?` | Yes | Country/station attribution for tax line. |
| `CommissionDetail` | `CommissionSnapshot?` | Yes | Recipient/rate/basis when source supplies agency/corporate/partner commission data. |
| `SourceAccountingReference` | `string?` | Yes | External accounting/settlement reference when supplied. |

`Code` and `Reference` remain the canonical generic source code/reference. For tax, this is where source tax code/reference is retained; `TaxJurisdiction` only adds typed country/station facts.

### `TaxJurisdictionSnapshot`

| Field | Type | Null |
|---|---|---:|
| `CountryId` | `int?` | Yes |
| `StationId` | `int?` | Yes |

At least one jurisdiction fact must be supplied for the VO to exist.

### `CommissionSnapshot`

| Field | Type | Null |
|---|---|---:|
| `RecipientType` | `CommissionRecipientType` | No |
| `RecipientReference` | `string` | No |
| `Rate` | `decimal?` | Yes |
| `BasisAmount` | `decimal?` | Yes |
| `BasisCurrencyId` | `int?` | Yes |
| `SourceReference` | `string?` | Yes |

`CommissionRecipientType`: `Agency, Corporate, Airline, Partner, Other`.

Do not infer commission recipient from SalesContext. A source must provide it.

## 8.2 `PricingAllocation`

Keep current field set:

`PricingLineId, OrderItemId?, OrderServiceId?, OrderJourneyId?, OrderSegmentId?, TravellerId?, Amount, CurrencyId, EquivalentAmount, EquivalentCurrencyId`.

This is the bridge that allows tax/fare/fee/commission facts to be attributed at the correct commercial grain without creating a generic fare-construction tree.

## 8.3 `ExchangeRateSnapshot`

Keep:

`FromCurrencyId, ToCurrencyId, Rate, DecimalPlaces, PeriodId?`.

Historical conversion is immutable. Precision must preserve the source rate; it must never fall back to a low-precision EF default. IATA/settlement exchange-rate references are source facts, not recalculated later.

## 8.4 `ServiceAccountingSnapshot` — future/source-gated

IATA Service can carry non-customer-facing internal value and interline settlement information. Ordering preserves what the source gives; Revenue Accounting interprets/recognizes it.

| Field | Type | Null |
|---|---|---:|
| `InternalValueAmount` | `decimal?` | Yes |
| `InternalValueCurrencyId` | `int?` | Yes |
| `SettlementAmount` | `decimal?` | Yes |
| `SettlementCurrencyId` | `int?` | Yes |
| `SettlementCarrierId` | `int?` | Yes |
| `InterlineSettlementCode` | `string?` | Yes |
| `AgreementReference` | `string?` | Yes |
| `SourceReference` | `string?` | Yes |

Ordering does not calculate proration when these values are absent.

---

# 9. Commercial change history

## 9.1 `OrderChange`

Current:
`Id, OrderId, ChangeType, CommercialVersion, ActorContext, SourceReference?, CommittedAt`.

Final additions:

| Field | Type | Null | Purpose |
|---|---|---:|---|
| `SourceSystem` | `string?` | Yes | AirOffer/AirPrice/Disruption/etc. when the change is sourced. |
| `ReasonCode` | `string?` | Yes | Business reason code from servicing/disruption source. |
| `IsInvoluntary` | `bool` | No | Distinguishes customer-voluntary servicing from involuntary disruption servicing. |
| `WaiverCode` | `string?` | Yes | Source-approved waiver; never self-generated. |

Recommended final `OrderChangeType` vocabulary:

```text
Create
AddProduct
RemoveService
Cancel
ChangeService
TravellerCorrection
ContactCorrection
Split
Refund
Exchange
Reissue
Revalidation
InvoluntaryReaccommodation
GroupNameUpdate
GroupCapacityChange
```

Do not add a new aggregate for each change type. New PricingLines/Items/Services/Documents plus the `OrderChange` provenance are the durable history.

---

# 10. Time model — no universal Order TTL

This section is a core ADR.

There are independent clocks:

1. **Offer expiry** — AirOffer fact before acceptance.
2. **Price guarantee / reservation validation validity** — AirPrice fact for a specific pricing/validation decision.
3. **Ticketing deadline** — accepted commercial/fare rule deadline; ATPCO Cat 5 can be relative to reservation confirmation or departure.
4. **Provider hold expiry** — FlightFlow/supplier resource fact.
5. **Payment authorization/checkout expiry** — JetPay/payment-provider fact.
6. **Group deadlines** — names/deposit/final payment contractual facts.
7. **Delivery consumption expiry** — delivery-provider/airline service fact.

They must not be collapsed into one `Order.TimeToLive`.

## 10.1 `OrderTimeLimit` — future Order child

Only **commercial/order deadlines** belong here.

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `Type` | `OrderTimeLimitType` | No |
| `Scope` | `OrderTimeLimitScope` | No |
| `OrderItemId` | `long?` | Yes |
| `PassengerGroupId` | `long?` | Yes |
| `DueAt` | `DateTimeOffset` | No |
| `SourceSystem` | `string` | No |
| `SourceReference` | `string?` | Yes |
| `SourceVersion` | `string?` | Yes |
| `CreatedByChangeId` | `long` | No |
| `SupersededByTimeLimitId` | `long?` | Yes |
| `CreatedAt` | `DateTimeOffset` | No |

`OrderTimeLimitType`:

```text
Ticketing
Payment
PassengerName
Deposit
FinalPayment
Other
```

`OrderTimeLimitScope`:

```text
Order
OrderItem
PassengerGroup
```

Reservation hold expiry and AirPrice validation validity **do not** go into `OrderTimeLimit`.

## 10.2 Current `LastTicketingDate`

Until `OrderTimeLimit` is materialized, `Order.LastTicketingDate` is the current canonical accepted Ticketing deadline fact.

## 10.3 Expiry semantics

- `FulfillmentReservation.ExpiresAt` ending means the current capacity hold can no longer be used for Confirm/Issue.
- A passed `ReservationValidationTimeLimit` means the validation evidence is stale. It **does not** by itself permanently expire the Order or forbid a fresh reservation operation.
- After a definitive reservation failure/release/expiry, a new reserve operation is allowed while the hard ticketing/order deadline remains open; it reruns AirPrice validation.
- A Held reservation whose validation evidence is stale must be revalidated before Confirm/Issue.
- The whole unissued Order becomes `Expired` only when a hard applicable Order-level commercial deadline is conclusively passed (currently `LastTicketingDate`, later `OrderTimeLimit`), not when an old validation attempt expires.
- A provider resource may be marked `Expired` by local time only if the provider contract guarantees automatic expiry at `ExpiresAt`. Otherwise local time only makes it ineligible and triggers reconciliation/release.
- An issued service/document is never invalidated by a late hold-expiry event.
- If Issue/Confirm outcome is Unknown, deadline handling must reconcile the uncertain external effect before destructive release/cancel.

---

# 11. Passenger Group domain

IATA defines Passenger Group as individual passengers travelling under one commercial group name with homogeneous itinerary; group bookings have special rules and can represent tour groups or sales allotment. Amadeus and Sabre publicly support group-specific pricing/capacity/name/ticketing workflows.

## 11.1 `PassengerGroup` — Order child, not aggregate root

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `GroupName` | `string` | No |
| `IntendedPassengerQuantity` | `int` | No |
| `GroupType` | `PassengerGroupType` | No |
| `AgreementReference` | `string?` | Yes |
| `OrganizerCustomerId` | `long?` | Yes |
| `CommercialStatus` | `OrderItemCommercialState` | No |
| `CreatedByChangeId` | `long` | No |
| `EndedByChangeId` | `long?` | Yes |
| `CreatedAt` | `DateTimeOffset` | No |
| `Terms` | `PassengerGroupTerms?` | Yes |

`PassengerGroupType`:

```text
StandardGroup
TourGroup
SalesAllotment
ClosedCharter
Other
```

Open charter is normally a segment operation type with public individual sales, not a special group type.

## 11.2 `PassengerGroupTerms`

Only accepted/source-supplied terms are populated.

| Field | Type | Null |
|---|---|---:|
| `MinimumPassengerQuantity` | `int?` | Yes |
| `MaximumPassengerQuantity` | `int?` | Yes |
| `TravelTogetherRequired` | `bool?` | Yes |
| `SubstitutionAllowed` | `bool?` | Yes |
| `IndividualTravelAllowed` | `bool?` | Yes |
| `SourcePricingReference` | `string?` | Yes |

Name/deposit/final-payment deadlines are `OrderTimeLimit` rows scoped to PassengerGroup, not fields duplicated here.

## 11.3 Group names and travellers

Named passengers become normal `OrderTraveller` records with `PassengerGroupId`. Their final `OrderService` occurrences are still traveller-specific.

Before names exist, the group may have commercial item(s) and capacity reservation without individual services. This is why group capacity is not forced into fake travellers.

---

# 12. Charter support

No `Charter` aggregate exists.

## 12.1 Open charter

- `OrderSegment.OperationType = OpenCharter`.
- Seats are sold to individual customers/travel agencies like normal retail services.
- Normal Air Service / reservation / payment / document / DCS lifecycle applies.

## 12.2 Closed charter

- `OrderSegment.OperationType = ClosedCharter`.
- A `PassengerGroup` with `GroupType = ClosedCharter` represents the contracted group.
- `AgreementReference` points to the commercial charter contract when supplied.
- Group OrderItem price may be a whole-group/whole-aircraft amount instead of passenger fare.
- Capacity is represented by `ReservationGroupSpace` before names exist.
- Names/travellers may arrive later; individual service/document occurrences are created when required by airline delivery/ticketing policy.
- Corporate/government/tour-operator payer remains `CustomerId` / SalesContext; travellers are beneficiaries, not forced to be buyers.

This covers charter without creating a second incompatible Order model.


---

# 13. Aggregate: FulfillmentReservation

## 13.1 Purpose

`FulfillmentReservation` owns the **business reservation resource truth observed by Ordering**. It does not own provider execution retries; those belong to `FulfillmentTask`.

One `FulfillmentReservation` = one logical reservation operation sent to one provider under one stable external-effect identity.

## 13.2 Fields

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Logical reservation operation identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `FulfillmentProviderKey` | `string` | No | NOW | Provider owning this operation. |
| `Mode` | `ReservationMode` | No | NOW | `HoldThenConfirm`, `ImmediateConfirm`, `None`. |
| `IdempotencyKey` | `string` | No | NOW | Stable for this logical operation and all Unknown replays. |
| `CorrelationReference` | `string` | No | NOW | Technical/business correlation; not provider booking ref. |
| `ProviderOperationRef` | `string?` | Yes | NOW | Supplier operation/hold reference. Immutable once known. |
| `Status` | `FulfillmentReservationStatus` | No | NOW | Aggregate resource summary. |
| `RequestedExpiresAt` | `DateTimeOffset?` | Yes | NOW | Exact expiry requested in the persisted original effect. Immutable across Unknown replay. |
| `ReservationValidationTimeLimit` | `DateTimeOffset?` | Yes | NOW | Validity of the AirPrice reservation-validation decision used by this operation. **Renewable evidence, not a hard Order expiry.** |
| `ExpiresAt` | `DateTimeOffset?` | Yes | NOW | Provider-observed hold/resource validity. Provider authoritative after successful response. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Operation created. |
| `LastObservedAt` | `DateTimeOffset` | No | NOW | Latest provider resource observation. |
| `Units` | collection | No | NOW | Provider operational units. |
| `GroupSpaces` | collection | No | FUTURE | Group/block capacity where no named individual services exist yet. |

### `FulfillmentReservationStatus`

Current vocabulary is retained:

```text
Pending
Waitlisted
Held
Confirmed
Rejected
CancellationPending
Released
Expired
Unknown
Mixed
Cancelled
```

`Unknown` is business resource uncertainty. It must never be silently converted to Failed because retry budget was exhausted.

## 13.3 `ReservationUnit`

One ReservationUnit = one provider operational unit covering 1..N OrderService occurrences.

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | NOW |
| `FulfillmentReservationId` | `long` | No | NOW |
| `UnitCorrelationKey` | `string` | No | NOW |
| `Status` | `ReservationMemberStatus` | No | NOW |
| `ProviderUnitRef` | `string?` | Yes | NOW |
| `RawStatusCode` | `string?` | Yes | NOW |
| `ObservedSeat` | `string?` | Yes | NOW |
| `ProviderValidUntil` | `DateTimeOffset?` | Yes | FUTURE / SOURCE-GATED |
| `OrderServiceIds` | `IReadOnlyCollection<long>` | No | NOW |

Air + selected Seat can be in one unit. A hotel room may later cover multiple traveller-specific partner-service occurrences. No generic coverage graph is required.

## 13.4 `ReservationGroupSpace` — future group/block capacity

Used only when capacity exists before passenger-specific OrderService occurrences.

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `FulfillmentReservationId` | `long` | No |
| `PassengerGroupId` | `long` | No |
| `CoveredJourneyIds` | `IReadOnlyCollection<long>` | No |
| `CoveredSegmentIds` | `IReadOnlyCollection<long>` | No |
| `RequestedQuantity` | `int` | No |
| `ConfirmedQuantity` | `int?` | Yes |
| `ProviderGroupReference` | `string?` | Yes |
| `Status` | `ReservationMemberStatus` | No |
| `ValidUntil` | `DateTimeOffset?` | Yes |
| `RawStatusCode` | `string?` | Yes |

Do not synthesize individual travellers merely to hold group block space.

## 13.5 Reservation invariants

1. Unknown retry = same Reservation + same IdempotencyKey + exact original request + new attempt.
2. Definitive `Rejected/Released/Expired/Cancelled` followed by a new business retry = new Reservation + new IdempotencyKey + fresh validation.
3. `Held/Confirmed` service cannot be reserved again unless servicing explicitly replaces/releases it.
4. Provider operation ref and unit refs are separate identities.
5. Provider A failure never automatically releases provider B success unless a future product contract explicitly defines coupled compensation.
6. PNR generation is based on first positive Air reservation and occurs once.
7. Reservation status does not overwrite sold segment facts.
8. Provider `ExpiresAt` and AirPrice `ReservationValidationTimeLimit` remain separate.

---

# 14. Reservation time and validation behavior — final decision

This supersedes any code behavior that treats an old validation TimeLimit as permanent Order expiry.

## 14.1 New reservation operation

Before a **new** reserve operation:

1. check hard applicable Order ticketing deadline;
2. run current AirPrice reservation validation for the intended fare/pricing-unit closure;
3. persist the validation result/time limit as operation evidence;
4. compute requested provider expiry from the applicable hard constraints known to the provider request;
5. persist the exact intent before dispatch.

The previous operation's validation TimeLimit does not prevent a new operation.

## 14.2 Unknown retry

Do not re-run AirPrice validation and do not recompute requested expiry. Replay/reconcile the exact persisted intent.

## 14.3 Held reservation when validation validity passes

The hold may still physically exist, but it is not eligible for payment-time confirmation/issue based solely on stale pricing evidence. Obtain a fresh authoritative validation before Confirm/Issue. Do not terminally expire the whole Order from this fact.

## 14.4 Provider hold expiry

At `ExpiresAt`:

- the current hold is no longer eligible for Confirm/Issue;
- if provider contract certifies automatic expiry, the reservation may become `Expired` from the clock/event;
- otherwise reconcile/read/release according to provider capability and retain uncertainty honestly.

`FlightReservationExpiredEvent.ReferenceId` must not be consumed until its identity semantics are explicitly known.

---

# 15. Aggregate: FulfillmentTask

## 15.1 Purpose

`FulfillmentTask` owns external-effect execution, retry, read-back and evidence. It is not the business reservation/document/payment state itself.

The Stage-2 implementation is reservation-specific today. The final shape is deliberately defined now so Stage 4+ does not need a new execution aggregate.

## 15.2 Final fields

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | NOW |
| `OrderId` | `long` | No | NOW |
| `FulfillmentReservationId` | `long?` | Yes | MASTER FINAL | Required for reservation tasks; null for tasks such as direct document/partner fulfillment that do not belong to one reservation. Current non-null implementation is Stage-2-specific. |
| `TaskType` | `OrderFulfillmentTaskType` | No | NOW |
| `FulfillmentProviderKey` | `string` | No | NOW |
| `IdempotencyKey` | `string` | No | NOW |
| `CorrelationReference` | `string` | No | NOW |
| `Status` | `OrderFulfillmentStatus` | No | NOW |
| `AttemptCount` | `int` | No | NOW |
| `CreatedAt` | `DateTimeOffset` | No | NOW |
| `CompletedAt` | `DateTimeOffset?` | Yes | NOW |
| `LastFailureKind` | `FulfillmentFailureKind?` | Yes | NOW |
| `LastFailureReason` | `FulfillmentFailureReason?` | Yes | NOW |
| `LastError` | `string?` | Yes | NOW |
| `Targets` | collection | No | NOW/FUTURE typed evolution |
| `Attempts` | collection | No | NOW |
| `Interactions` | collection | No | NOW |

## 15.3 `FulfillmentTaskTarget` — final typed target shape

Current Stage-2 target is `ReservationUnitId`. Final shape is:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `FulfillmentTaskId` | `long` | No |
| `TargetType` | `FulfillmentTargetType` | No |
| `TargetId` | `long` | No |
| `Action` | `OrderFulfillmentTargetAction` | No |

`FulfillmentTargetType` is a **closed finite enum**, not a generic graph:

```text
ReservationUnit
ReservationGroupSpace
OrderService
OrderItem
ElectronicTicket
TicketCoupon
ElectronicMiscDocument
EmdCoupon
PricingLine
PassengerGroup
```

Materialize the typed target only when Stage 4 needs non-reservation targets. Do not build it early for its own sake.

## 15.4 `FulfillmentTaskAttempt`

Current fields are final:

`Id, FulfillmentTaskId, AttemptNumber, StartedAt, CompletedAt?, Outcome?, FailureKind?, FailureReason?, Error?`.

## 15.5 `ProviderInteraction`

The latest reviewed implementation is accepted as the final semantic direction.

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `FulfillmentTaskId` | `long` | No |
| `FulfillmentTaskAttemptId` | `long` | No |
| `AttemptNumber` | `int` | No |
| `Sequence` | `int` | No |
| `FulfillmentProviderKey` | `string` | No |
| `InteractionType` | `ProviderInteractionType` | No |
| `IdempotencyKey` | `string?` | Yes for reads only |
| `CorrelationReference` | `string?` | Yes |
| `RequestPayload` | `string` | No |
| `RequestHash` | `string` | No |
| `ResponsePayload` | `string?` | Yes |
| `ResponseHash` | `string?` | Yes |
| `ProviderOperationRef` | `string?` | Yes |
| `Status` | `ProviderInteractionStatus` | No |
| `StartedAt` | `DateTimeOffset` | No |
| `CompletedAt` | `DateTimeOffset?` | Yes |
| `ProviderStatusCode` | `int?` | Yes |
| `Error` | `string?` | Yes |

One Attempt may contain multiple ordered interactions, e.g. `ReadReservation -> Replay CreateHold`.

The exact original semantic request must survive mutable Order changes and be replayable under the same external-effect identity.

## 15.6 Final task types

Task types are introduced only when their Stage arrives. Final vocabulary must be capable of:

```text
ReserveInventory
ReleaseReserved
ConfirmInventory
ExtendReservation
ReconcileReservation
CancelConfirmed
IssueTicket
IssueEmd
VoidDocument
RefundDocument
ExchangeDocument
RevalidateDocument
ReservePartnerService
CancelPartnerService
```

No generic programmable workflow engine is authorized.

---

# 16. Payment / checkout domain

## 16.1 Ownership

JetPay or another standard checkout/payment orchestrator owns:

- payment method/tender;
- authorization/capture transaction lifecycle;
- PSP references;
- card/wallet data;
- refunds at payment rail level;
- fraud/3DS/customer action state.

Ordering owns only the evidence needed to decide whether the commercial Order can progress.

The common checkout ACL must support the semantic operations:

```text
CreatePaymentIntent
GetPaymentIntent
CapturePaymentIntent
CancelPaymentIntent
```

plus authoritative asynchronous intent-status events. The mock must behave like the final ACL; replacing it with JetPay must not redesign Ordering.

## 16.2 `OrderPaymentCoverage` — Order child

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `PaymentIntentId` | `string` | No |
| `CommercialVersion` | `int` | No |
| `RequestedAmount` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `CaptureMode` | `PaymentCaptureMode` | No |
| `Status` | `CheckoutPaymentStatus` | No |
| `AuthorizedAmount` | `decimal` | No |
| `CapturedAmount` | `decimal` | No |
| `ReturnedAmount` | `decimal` | No |
| `AuthorizationExpiresAt` | `DateTimeOffset?` | Yes |
| `AppliesToCurrentOrder` | `bool` | No |
| `UpdatedAt` | `DateTimeOffset` | No |
| `ProviderVersion` | `string?` | Yes |
| `Allocations` | collection | No |

### `PaymentCaptureMode`

```text
Automatic
Manual
```

### `CheckoutPaymentStatus`

```text
Created
RequiresCustomerAction
Processing
Authorized
PartiallyCaptured
Captured
Failed
Cancelled
Expired
```

A timeout is not a PaymentIntent business state; use Get/read-back to recover the provider resource.

## 16.3 `OrderPaymentAllocation` — future when split/group-scoped funding exists

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderPaymentCoverageId` | `long` | No |
| `OrderItemId` | `long?` | Yes |
| `PricingLineId` | `long?` | Yes |
| `PassengerGroupId` | `long?` | Yes |
| `Amount` | `decimal` | No |
| `CurrencyId` | `int` | No |

At least one target is required when allocations are used.

The final domain supports multiple payment intents/split funding. An intent contributes at most once; duplicate events never double-count it.

## 16.4 Payment gate

Baseline issuance/confirmation gate:

- captured applied amount must cover required payable scope;
- currency/version must match;
- authorization-only counts only if an explicit airline payment policy says authorization is sufficient for that fulfillment step;
- `PaidUnapplied`/reversed/returned coverage cannot authorize issue;
- zero-value payable scope needs no payment intent.

Ordering does not derive card/tender/provider details.

---

# 17. Stage 3 flow — Checkout + Confirm Reservation

```text
Order has Held reservation(s)
        ↓
Create/Get checkout intent
        ↓
Customer action / processing
        ↓
Captured (or explicitly accepted authorization policy)
        ↓
Re-check hard ticketing deadline
        ↓
If reservation validation evidence stale -> fresh AirPrice validation
        ↓
Confirm each HoldThenConfirm reservation independently
        ↓
Confirmed capacity + valid payment evidence
        ↓
Eligible for Stage 4 Issue
```

Rules:

- Payment provider B failing never destroys provider A's successful reservation automatically.
- Confirm timeout = reservation confirmation uncertainty, not failure; recover via provider contract/read-back/replay if certified.
- Successful payment plus expired hold does not permit issue; reserve again if commercial deadline remains open.
- Successful hold plus insufficient payment does not permit issue.

---

# 18. Aggregate: ElectronicTicket

Separate ETKT from EMD permanently.

## 18.1 `ElectronicTicket` root — final field shape

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OriginalOrderId` | `long` | No |
| `CurrentServicingOrderId` | `long` | No |
| `TravellerId` | `long` | No |
| `IssueOperationId` | `long` | No |
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
| `PredecessorExchangeOperationId` | `long?` | Yes |
| `Coupons` | collection | No |
| `PriceLinks` | collection | No |
| `VoidRecord` | `DocumentVoidRecord?` | Yes |
| `RefundRecords` | collection | No |
| `ExchangeRecords` | collection | No |
| `RevalidationRecords` | collection | No |

### `DocumentIssuanceContext`

| Field | Type | Null |
|---|---|---:|
| `IssuerCarrierId` | `int` | No |
| `IssuingOfficeId` | `long?` | Yes |
| `IssuedByActorId` | `long?` | Yes |
| `TravelAgencyId` | `long?` | Yes |
| `AgencyIataNumber` | `string?` | Yes |
| `Pcc` | `string?` | Yes |
| `SalesChannel` | `SalesChannel?` | Yes |

Document context is frozen at issuance; later Order seller changes do not rewrite it.

## 18.2 `TicketCoupon`

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

Financial status uses the current stable vocabulary:
`Open, Used, Void, Exchanged, Refunded, Suspended`.

Control status remains separate from financial status.

## 18.3 `IssuedSegmentSnapshot`

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

Issued snapshot is never rebuilt from a later changed OrderSegment.

## 18.4 `DocumentPriceLink`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicTicketId` | `long` | No |
| `TicketCouponId` | `long?` | Yes |
| `PricingLineId` | `long` | No |
| `PricingAllocationId` | `long?` | Yes |
| `AttributedValue` | `decimal` | No |
| `CurrencyId` | `int` | No |

This is the immutable bridge needed for ticket/coupon accounting, refund and exchange without recreating fare construction.


## 18.5 Ticket servicing records

### `DocumentVoidRecord`

| Field | Type | Null |
|---|---|---:|
| `OperationId` | `long` | No |
| `ReasonCode` | `string?` | Yes |
| `ReasonText` | `string?` | Yes |
| `ProviderReference` | `string?` | Yes |
| `ActorId` | `long?` | Yes |
| `VoidedAt` | `DateTimeOffset` | No |

Void is document disposition within issuer/void eligibility. It is not a refund.

### `DocumentRefundRecord`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicTicketId` | `long` | No |
| `OperationId` | `long` | No |
| `QuotedRefundId` | `string` | No |
| `PricingSource` | `string` | No |
| `SourcePricingReference` | `string` | No |
| `RefundType` | `DocumentRefundType` | No |
| `ApprovedAmount` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `PenaltyAmount` | `decimal?` | Yes |
| `WaiverCode` | `string?` | Yes |
| `DispositionCode` | `string?` | Yes |
| `PaymentDispositionReference` | `string?` | Yes |
| `ProviderReference` | `string?` | Yes |
| `RefundedAt` | `DateTimeOffset` | No |
| `CouponIds` | `IReadOnlyCollection<long>` | No |

Refund authority is the accepted AirPrice servicing decision plus current document/control evidence. Sale-time refundability flags never substitute for it.

### `DocumentExchangeRecord`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `PredecessorElectronicTicketId` | `long` | No |
| `SuccessorElectronicTicketId` | `long` | No |
| `OperationId` | `long` | No |
| `QuotedExchangeId` | `string` | No |
| `SourcePricingReference` | `string` | No |
| `AdditionalCollection` | `decimal` | No |
| `ResidualValue` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `PenaltyAmount` | `decimal?` | Yes |
| `WaiverCode` | `string?` | Yes |
| `ProviderReference` | `string?` | Yes |
| `ExchangedAt` | `DateTimeOffset` | No |

Coupon predecessor/successor lineage must also be preserved.

### `DocumentRevalidationRecord`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicTicketId` | `long` | No |
| `OperationId` | `long` | No |
| `CouponIds` | `IReadOnlyCollection<long>` | No |
| `SourceDecisionReference` | `string` | No |
| `ProviderReference` | `string?` | Yes |
| `RevalidatedAt` | `DateTimeOffset` | No |

Revalidation is allowed only when authoritative servicing says no fare/document exchange is required. Never infer it from zero price difference.

---

# 19. Aggregate: ElectronicMiscDocument

IATA EMD is the accountable document for optional/ancillary value. Keep EMD-A and EMD-S separate from ETKT.

## 19.1 `ElectronicMiscDocument`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OriginalOrderId` | `long` | No |
| `CurrentServicingOrderId` | `long` | No |
| `TravellerId` | `long` | No |
| `IssueOperationId` | `long` | No |
| `DocumentNumber` | `string` | No |
| `Type` | `ElectronicMiscDocumentType` | No |
| `IssuanceContext` | `DocumentIssuanceContext` | No |
| `IssuedAt` | `DateTimeOffset` | No |
| `IssuedTotal` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `ProviderReference` | `string?` | Yes |
| `StatusSummary` | `ElectronicMiscDocumentStatus` | No |
| `DocumentVersion` | `int` | No |
| `PredecessorEmdId` | `long?` | Yes |
| `Coupons` | collection | No |
| `PriceLinks` | collection | No |
| `RefundRecords` | collection | No |
| `ExchangeRecords` | collection | No |
| `AssociationHistory` | collection | No |

Current type values remain:

```text
Associated   // EMD-A
Standalone   // EMD-S
```

## 19.2 `EmdCoupon`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicMiscDocumentId` | `long` | No |
| `CouponNumber` | `int` | No |
| `Purpose` | `EmdCouponPurpose` | No |
| `OriginalOrderServiceId` | `long?` | Yes |
| `CurrentOrderServiceId` | `long?` | Yes |
| `PricingLineId` | `long?` | Yes |
| `ServiceSubCode` | `string?` | Yes |
| `ReasonForIssuanceCode` | `string?` | Yes |
| `ReasonForIssuanceSubCode` | `string?` | Yes |
| `AssociatedTicketCouponId` | `long?` | Yes |
| `IssuanceValue` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `Status` | `EmdCouponStatus` | No |
| `ProviderCouponStatusCode` | `string?` | Yes |
| `PredecessorEmdCouponId` | `long?` | Yes |

At least one of `OrderServiceId` or `PricingLineId` is required for a value-bearing coupon, according to its purpose.

Current purposes are retained:
`Service, Fee, Deposit, ResidualValue`.

## 19.3 EMD association history

EMD-A association changes are append-only:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `EmdCouponId` | `long` | No |
| `AssociationAction` | `EmdAssociationAction` | No |
| `TicketCouponId` | `long?` | Yes |
| `OperationId` | `long` | No |
| `ProviderReference` | `string?` | Yes |
| `OccurredAt` | `DateTimeOffset` | No |

Actions: `Associate, Disassociate, Reassociate`.

Do not overwrite prior association merely because an exchange changes the target ticket.

---

# 20. Aggregate: DocumentStock

Document stock owns controlled accountable-document number allocation. Document numbers are never random.

## 20.1 `DocumentStock`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OwnerAirlineId` | `int` | No |
| `OfficeId` | `long?` | Yes |
| `DocumentType` | `DocumentStockType` | No |
| `AirlinePrefix` | `string` | No |
| `FormCode` | `string?` | Yes |
| `SerialFrom` | `long` | No |
| `SerialTo` | `long` | No |
| `NextSerial` | `long` | No |
| `SerialWidth` | `int` | No |
| `CheckDigitProfile` | `string?` | Yes |
| `Status` | `DocumentStockStatus` | No |
| `CreatedAt` | `DateTimeOffset` | No |

## 20.2 `DocumentStockAllocation`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `DocumentStockId` | `long` | No |
| `OperationId` | `long` | No |
| `Role` | `DocumentRole` | No |
| `DocumentNumber` | `string` | No |
| `AllocatedSerial` | `long` | No |
| `AllocatedAt` | `DateTimeOffset` | No |

Same `(OperationId, Role)` replay returns the same number; allocated numbers are never recycled after uncertain external issuance.

---

# 21. Issue end-to-end flow

```text
Order scope selected
  ↓
Commercial services active and documentable
  ↓
Required reservations confirmed/committed
  ↓
Payment coverage sufficient
  ↓
Ticketing deadline open
  ↓
Required DCS/control facts not conflicting
  ↓
Plan ETKT + EMD set
  ↓
Reserve/allocate document numbers
  ↓
External/local issuer effect with durable FulfillmentTask evidence
  ↓
Persist ETKT/EMD/coupons/price links atomically with authoritative success
  ↓
Order summary becomes Ticketed when all required documentable scope is issued
```

Rules:

- Air document coupons follow traveller × flight/segment grain.
- Fare basis/RBD/cabin/segment facts are snapshotted into ticket coupons at issue.
- Seat/baggage/ancillary documents use EMD according to service/document requirements, not a generic `TrafficDocument` abstraction.
- Unknown issue outcome blocks destructive cancellation/refund until reconciled.

---

# 22. Cancel, Void, Refund, Exchange, Reissue, Revalidate

## 22.1 Cancel — commercial scope, normally unissued

`CancelOrder` / `CancelServices` flow:

1. resolve exact active unissued commercial scope;
2. block if issue effect is Unknown;
3. release Held reservations or cancel Confirmed capacity under provider contract;
4. cancel/correct checkout coverage as payment state requires;
5. append `OrderChange(Cancel)`;
6. mark affected items/services Cancelled without deleting history;
7. append cancellation pricing lines if authoritative source requires fees/credits.

Cancel is not equivalent to Void or Refund.

## 22.2 Void — issued accountable document within void authority

`VoidDocument`:

- requires issuer-authorized void eligibility/deadline;
- requires coupon unused/control eligibility;
- appends `DocumentVoidRecord`;
- updates document/coupon statuses;
- payment reversal may follow payment-owner contract;
- commercial Order need not be fully cancelled unless caller explicitly requested cancellation.

## 22.3 Refund

`RefundOrder` / `RefundServices`:

1. determine coupon/service scope;
2. obtain AirPrice servicing quote/decision under current document/control/delivery facts;
3. quote contains refundable value, taxes, penalties, waiver and disposition authority;
4. accept quote against unchanged relevant versions/scope;
5. append `OrderChange(Refund)` and credit/penalty PricingLines;
6. append document refund records and coupon dispositions;
7. request payment return/refund through payment owner;
8. retain sale, document, payment and refund histories permanently.

Partial refund is supported at coupon/service scope.

## 22.4 Exchange / reissue

`ExchangeOrder`:

1. obtain new requested itinerary/services;
2. AirPrice evaluates old ticket/coupon state + old accepted pricing context + new pricing scope;
3. reserve new required capacity;
4. obtain additional-collection funding authority if needed;
5. issue successor ETKT/EMD set;
6. only after successor outcome is known, disposition predecessor coupons/docs according to authoritative plan;
7. append `OrderChange(Exchange)` + new PricingLines;
8. create successor services/items; old occurrences become Replaced, never overwritten;
9. link predecessor/successor tickets and coupons;
10. release old capacity after safe pivot.

Unknown new issue blocks destructive predecessor cleanup.

## 22.5 Revalidation

Only use when source authority explicitly says the change can be applied without a fare/document exchange. Record document revalidation evidence and changed coupon association/segment snapshot as authorized; do not infer revalidation from “zero amount”.

---

# 23. Split Order

Split is a commercial ownership operation, not a provider-seat split only.

## 23.1 `OrderLineage`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `RelatedOrderId` | `long` | No |
| `RelationType` | `OrderLineageType` | No |
| `ChangeId` | `long` | No |
| `OccurredAt` | `DateTimeOffset` | No |

Relations: `SplitParent, SplitChild`.

## 23.2 Split invariants

- split whole travellers, including infant/guardian closure;
- move/clone commercial scope according to accepted ownership rules without duplicating economic value;
- current servicing ownership of ETKT/EMD moves to the correct child order while original issue Order remains immutable;
- shared/multi-beneficiary supplier reservations require provider split support or explicit re-reservation; never infer array-order mapping;
- pricing transfer is represented by explicit transfer/adjustment lines, not silent total mutation;
- RecordLocator/PNR outcome follows provider capability; no assumption that child must share or receive a new provider record locator.

---

# 24. DCS / Service Delivery domain

DCS owns check-in, boarding and operational consumption. Ordering stores only sourced evidence needed for servicing/accounting/customer Order state.

## 24.1 `OrderServiceDeliveryObservation` — append-only Order child

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `OrderServiceId` | `long` | No |
| `Sequence` | `int` | No |
| `Status` | `ServiceDeliveryStatus` | No |
| `MilestoneCode` | `string?` | Yes |
| `DeliveryProviderKey` | `string` | No |
| `ProviderReference` | `string?` | Yes |
| `RawStatusCode` | `string?` | Yes |
| `EffectiveAt` | `DateTimeOffset` | No |
| `ObservedAt` | `DateTimeOffset` | No |
| `SourceVersion` | `string?` | Yes |

IATA-aligned `ServiceDeliveryStatus`:

```text
ReadyToProceed
ReadyToDeliver
InProgress
Delivered
NotClaimed
FailedToDeliver
UnableToDeliver
Expired
Suspended
Removed
```

Use the exact source status/milestone as raw evidence where mapping is not authoritative.

## 24.2 `OrderAirDeliveryEvidence` — source-specific DCS details

Materialize only if DCS contracts expose them.

| Field | Type | Null |
|---|---|---:|
| `DeliveryObservationId` | `long` | No |
| `DcsPassengerReference` | `string?` | Yes |
| `OperatingFlightReference` | `string?` | Yes |
| `CheckInStatusCode` | `string?` | Yes |
| `BoardingStatusCode` | `string?` | Yes |
| `LiftStatusCode` | `string?` | Yes |
| `ActualSeatNumber` | `string?` | Yes |
| `CheckedInAt` | `DateTimeOffset?` | Yes |
| `BoardedAt` | `DateTimeOffset?` | Yes |
| `LiftedAt` | `DateTimeOffset?` | Yes |
| `NoShowAt` | `DateTimeOffset?` | Yes |

Rules:

- actual seat does not overwrite sold `OrderSeatService.SeatNumber`;
- check-in alone does not mean flown/consumed;
- coupon `Used`/service `Delivered` transitions require authoritative delivery evidence;
- no-show/failed delivery remains distinguishable from voluntary refund/cancel;
- DCS evidence can block refund/exchange when operational consumption/control says so.

---

# 25. Disruption / irregular operations

Operations/DCS owns the disruption. Ordering owns the effect on this Order.

## 25.1 `OrderDisruptionImpact`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `SourceSystem` | `string` | No |
| `SourceDisruptionReference` | `string` | No |
| `Type` | `DisruptionImpactType` | No |
| `SourceTypeCode` | `string?` | Yes |
| `OperatingFlightReference` | `string?` | Yes |
| `AffectedOrderServiceIds` | `IReadOnlyCollection<long>` | No |
| `EffectiveAt` | `DateTimeOffset` | No |
| `ObservedAt` | `DateTimeOffset` | No |
| `ResolutionStatus` | `DisruptionResolutionStatus` | No |
| `AcceptedChangeId` | `long?` | Yes |

High-level `DisruptionImpactType`:
`Cancellation, ScheduleChange, Delay, Misconnection, EquipmentChange, Other`.

`DisruptionResolutionStatus`:
`Observed, ProtectionAvailable, ReaccommodationAccepted, Resolved, Closed`.

## 25.2 Involuntary reaccommodation flow

```text
Disruption observed
  ↓
Affected service scope frozen
  ↓
Protection/reaccommodation source proposes new service(s)
  ↓
Customer/airline policy accepts protection
  ↓
OrderChange(IsInvoluntary=true, reason/waiver/source ref)
  ↓
New successor services + reservation
  ↓
Revalidate or reissue documents according to authority
  ↓
Reassociate EMDs/ancillaries where permitted
  ↓
Release old capacity only after new effect is safe
```

Do not rewrite the sold OrderSegment to match operational schedule changes.

---

# 26. Revenue Accounting / Order Accounting readiness

Ordering is **not** the revenue accounting ledger. Its responsibility is to preserve and publish enough immutable business evidence so accounting never reconstructs economics from a mutable current Order.

## 26.1 Required outbound accounting facts

### Sale / commercial

- OrderReference / OrderId / owner airline
- OrderItem / OrderService identities
- CommercialVersion / OrderChange
- sale date/time
- seller channel / office / agency / corporate identifiers when sourced
- passenger/group scope
- sold marketing/operating carrier and itinerary

### Price

- fare/tax/fee/YQ-YR/ancillary/discount/markup/commission lines
- code + source reference
- exact source amount/currency
- exact equivalent amount/currency
- immutable exchange-rate snapshot + period reference
- tax country/station when supplied
- allocation to item/service/traveller/journey/segment
- settlement-only vs customer-price treatment
- commission recipient/rate/basis when supplied

### Documents

- ETKT/EMD document number
- issuer/office/agency context
- issue date
- coupons and original/current service links
- fare basis / RBD / cabin / segment snapshot
- coupon financial/control history
- EMD service/RFIC/RFISC/sub-code and association history
- void/refund/exchange/revalidation lineage

### Payment

- PaymentIntent reference
- authorized/captured/returned amounts
- currency
- coverage allocation if present
- no PAN/card secrets

### Delivery

- service delivery status/milestones
- DCS consumption/no-show evidence
- delivery provider reference

### Interline / settlement

- responsible/validating/operating/marketing/issuer carriers
- source-provided internal value
- source-provided settlement amount/currency
- agreement/settlement reference
- optional-service interline settlement/concurrence facts when supplied

Revenue Accounting owns:

- revenue recognition journal;
- proration when not explicitly supplied;
- interline billing/clearing;
- accounting-period control;
- GL postings;
- tax ledger/reporting.

Ordering must never manufacture a prorated value to satisfy accounting.

---

# 27. Interline, codeshare and partner servicing

The final model supports:

- marketing carrier != operating carrier on Segment;
- responsible airline and validating carrier on Service when sourced;
- issuer/plating carrier in document issuance context;
- external provider reservation refs;
- ET coupon control transfer (`TicketCouponControlStatus`);
- EMD associated/standalone and service concurrence facts;
- source-provided interline settlement/internal values;
- codeshare disruption/reaccommodation without rewriting sold history.

If a partner is the actual service supplier, `FulfillmentProviderKey` routes external fulfillment while `ResponsibleAirlineId` / settlement facts preserve the business party identity.

---

# 28. Group end-to-end scenario

```text
Group offer / negotiated contract
  ↓
Order + PassengerGroup + group-priced OrderItem
  ↓
Reserve group block capacity (ReservationGroupSpace)
  ↓
Deposit deadline / payment
  ↓
Passenger-name deadline
  ↓
Create named OrderTravellers + passenger-specific OrderServices
  ↓
Reconcile group block with named services
  ↓
Final payment
  ↓
Issue ETKT/EMD per passenger as required
  ↓
DCS delivery
```

Required behaviors:

- block space can exist before passenger names;
- names may be substituted only if accepted group terms permit;
- reduction/cancellation releases group capacity without deleting commercial history;
- group deposit/final payment can be funded by group organizer and later individual payments if payment allocations support it;
- travel-together/minimum-group constraints come from accepted source terms;
- named passengers remain normal travellers/services/documents, not a separate group-ticket universe.

---

# 29. Charter end-to-end scenarios

## Open charter

Normal B2C/agency Order:

`OpenCharter Segment -> passenger services -> reserve -> pay -> issue -> DCS`.

## Closed charter

```text
Corporate/government/tour-operator customer
  ↓
ClosedCharter PassengerGroup + contracted group/air item
  ↓
whole-aircraft/group capacity reservation
  ↓
contractual deposit/final-payment/name deadlines
  ↓
manifest/named travellers added when available
  ↓
individual delivery/document records only if airline policy requires them
  ↓
DCS manifest/delivery evidence
  ↓
accounting based on group contract price + source settlement facts
```

Do not force closed charter into “one retail AirFare OrderItem per passenger × bound” before passenger-level commercial services actually exist.

---

# 30. Canonical commands/use cases

These are **domain use cases**, not controller/file instructions.

## Sale / identity

- `CreateOrderFromOffer`
- `CorrectTravellerProfile`
- `UpdateTravellerDocuments`
- `UpdateOrderContact`
- `AddOrderRemark`

## Reservation

- `ReserveOrder`
- `ReserveServices`
- `ReserveGroupSpace`
- `ReleaseReservation`
- `ExtendReservation` (only supported provider)
- `ReconcileReservation`
- `ConfirmReservation`
- `CancelConfirmedReservation`
- `ApplyReservationExpiryEvidence`

## Payment

- `StartCheckout`
- `RefreshPaymentIntent`
- `CapturePaymentIntent`
- `CancelPaymentIntent`
- `ApplyPaymentIntentObservation`

## Issue

- `IssueOrder`
- `IssueAirDocuments`
- `IssueEmds`

## Unissued servicing

- `AddServiceFromOffer`
- `RemoveService`
- `ChangeService`
- `CancelOrder`
- `CancelServices`

## Documents / financial servicing

- `VoidDocument`
- `RefundOrder`
- `RefundServices`
- `ExchangeOrder`
- `ReissueDocuments`
- `RevalidateTicket`

## Split

- `SplitOrder`

## Group

- `CreateGroupOrder`
- `UpdatePassengerGroupTermsFromAuthority`
- `AddGroupTravellers`
- `SubstituteGroupTraveller`
- `ReduceGroupCapacity`
- `ApplyGroupDeadline`

## Delivery / DCS

- `ApplyServiceDeliveryObservation`
- `ApplyAirDeliveryEvidence`

## Disruption

- `RecordDisruptionImpact`
- `AcceptInvoluntaryReaccommodation`
- `RejectProtection`
- `CompleteDisruptionServicing`

Every command must have: exact scope, source authority, preconditions, invariants, state transitions, Unknown behavior and acceptance scenarios in its Stage PRD.

---

# 31. Core eligibility matrix

| Action | Required truths |
|---|---|
| Reserve | active reservable services/group space; fare topology/validation available; no positive/unresolved conflicting reservation; hard ticketing deadline open |
| Confirm reservation | Held resource; provider supports confirm; provider hold still valid; fresh pricing validation; required payment authority satisfied |
| Issue | active documentable services; required reservations Confirmed; payment gate satisfied; ticketing deadline open; no conflicting Unknown issue/control; stock/issuer authority available |
| Cancel unissued | active unissued scope; no conflicting Unknown issue; reservation/payment dispositions executable |
| Void | issued document; issuer void authority/deadline; eligible unused coupon/control |
| Refund | authoritative AirPrice refund decision; coupon/service/delivery/control eligible; no conflicting disposition |
| Exchange | authoritative old/new AirPrice decision; new capacity/funding plan; old coupon control; no unresolved conflicting effect |
| Revalidate | explicit authoritative revalidation eligibility; no implicit fare change |
| Split | whole traveller/guardian closure; shared resource/document scope resolvable; no duplicated monetary ownership |
| DCS Deliver | source delivery provider evidence; never inferred from clock/order status |

---

# 32. Failure / Unknown doctrine

For every external mutation:

```text
persist exact intended effect
  ↓
call provider
  ↓
known success -> record authoritative resource/document fact
known rejection/no effect -> terminal negative
lost response/timeout/ambiguous payload -> Unknown
```

Unknown rules:

1. never turn Unknown into Failed because max attempts elapsed;
2. prefer authoritative read-back;
3. mutation replay requires same provider identity + same idempotency key + exact persisted request;
4. later definitive source evidence updates the business resource but never erases the uncertain attempt history;
5. destructive compensation waits until potentially conflicting Unknown effects are resolved.

---

# 33. Scenario matrix — airline completeness target

The Master model must be capable of the following without changing aggregate ownership.

## Core retail

1. one ADT one-way
2. round trip one PricingUnit
3. round trip composed from one-way pricing units
4. connection with one FareComponent across multiple segments
5. open jaw / circle trip when source supports semantic type
6. multi-pax ADT/CHD/INF
7. seat selection
8. baggage allowance + paid baggage
9. meal/lounge/priority/other ancillary
10. multi-currency fare/tax/fee/commission
11. agency/corporate/partner sale
12. codeshare marketing/operating carrier

## Reservation

13. FlightFlow HoldThenConfirm
14. ImmediateConfirm supplier
15. direct-issue service with no reservation
16. multi-provider Order
17. partial provider outcomes
18. Unknown retry exact replay
19. definitive retry new operation
20. reservation expiry before ticketing deadline then fresh reserve
21. validation expiry while hold remains then fresh validation before confirm
22. waitlist
23. extendable hold
24. group block before names

## Payment

25. automatic capture
26. authorize/manual capture
27. requires customer action
28. failed/cancelled/expired intent
29. multiple intents / split tender
30. group deposit + final payment
31. paid-unapplied/returned evidence

## Issue / docs

32. ETKT one/multi coupon
33. EMD-A seat/baggage/ancillary
34. EMD-S fee/deposit/residual
35. document stock replay after lost response
36. partial document issue Unknown
37. agency issue context
38. interline/codeshare ticket

## Servicing

39. unissued cancel
40. ticket void
41. full refund
42. partial coupon refund
43. tax/penalty/waiver refund
44. voluntary exchange/additional collection
45. exchange with residual value
46. reissue
47. revalidation
48. add ancillary after ticketing
49. EMD reassociation after exchange
50. split Order

## Group / charter

51. group block with unnamed pax
52. group names added later
53. group name substitution
54. group capacity reduction
55. group cancellation/deposit disposition
56. open charter individual sale
57. closed charter whole-aircraft/group contract
58. charter passenger manifest later

## DCS / disruption

59. check-in
60. boarding
61. actual seat reassignment
62. flown/delivered
63. no-show/not-claimed
64. denied/failed delivery
65. flight cancellation disruption
66. schedule change
67. misconnection
68. involuntary reaccommodation
69. involuntary reissue/revalidation
70. disruption ancillary/hotel/meal compensation when sold/accountable

## Accounting / interline

71. tax code/country/station preservation
72. agency commission recipient/rate
73. immutable ROE
74. service internal value
75. interline settlement values
76. ET/EMD issue/void/refund/exchange history
77. DCS-delivery-driven revenue recognition fact
78. split lineage without double revenue
79. codeshare responsible/operating/marketing/issuer identities
80. accounting after refund/exchange remains reconstructable from immutable history

---

# 34. Stage materialization plan

The Master shape does **not** authorize implementation of all future types now.

## Stage 1 — Create Order from Offer — implemented

Materialized:
`Order, Items, Air/Seat Services, Travellers, Contacts, Journey/Segment/Leg, Changes, Pricing, Remarks, fare topology`.

Master corrections to carry forward:
- preserve `CabinClassId` when real Offer source provides it;
- fare PricingUnit source kind must be lossless; no invented source vocabulary.

## Stage 2 — Reserve / PNR / recovery — current

Materialized:
`FulfillmentReservation, ReservationUnit, FulfillmentTask, Attempts, Interactions, PNR`.

Closure corrections are listed in Section 36.

## Stage 3 — Checkout / payment coverage / confirm held reservation

Materialize:
`OrderPaymentCoverage`, standard checkout ACL/mock, confirmation behavior.

Do not create Payment aggregate.

## Stage 4 — Issue ETKT/EMD + DocumentStock

Materialize:
`ElectronicTicket, TicketCoupon, EMD, EmdCoupon, DocumentStock, typed FulfillmentTask targets required by Issue`.

## Stage 5 — Cancel + Void

Materialize only cancellation/document-void histories and provider behaviors.

## Stage 6 — Ancillary servicing

Materialize optional-service/baggage/partner service subtypes and EMD association behaviors actually supported.

## Stage 7 — Split

Materialize OrderLineage and split behavior.

## Stage 8 — Refund

Materialize refund histories and AirPrice servicing decision mapping.

## Stage 9 — Exchange / Reissue / Revalidation

Materialize successor commercial lineage and document exchange/revalidation histories.

## Stage 10 — Group + Charter

Materialize PassengerGroup, group terms, OrderTimeLimit group scopes and ReservationGroupSpace.

## Stage 11 — DCS / Delivery + Disruption

Materialize service delivery observations, DCS evidence and disruption impact/reaccommodation behavior.

## Stage 12 — Interline / Revenue Accounting hardening

Materialize only source-backed internal/settlement/accounting attributes not already required earlier; validate outbound accounting facts end-to-end.

---

# 35. Explicit no-go list

Do not create without a real Stage requirement + source:

- `FareConstruction` aggregate/tree
- local Payment aggregate
- Refund aggregate
- Exchange aggregate
- DCS aggregate in Ordering
- Disruption root in Ordering
- RevenueAccounting ledger in Ordering
- generic JSON service metadata
- generic beneficiary/coverage graph
- generic programmable policy/workflow engine
- invented ATPCO rule/tariff/routing facts
- invented supplier status mappings
- invented `PricingUnitKind` values presented as source truth
- Order-wide `TimeToLive` that mixes provider hold and ticketing/payment rules
- silent mutation of historical sold itinerary/document snapshots

---

# 36. Review of current Stage 2 HEAD `3e130e0` — final product/domain verdict

## 36.1 Accepted

### Reservation aggregate grain — ACCEPT

`FulfillmentReservation` per logical provider operation and `ReservationUnit` covering 1..N OrderServices is the correct direction for airline + multi-supplier reservation.

### ProviderInteraction — ACCEPT

Latest implementation now persists:

- Task + Attempt identity
- provider key
- interaction type/sequence
- idempotency/correlation
- exact request + hash
- response + hash
- provider operation ref/status/error/timestamps

This is better than v1's separate ProviderInteraction aggregate because it keeps the useful evidence while ownership stays inside FulfillmentTask.

### Exact replay — ACCEPT

Unknown replay can use the original persisted request instead of rebuilding it from mutable current Order facts. One attempt can contain read-back + replay interactions.

### PNR / multi-provider / reservation state — ACCEPT

- PNR is host identity, separate from HoldId/unit refs.
- no automatic cross-provider compensation.
- definitive retry creates new reservation identity.
- Unknown replay preserves original operation identity.

## 36.2 Required corrections before `STAGE_2_CLOSED`

### C1 — `ReservationValidationTimeLimit` is not hard Order expiry

Current code can expire the whole Order when all latest validation limits have elapsed and can reject a new reservation solely because the previous operation's validation limit passed.

**Required domain behavior:**

- it is validity of that AirPrice validation evidence;
- stale validation blocks Confirm/Issue until refreshed;
- a new reserve after terminal reservation must rerun validation;
- the Order remains reservable while the hard ticketing deadline is still open;
- only the hard accepted ticketing/order deadline may terminally expire the initial unissued Order.

### C2 — PricingUnit source vocabulary is still not canonical

Current Ordering enum contains `ThroughOneWay` and `SectorSum`, while the repository already contains a different AirOffer source vocabulary including `RoundTripFromOneWays`.

**Required final shape:**

```text
OrderFarePricingUnit.SourceKind: string       // exact source
OrderFarePricingUnit.SemanticType: FarePricingUnitType
```

Do not modify a captured production payload to invent missing component/bound identity. Resolve topology from actual coupon fare references/source identities where unambiguous; otherwise mark the unsupported source shape `BLOCKED_SOURCE`.

### C3 — provider auto-expiry is source-gated

Current FlightFlow capability sets `ExpiresAutomatically = true`.

This is valid only if FlightFlow contract/team authority explicitly guarantees that `ExpiresAt` means authoritative automatic resource expiry. Otherwise local clock merely makes the hold ineligible and should trigger reconciliation/release; it does not prove remote resource state.

### C4 — FlightFlow INF + Revenue remain provider-contract gates

Domain is frozen:

- lap infant has Traveller + Air Service;
- lap infant consumes no independent seat;
- infant Air Service belongs to parent ReservationUnit.

Wire behavior (`INF omitted` vs `INF passenger without seat`) must come from FlightFlow authority.

`Revenue=0m` may remain adapter-only only if FlightFlow confirms zero is neutral. Never substitute customer price.

## 36.3 Closure policy

These four corrections/source gates are the final Stage-2 product-domain closure set. No new Stage-2 concept may be introduced after this Master approval unless a real provider/source contract contradicts a frozen decision.

GitHub currently exposes no CI/workflow execution for this HEAD, therefore this review verifies source behavior/tests by inspection but does not claim the full suite was executed green.

---

# 37. Benchmark traceability

Public sources used as semantic benchmarks (not as proprietary internal design specifications):

## IATA

- ONE Order / Fulfilment with Orders: https://www.iata.org/en/programs/airline-distribution/retailing/one-order/
- Reservations Handbook: https://www.iata.org/en/publications/manuals/reservations-handbook/
- Finance, Retailing & Distribution Manuals: https://www.iata.org/en/publications/manuals/finance-retailing-distribution/
- Revenue Accounting Manual: https://www.iata.org/en/publications/manuals/revenue-accounting-manual/
- Ticketing Handbook 2026: https://www.iata.org/en/store/publications/manuals-standards-and-regulations/ticketing-handbook-thb__thb/
- Business Reference Architecture / Financial Management: https://www.iata.org/reference-architecture
- AIDM Service (25.2): https://airtechzone.iata.org/aidm_model/25.2/EARoot/EA6/EA2/EA2/EA3/EA11503.htm
- AIDM Passenger Group: https://airtechzone.iata.org/aidm_model/24.1/EARoot/EA6/EA1/EA2/EA9/EA12051.htm
- AIDM Service Delivery Status: https://airtechzone.iata.org/aidm_model/24.1/EARoot/EA6/EA1/EA1/EA2/EA11498.htm
- IATA EMD definition: https://portal.iata.org/faq/articles/en_US/FAQ/What-is-an-Electronic-Miscellaneous-Document-EMD-1415811054748
- IATA charter definitions: Reference Manual for Audit Programs Ed.14.

## ATPCO

- Category 5 Advance Reservations and Ticketing: https://faremanager.atpco.net/atpapps/fmhelp/mergedProjects/Rules/Advance_Reservations_and_Ticketing_%285%29.htm
- Optional Services overview: https://faremanager.atpco.net/atpapps/fmhelp/mergedProjects/Optional%20Services/what_are_optional_services.htm
- Optional Services provisions: https://faremanager.atpco.net/atpapps/fmhelp/mergedProjects/Optional%20Services/View_Provisions_Details.htm
- Voluntary Change/Refund standards background: ATPCO Category 31 / Category 33 public material.

## Amadeus

- Amadeus Nevio: https://amadeus.com/en/airlines/products/nevio
- Airline product portfolio / Altéa: https://amadeus.com/en/airlines/products/all
- Group bookings in Altéa training catalogue.
- Altéa Inventory-DCS Links / Departure Control training.

## Sabre

- SabreMosaic Offer/Order/Settlement/Delivery public description.
- Sabre Offers & Orders API user guide.
- SabreSonic / Group Optimizer public product material.
- Sabre passenger-service/departure-control public material.

---

# 38. Coding Agent contract

For every Stage, the Agent receives:

1. this Master ADR/PRD as read-only domain authority;
2. one Stage-specific implementation prompt selecting the exact subset to materialize;
3. actual current provider/source contracts.

The Agent must not:

- add an entity/field/status because a future scenario might need it unless the Stage prompt explicitly authorizes it;
- rename/remove Master concepts on its own;
- infer missing provider contract behavior from donor repos;
- copy v2/v3 wholesale;
- reinterpret a source identifier/status by array order or convenient local semantics;
- turn technical timeout into business rejection;
- replace source-gated fields with defaults that change business meaning.

When implementation exposes a domain gap, it reports the exact missing business decision/source contract and stops that scenario. It does not invent the answer.

---

# 39. Final ADR summary

The final strategic model is:

```text
Order = commercial + historical truth
FulfillmentReservation = supplier resource truth
FulfillmentTask = external effect/recovery evidence
PaymentCoverage = external payment evidence inside Order
ETKT / EMD = accountable document truth
DocumentStock = document-number authority
DCS/Disruption = external operational authority, recorded as sourced Order evidence
Revenue Accounting = downstream accounting authority consuming immutable Order/document/payment/delivery facts
```

The design intentionally keeps v1's simplicity, v2's proven airline document/servicing semantics, v3's correct time/Unknown/evidence distinctions, and discards the speculative abstraction depth of v2/v3.

The implementation remains Stage-by-Stage. The full domain model is known now so later Stages extend the model additively instead of rediscovering ownership or replacing earlier aggregate boundaries.

---

# 13. Aggregate: FulfillmentReservation

`FulfillmentReservation` is the durable Ordering-side truth for a reservation/resource outcome owned by one fulfillment provider. It is not a provider request log and it is not the commercial Order.

## 13.1 Root fields

| Field | Type | Null | Stage | Meaning / invariant |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Logical reservation-operation identity. A definitive terminal retry creates a new reservation. Unknown retry keeps this identity. |
| `OrderId` | `long` | No | NOW | Parent commercial Order. |
| `FulfillmentProviderKey` | `string` | No | NOW | Provider responsible for the resource. |
| `Mode` | `ReservationMode` | No | NOW | `HoldThenConfirm`, `ImmediateConfirm`, or `None`. `None` does not create a reservation. |
| `IdempotencyKey` | `string` | No | NOW | Stable mutation identity for this logical reservation. |
| `CorrelationReference` | `string` | No | NOW | Technical/business correlation; never substitutes for OrderReference/PNR/provider refs. |
| `ProviderOperationRef` | `string?` | Yes | NOW | Provider operation/resource reference such as FlightFlow HoldId. Immutable once known. |
| `Status` | `FulfillmentReservationStatus` | No | NOW | Business reservation summary derived from unit/resource evidence. |
| `RequestedExpiresAt` | `DateTimeOffset?` | Yes | NOW | Exact requested provider hold expiry persisted as part of external intent. Never recomputed on Unknown replay. |
| `ReservationValidationTimeLimit` | `DateTimeOffset?` | Yes | NOW | Validity of the AirPrice reservation-validation decision used for this attempt. It is not a hard Order expiry. |
| `ExpiresAt` | `DateTimeOffset?` | Yes | NOW | Provider-returned resource expiry/valid-until. Provider truth for current reservation. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Local creation time. |
| `LastObservedAt` | `DateTimeOffset` | No | NOW | Last authoritative/local evidence application time. |

### Status vocabulary

```text
Pending
Held
Confirmed
Waitlisted
Rejected
Released
Expired
Cancelled
Mixed
Unknown
```

`Unknown` means the business truth is unresolved. It does not mean failed.

## 13.2 `ReservationUnit`

One ReservationUnit is one provider-operational unit. It may cover more than one OrderService when the provider treats them atomically, e.g. Air + Seat, or an adult unit also carrying a lap-infant service.

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | NOW |
| `FulfillmentReservationId` | `long` | No | NOW |
| `UnitCorrelationKey` | `string` | No | NOW |
| `OrderServiceIds` | `IReadOnlyCollection<long>` | No | NOW |
| `Status` | `ReservationMemberStatus` | No | NOW |
| `ProviderUnitRef` | `string?` | Yes | NOW |
| `RawStatusCode` | `string?` | Yes | NOW |
| `ObservedSeat` | `string?` | Yes | NOW |
| `ProviderValidUntil` | `DateTimeOffset?` | Yes | FUTURE / SOURCE-GATED |

A `ReservationUnit` is not a generic coverage graph. The list of service IDs is the explicit business coverage of that provider unit.

## 13.3 `ReservationGroupSpace` — future group capacity child

Group capacity can exist before passenger names and therefore cannot be faked as passenger-specific OrderServices.

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `FulfillmentReservationId` | `long` | No |
| `PassengerGroupId` | `long` | No |
| `UnitCorrelationKey` | `string` | No |
| `RequestedPassengerQuantity` | `int` | No |
| `ReservedPassengerQuantity` | `int` | No |
| `ProviderGroupRef` | `string?` | Yes |
| `CabinClassId` | `long?` | Yes |
| `RbdId` | `long?` | Yes |
| `Status` | `ReservationMemberStatus` | No |
| `RawStatusCode` | `string?` | Yes |
| `ProviderValidUntil` | `DateTimeOffset?` | Yes |

Materialize only for a real group-capacity provider contract.

## 13.4 Reservation lifecycle invariants

1. New logical Reserve -> `Pending` -> `Held/Confirmed/Waitlisted/Rejected/Unknown`.
2. `Held -> Confirmed` only through provider confirmation evidence.
3. `Held -> Released` only through definitive release evidence, or certified provider automatic-expiry semantics for `Expired`.
4. `Unknown` retry uses the same `FulfillmentReservation`, same idempotency key, and the exact persisted original mutation request.
5. Definitive `Rejected/Released/Expired/Cancelled` followed by a new customer/business retry creates a new FulfillmentReservation and re-runs current eligibility/validation.
6. Held/Confirmed services are never re-reserved as a new resource without first reaching a conclusive terminal state or provider-supported change/split operation.
7. Partial/mixed provider truth is retained. It is never collapsed to a boolean.
8. Multi-provider reservation operations are independent; one provider failure does not automatically release another provider's successful resource unless an explicit business compensation policy says so.
9. `ExpiresAt`, `ReservationValidationTimeLimit`, and `LastTicketingDate` remain distinct facts.
10. A stale validation decision blocks Confirm/Issue until refreshed; it does not permanently expire the Order while the hard ticketing deadline remains open.

## 13.5 Time decision for airline reservation

For a new FlightFlow hold, the requested expiry may not exceed any currently applicable known constraint:

```text
RequestedExpiresAt = earliest(
    AirPrice reservation-validation validity when supplied,
    hard accepted ticketing deadline when supplied,
    provider maximum-hold constraint when the provider contract supplies one
)
```

This formula bounds the *requested hold*. It does not merge the source clocks.

Provider returned `ExpiresAt` becomes the current resource deadline. If it is earlier, the resource ends earlier. If it is later than the pricing-validation validity, the resource can still physically exist but cannot be Confirmed/Issued until validation is refreshed.

### Correct behaviour when validation expires before the hold

```text
AirPrice validation expires
    ↓
Reservation still physically Held
    ↓
Confirm/Issue = blocked
    ↓
Run fresh AirPrice validation
    ├─ valid → continue using the held resource if provider contract permits
    └─ invalid/reprice required → service/order servicing flow decides release/reprice
```

Do **not** convert the whole Order to `Expired` only because one old validation result expired.

### Correct behaviour when provider hold expires first

```text
Provider hold expires
    ↓
FulfillmentReservation = Expired (only with certified automatic-expiry semantics/evidence)
    ↓
Order remains commercially alive if ticketing/order deadline is still open
    ↓
A new Reserve may run with a new reservation identity and fresh AirPrice validation
```

---

# 14. Aggregate: FulfillmentTask

`FulfillmentTask` owns execution/recovery of an external effect. It does **not** own the business resource state; that belongs to FulfillmentReservation, documents, payment owner, etc.

## 14.1 Final root shape

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Execution task identity. |
| `OrderId` | `long` | No | NOW | Commercial Order. |
| `BusinessOperationId` | `long?` | Yes | FUTURE | Servicing operation identity when a later servicing Stage materializes it as an application-level operation reference, not an aggregate. |
| `FulfillmentReservationId` | `long?` | Yes | NOW→GENERALIZE LATER | Required for reservation tasks today; nullable for document/provider tasks later. |
| `TaskType` | `OrderFulfillmentTaskType` | No | NOW | Typed external effect. |
| `FulfillmentProviderKey` | `string` | No | NOW | Adapter/provider. |
| `IdempotencyKey` | `string` | No | NOW | Stable effect identity. |
| `CorrelationReference` | `string` | No | NOW | Stable correlation. |
| `Status` | `OrderFulfillmentStatus` | No | NOW | Pending/InProgress/Succeeded/Failed/Unknown/etc. execution state. |
| `AttemptCount` | `int` | No | NOW | Number of attempts. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Creation time. |
| `CompletedAt` | `DateTimeOffset?` | Yes | NOW | Terminal completion time. |
| `LastFailureKind` | `FulfillmentFailureKind?` | Yes | NOW | Latest classified failure. |
| `LastFailureReason` | `FulfillmentFailureReason?` | Yes | NOW | Latest reason. |
| `LastError` | `string?` | Yes | NOW | Bounded diagnostic text. |

The aggregate remains one concept across Reserve, Confirm, Release, Issue, EMD issue, provider ancillary fulfilment and document recovery. Stage 4 must generalize the reservation-only reference; it must not create a second parallel task aggregate.

## 14.2 `FulfillmentTaskTarget`

Current Stage 2 target is `ReservationUnitId`. Final typed execution target is:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `FulfillmentTaskId` | `long` | No |
| `TargetKind` | `FulfillmentTargetKind` | No |
| `TargetId` | `long` | No |
| `Action` | `OrderFulfillmentTargetAction` | No |

`FulfillmentTargetKind` is a closed enum, not a generic arbitrary graph:

```text
ReservationUnit
OrderService
ElectronicTicket
TicketCoupon
ElectronicMiscDocument
EmdCoupon
DocumentStockAllocation
PassengerGroupSpace
```

Materialize new target kinds only with the Stage that needs them.

## 14.3 `FulfillmentTaskAttempt`

Keep:

`Id, FulfillmentTaskId, AttemptNumber, StartedAt, CompletedAt?, Outcome?, FailureKind?, FailureReason?, Error?`.

One attempt can contain 1..N ProviderInteractions, e.g. read-back then same-key mutation replay.

## 14.4 `ProviderInteraction`

Current R4 shape is the correct final direction:

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `FulfillmentTaskId` | `long` | No |
| `FulfillmentTaskAttemptId` | `long` | No |
| `AttemptNumber` | `int` | No |
| `Sequence` | `int` | No |
| `FulfillmentProviderKey` | `string` | No |
| `InteractionType` | `ProviderInteractionType` | No |
| `IdempotencyKey` | `string?` | Yes only for read-only interactions |
| `CorrelationReference` | `string?` | Yes |
| `RequestPayload` | `string` | No |
| `RequestHash` | `string` | No |
| `ResponsePayload` | `string?` | Yes |
| `ResponseHash` | `string?` | Yes |
| `ProviderOperationRef` | `string?` | Yes |
| `Status` | `ProviderInteractionStatus` | No |
| `StartedAt` | `DateTimeOffset` | No |
| `CompletedAt` | `DateTimeOffset?` | Yes |
| `ProviderStatusCode` | `int?` | Yes |
| `Error` | `string?` | Yes |

Business-normalized provider outcome does not live only in the interaction log; it is applied to the owner aggregate. Interaction evidence remains durable for recovery/audit.

---

# 15. Order-side payment domain

Payment transaction lifecycle belongs to JetPay / the checkout orchestrator. Ordering keeps only payment evidence necessary to decide whether a specific accepted commercial version may proceed.

Public modern airline platforms also keep Payment as a separate capability from Order/Delivery; Amadeus Nevio explicitly separates Order Management, Payment Management and Delivery Management, while SabreMosaic describes Offer, Order, Settle and Deliver capability families.

## 15.1 Canonical checkout ACL semantics

The Ordering-facing contract is provider-neutral:

```text
CreatePaymentIntent
GetPaymentIntent
CapturePaymentIntent
CancelPaymentIntent
```

Standard semantic lifecycle:

```text
Created
RequiresCustomerAction
Processing
Authorized
PartiallyCaptured
Captured
Failed
Cancelled
Expired
```

Supported capture modes:

```text
Automatic
Manual
```

Stage 3 baseline may use Automatic capture only. Manual authorization is not Issue authority until the airline explicitly approves that business policy.

## 15.2 `OrderPaymentCoverage` — Order child

| Field | Type | Null | Meaning |
|---|---|---:|---|
| `Id` | `long` | No | Local evidence identity. |
| `OrderId` | `long` | No | Parent Order. |
| `PaymentIntentId` | `string` | No | Checkout orchestrator identity. |
| `CommercialVersion` | `int` | No | Exact commercial version this payment intent covers. |
| `Purpose` | `PaymentCoveragePurpose` | No | Sale/AddService/ExchangeAdditionalCollection/GroupDeposit/FinalPayment/Other. |
| `RequestedAmount` | `decimal` | No | Amount requested for this intent. |
| `CurrencyId` | `int` | No | Currency; no local FX guess is allowed. |
| `CaptureMode` | `PaymentCaptureMode` | No | Automatic/Manual. |
| `Status` | `CheckoutPaymentStatus` | No | Normalized orchestrator status. |
| `AuthorizedAmount` | `decimal` | No | Latest authoritative authorized amount. |
| `CapturedAmount` | `decimal` | No | Latest authoritative captured amount. |
| `AppliedAmount` | `decimal` | No | Amount currently applicable to this Order/commercial version; prevents double-counting stale/paid-unapplied funds. |
| `AuthorizationExpiresAt` | `DateTimeOffset?` | Yes | Provider authorization expiry when supplied. |
| `AppliesToCurrentOrder` | `bool` | No | False when payment is paid-but-unapplied or belongs to superseded commercial facts. |
| `CreatedAt` | `DateTimeOffset` | No | First observation. |
| `UpdatedAt` | `DateTimeOffset` | No | Latest observation. |

### Final multiple-payment rule

The *final domain* supports multiple payment intents because split tender, vouchers, group deposits/final payments and later servicing are legitimate airline scenarios. Stage 3 may initially allow only one active intent per commercial version.

Coverage is calculated only from distinct applicable intents. An authorization later captured is one intent, not two amounts.

```text
CapturedCoverage = Σ AppliedAmount for applicable Captured/PartiallyCaptured intents
```

Authorization-only coverage is usable only under an explicitly approved airline issue policy and before `AuthorizationExpiresAt`.

## 15.3 `RefundSettlementEvidence` — future Order child

Commercial/document refund and money movement are separate facts.

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `RefundChangeId` | `long` | No |
| `PaymentIntentId` | `string?` | Yes |
| `RefundTransactionId` | `string` | No |
| `Amount` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `Status` | `RefundSettlementStatus` | No |
| `ProviderReference` | `string?` | Yes |
| `CompletedAt` | `DateTimeOffset?` | Yes |

This is evidence only. JetPay owns the refund transaction.

---

# 16. Aggregate: ElectronicTicket

ElectronicTicket is retained as a real aggregate while AeroTech still operates in the hybrid PNR/ETKT/EMD world. This is compatible with a future ONE Order transition; IATA ONE Order aims to phase out those legacy records, but current airline interoperability still requires them.

## 16.1 Root fields

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OriginalOrderId` | `long` | No |
| `CurrentServicingOrderId` | `long` | No |
| `TravellerId` | `long` | No |
| `OperationId` | `long` | No |
| `DocumentNumber` | `string` | No |
| `IssuerCarrierId` | `long` | No |
| `ValidatingCarrierId` | `long?` | Yes / source-gated |
| `IssuingOfficeId` | `long?` | Yes |
| `Authority` | `DocumentAuthority` | No |
| `IssuedAt` | `DateTimeOffset` | No |
| `VoidDeadline` | `DateTimeOffset?` | Yes |
| `IssuedTotal` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `StatusSummary` | `ElectronicTicketStatus` | No |
| `DocumentVersion` | `int` | No |
| `ProviderReference` | `string?` | Yes |
| `PredecessorElectronicTicketId` | `long?` | Yes |
| `PredecessorExchangeOperationId` | `long?` | Yes |

Status vocabulary stays:

```text
Issued
PartiallyUsed
Used
Voided
Exchanged
Refunded
Suspended
```

## 16.2 `TicketCoupon`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `TicketId` | `long` | No |
| `CouponNumber` | `int` | No |
| `OrderServiceId` | `long` | No |
| `CurrentOrderServiceId` | `long` | No |
| `JourneySegmentId` | `long` | No |
| `OrderFareComponentId` | `long?` | Yes / future-source link |
| `IssuedSegment` | `IssuedSegmentSnapshot` | No |
| `FareBasisSnapshot` | `string?` | Yes |
| `BookingClassSnapshot` | `string?` | Yes |
| `CabinClassIdSnapshot` | `long?` | Yes |
| `IssuanceValue` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `FinancialStatus` | `TicketCouponFinancialStatus` | No |
| `ControlStatus` | `TicketCouponControlStatus` | No |
| `ProviderCouponStatusCode` | `string?` | Yes |
| `PredecessorTicketCouponId` | `long?` | Yes |
| `UsedAt` | `DateTimeOffset?` | Yes / DCS-source |
| `UsageReference` | `string?` | Yes / DCS-source |

`FinancialStatus`:

```text
Open
Used
Void
Exchanged
Refunded
Suspended
```

`ControlStatus`:

```text
Local
External
ReleasePending
Unknown
```

Operational/DCS evidence can drive a coupon to Used only through an authoritative source mapping. A stale local Order status is not enough.

## 16.3 `IssuedSegmentSnapshot`

Minimum final historical ticket snapshot:

```text
MarketingAirlineId
OperatingAirlineId?
FlightNumber
OriginAirportId
DestinationAirportId
DepartureDateTime
ArrivalDateTime
BookingClass?
CabinClassId?
```

It is immutable even after schedule changes/reaccommodation; successor documents/services carry new snapshots.

## 16.4 `DocumentPriceLink`

Each ticket/EMD coupon retains attribution back to accepted pricing:

```text
Id
DocumentId
CouponId?
PricingLineId
PricingAllocationId?
AttributedValue
CurrencyId
```

This is mandatory for refund/exchange history and revenue-accounting traceability.

---

# 17. Aggregate: ElectronicMiscDocument

IATA defines EMD as the standard value document for optional/ancillary services and distinguishes EMD-A (associated to an ET) from EMD-S (standalone). AeroTech keeps that distinction.

## 17.1 Root fields

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OriginalOrderId` | `long` | No |
| `CurrentServicingOrderId` | `long` | No |
| `TravellerId` | `long?` | Yes |
| `OperationId` | `long` | No |
| `DocumentNumber` | `string` | No |
| `Type` | `ElectronicMiscDocumentType` | No |
| `ReasonForIssuanceCode` | `string` | No |
| `IssuerCarrierId` | `long` | No |
| `IssuingOfficeId` | `long?` | Yes |
| `Authority` | `DocumentAuthority` | No |
| `IssuedAt` | `DateTimeOffset` | No |
| `IssuedTotal` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `ProviderReference` | `string?` | Yes |
| `StatusSummary` | `ElectronicMiscDocumentStatus` | No |
| `DocumentVersion` | `int` | No |

Types:

```text
Associated    // EMD-A
Standalone    // EMD-S
```

## 17.2 `EmdCoupon`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicMiscDocumentId` | `long` | No |
| `CouponNumber` | `int` | No |
| `Purpose` | `EmdCouponPurpose` | No |
| `ReasonForIssuanceSubCode` | `string` | No |
| `OrderServiceId` | `long?` | Yes |
| `PricingLineId` | `long?` | Yes |
| `ExternalValueReference` | `string?` | Yes |
| `AssociatedTicketCouponId` | `long?` | Yes |
| `IssuanceValue` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `Status` | `EmdCouponStatus` | No |
| `PredecessorElectronicMiscDocumentId` | `long?` | Yes |
| `PredecessorDocumentNumber` | `string?` | Yes |
| `PredecessorCouponNumber` | `int?` | Yes |

Purpose vocabulary:

```text
Service
Fee
Deposit
ResidualValue
```

Status:

```text
OpenForUse
Void
Refunded
Exchanged
```

Association/disassociation/reassociation history is append-only and required for EMD-A across ticket reissue.

## 17.3 Optional service source semantics

ATPCO Optional Services source may supply Service Sub Code, service type, Group/SubGroup, commercial name, EMD type, RFIC and SSR code. These belong to accepted ancillary/service-definition snapshots, not to hard-coded Ordering taxonomy. No code is invented when the source is absent.

---

# 18. Aggregate: DocumentStock

The current v2 donor concept is retained for local document authority.

## 18.1 Root

```text
Id: long
OwnerAirlineId: long
AirlineOfficeId: long?
DocumentType: string
Prefix: string
SerialWidth: int
CheckDigitProfile: string
RangeFrom: long
RangeTo: long
NextNumber: long
Status: DocumentStockStatus
Allocations: DocumentStockAllocation[]
```

## 18.2 `DocumentStockAllocation`

```text
Id: long
DocumentStockId: long
OperationId: long
DocumentRole: string
Serial: long
DocumentNumber: string
State: Reserved | Issued | Retired
AllocatedAt: DateTimeOffset
SettledAt: DateTimeOffset?
```

Rules:

1. Allocation is idempotent for the same operation + document role.
2. Issued/retired numbers are never recycled.
3. Random ticket numbering is forbidden.
4. External issuer authority may bypass local stock allocation, but its document number/provider evidence must still be retained.

---

# 19. Delivery / DCS domain

DCS owns operational check-in, boarding and consumption facts. Ordering keeps source observations because delivery affects servicing and revenue-accounting eligibility, but it does not become the DCS.

IATA AIDM explicitly separates Service delivery status and delivery milestones; the standard status vocabulary includes Ready to Proceed, Ready to Deliver, In Progress, Delivered, Not Claimed, Failed to Deliver, Unable to Deliver, Expired, Suspended and Removed.

## 19.1 `OrderDeliveryObservation` — append-only Order child

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `OrderServiceId` | `long` | No |
| `TravellerId` | `long` | No |
| `SegmentId` | `long?` | Yes |
| `TicketCouponId` | `long?` | Yes |
| `EmdCouponId` | `long?` | Yes |
| `Status` | `ServiceDeliveryStatus` | No |
| `MilestoneCode` | `string?` | Yes |
| `RawStatusCode` | `string?` | Yes |
| `SourceSystem` | `string` | No |
| `SourceReference` | `string` | No |
| `SourceVersion` | `string?` | Yes |
| `OperationalSeatNumber` | `string?` | Yes |
| `ObservedAt` | `DateTimeOffset` | No |
| `ReceivedAt` | `DateTimeOffset` | No |

Canonical `ServiceDeliveryStatus` mirrors the IATA semantic vocabulary:

```text
ReadyToProceed
ReadyToDeliver
InProgress
Delivered
NotClaimed
FailedToDeliver
UnableToDeliver
Expired
Suspended
Removed
Unknown
```

`Unknown` is AeroTech's safe mapping when the raw provider/DCS status is not yet semantically mapped.

### DCS rules

1. Check-in/boarding/seat changes do not overwrite the accepted sale snapshot in `OrderSegment` or `OrderSeatService`.
2. An operational seat assignment/reassignment is stored in delivery observation and current delivery projection.
3. `NotClaimed`/no-show is source evidence and may affect refund/exchange eligibility; it does not itself calculate the penalty/refund.
4. `InProgress` may block destructive financial servicing until authoritative resolution, consistent with IATA delivery semantics.
5. `Delivered`/authoritative use evidence can update TicketCoupon/EMD coupon usage/control through a defined mapping.
6. Baggage DCS/logistics details belong to baggage/delivery systems; Ordering retains only facts needed for ordered-service delivery/accounting and document state.

---

# 20. Disruption domain

Disruption Management is not an Ordering aggregate. IATA's Business Reference Architecture describes it as a capability that recognizes disruption, finds alternatives and **calls Order Management** to apply individual Order changes. This is the boundary AeroTech adopts.

## 20.1 `OrderDisruptionImpact` — Order evidence child

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `SourceDisruptionRef` | `string` | No |
| `SourceSystem` | `string` | No |
| `EventType` | `DisruptionEventType` | No |
| `RawEventCode` | `string?` | Yes |
| `AffectedSegmentId` | `long?` | Yes |
| `AffectedOrderServiceIds` | `IReadOnlyCollection<long>` | No |
| `OccurredAt` | `DateTimeOffset?` | Yes |
| `ObservedAt` | `DateTimeOffset` | No |
| `ResolvedByChangeId` | `long?` | Yes |

`DisruptionEventType`:

```text
FlightCancellation
ScheduleChange
Delay
FlightNumberChange
AirportChange
EquipmentChange
Misconnection
Other
```

The raw source code is retained whenever mapped to `Other` or when provider semantics matter.

## 20.2 Involuntary servicing

A disruption solution is applied as a normal `OrderChange` with:

```text
IsInvoluntary = true
SourceReference = disruption/decision reference
ReasonCode = source reason
WaiverCode = source-approved waiver when supplied
```

Resulting replacement Services/Pricing/FareTopology/Reservations/Documents use the same normal domain model as voluntary servicing. There is no second "disruption order model".

Rules:

1. Operational schedule events never silently overwrite sold itinerary history.
2. Accepted reaccommodation creates successor service facts and preserves predecessor lineage.
3. Alternative selection/price/waiver must come from Offer/AirPrice/disruption authority; Ordering does not invent a free alternative.
4. Customer may accept protection, select another allowed alternative, or request refund when source policy allows it.
5. Interline responsibility and settlement values remain explicit source facts.

---

# 21. Revenue Accounting / Order Accounting boundary

Revenue Accounting is a downstream financial authority, not an Ordering aggregate. IATA's current Offers & Orders financial architecture expects real/near-real-time interchange between Order Management and accounting capabilities. Service is the lowest operational/accounting detail; delivery confirmation, principal/agent role, partner cost and disruption liabilities matter to recognition.

## 21.1 Facts Ordering must preserve

### Sale/commercial facts

- Order/OrderItem/OrderService identities and lineage.
- customer total and full append-only PricingLines/Allocations;
- fare/tax/fee/surcharge/discount/commission categories and source codes/references;
- tax country/station attribution when source supplies it;
- historical exchange-rate snapshot;
- SalesContext, office/agency/corporate identity when supplied;
- marketing/operating/responsible/validating carrier facts when supplied.

### Payment/settlement facts

- payment intent reference and captured/applied amount evidence;
- refund settlement evidence;
- seller/partner settlement reference when supplied;
- internal value/settlement amount for partner/interline services when supplied.

### Document facts

- ETKT/EMD number, issuer, coupon linkage, issue/void/refund/exchange history;
- coupon price allocation and fare basis/segment snapshots;
- EMD RFIC/RFISC, association and value history.

### Delivery facts

- Service delivery status/milestones;
- consumed/delivered/not-claimed evidence;
- authoritative coupon usage/control updates.

### Servicing/disruption facts

- voluntary vs involuntary change;
- penalty/waiver/source decision;
- predecessor/successor service and document lineage;
- refund/exchange deltas and residual value.

## 21.2 `ServiceAccountingSnapshot` final source-backed fields

The future VO described earlier is extended to:

```text
AccountingRole: Principal | Agent | Unknown
SupplierId: long?
InternalValueAmount: decimal?
InternalValueCurrencyId: int?
SettlementAmount: decimal?
SettlementCurrencyId: int?
SupplierCostAmount: decimal?
SupplierCostCurrencyId: int?
SettlementCarrierId: int?
InterlineSettlementCode: string?
AgreementReference: string?
SourceReference: string?
```

Every monetary field is source-backed. Ordering does not perform interline proration or revenue recognition when the source does not provide the required authority.

## 21.3 Accounting event contract — semantic requirement

The implementation may publish integration events according to repository conventions, but the domain contract requires that downstream Revenue Accounting can reconstruct, by stable IDs:

```text
Accepted sale
Price attribution
Payment/cash evidence
Document issue/disposition
Service delivery/consumption
Refund/exchange/reissue lineage
Partner internal/settlement value
```

No accounting consumer should have to infer fare/tax/commission or coupon/service lineage from free text.

---

# 22. Servicing flows

## 22.1 Cancel — unissued commercial scope

### Command

```text
CancelOrder / CancelServices
```

### Flow

1. Resolve active requested commercial scope and dependency closure.
2. Block while Issue/Confirm external effect is Unknown.
3. Obtain current source authority/penalty decision when required.
4. Release/cancel provider reservations as required.
5. Append `OrderChange(Cancel)`.
6. Mark affected Items/Services `Cancelled`; never delete them.
7. Append penalty/refund pricing lines from source decision.
8. Reverse/refund external payment through JetPay when required; retain settlement evidence.
9. Preserve PNR, provider refs and all history.

`OrderStatus.Cancelled` is only a full-order summary when all active commercial scope is cancelled and no issued value remains requiring document servicing.

## 22.2 Void — issued document reversal

### Command

```text
VoidDocuments
```

Rules:

- within issuer/source void authority/deadline;
- coupons must remain void-eligible and not consumed;
- ETKT/EMD state becomes Void/Voided;
- payment reversal is external evidence, not local payment lifecycle;
- void does not delete original sale/issue history.

## 22.3 Refund

### Commands

```text
QuoteRefund
CommitRefund
ReconcileRefundSettlement
```

Automated refund amount/penalty/eligibility belongs to AirPrice servicing authority using current document/control/delivery facts and ATPCO Cat 33/airline policy.

### Required accepted decision snapshot

`RefundDecisionSnapshot`:

| Field | Type | Null |
|---|---|---:|
| `QuoteId` | `string` | No |
| `SourceSystem` | `string` | No |
| `SourcePricingReference` | `string?` | Yes |
| `CommercialVersion` | `int` | No |
| `DocumentVersionVector` | source-specific stable refs | No |
| `ApprovedCouponIds` | collection | No |
| `ApprovedServiceIds` | collection | No |
| `RefundAmount` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `PenaltyAmount` | `decimal?` | Yes |
| `WaiverCode` | `string?` | Yes |
| `Disposition` | `RefundDisposition` | No |
| `ValidUntil` | `DateTimeOffset?` | Yes |

Do not use initial-sale `IsRefundable` as final automated refund authority.

### Commit

- append `OrderChange(Refund)`;
- append refund/penalty PricingLines;
- mark exact ETKT/EMD coupons Refunded;
- close commercial Services only when no surviving document/service entitlement remains;
- retain `RefundSettlementEvidence` until money movement is known;
- Order history remains queryable after full refund.

## 22.4 Voluntary Exchange / Reissue

### Commands

```text
QuoteExchange
CommitExchange
ReconcileExchange
```

AirPrice servicing authority supplies the accepted old/new pricing decision using ATPCO Cat 31/airline policy.

### `ExchangeDecisionSnapshot`

```text
QuoteId: string
SourceSystem: string
SourcePricingReference: string?
CommercialVersion: int
TargetSelectionReference: string
PredecessorTicketCouponIds: long[]
AffectedOrderServiceIds: long[]
NewOffer/selection reference: string?
AdditionalCollection: decimal
ResidualValue: decimal
PenaltyAmount: decimal?
CurrencyId: int
WaiverCode: string?
ValidUntil: DateTimeOffset?
```

### Commit flow

1. freeze accepted decision/scope;
2. create successor OrderItems/Services/FarePricingUnits/PricingLines under one `OrderChange(Exchange)`;
3. reserve new resource(s);
4. collect additional payment or establish residual/refund disposition;
5. issue successor ETKT/EMD as required;
6. mark predecessor coupons `Exchanged` only after successor document effect is known;
7. preserve predecessor/successor coupon and service lineage;
8. disassociate/reassociate EMD-A coupons according to issuer/source authority;
9. release old reservation/resource only at the safe pivot defined by provider/document authority.

No old accepted sale/document snapshot is overwritten.

## 22.5 Revalidation

### Command

```text
RevalidateTicket
```

Revalidation is allowed only when issuer/AirPrice authority says the change does not require fare reissue/additional collection. It records document revalidation history and current service binding without fabricating a new fare.

## 22.6 Involuntary reaccommodation

Uses the Exchange/Reissue/Revalidation machinery with `OrderChange.IsInvoluntary = true` and disruption-source authority. Voluntary penalties are not inferred; waiver/free-protection rules must be source decisions.

---

# 23. Order split

Split is a commercial ownership operation plus provider/document servicing; it is not a clone-and-forget command.

## 23.1 `OrderLineage` — Order child

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OrderId` | `long` | No |
| `RelatedOrderId` | `long` | No |
| `RelationType` | `OrderLineageType` | No |
| `OperationId` | `long` | No |
| `CreatedAt` | `DateTimeOffset` | No |

Types:

```text
SplitParent
SplitChild
```

## 23.2 Split invariants

1. split scope is whole travellers, not arbitrary detached service rows;
2. lap infant moves with its responsible adult unless a new valid guardian relationship is explicitly provided;
3. all active services/items/pricing/document relations for moved travellers are closed/transferred consistently;
4. source services are `Transferred`, not deleted;
5. child Order starts its own current commercial version/history but keeps lineage to predecessor facts;
6. provider PNR/hold split must use authoritative provider mapping; never infer returned seat/member mapping by array order;
7. ticket/EMD servicing follows document authority; a commercial split alone does not change coupon ownership/control magically;
8. shared/group/partner services require an explicit split rule before the operation is accepted.

---

# 24. Group booking end-to-end

IATA Passenger Group semantics explicitly allow a commercial group with intended passenger quantity and special booking/fare rules. The final domain supports unnamed and named phases.

## 24.1 Group flow

```text
CreateGroupOrder
   PassengerGroup + group OrderItem + itinerary + accepted group terms/price
        ↓
ReserveGroupSpace
   ReservationGroupSpace quantity, provider group ref, deadlines
        ↓
CollectDeposit (when required)
        ↓
Add/ReplacePassengerNames
   materialize OrderTravellers
        ↓
Materialize passenger-specific OrderServices when airline policy requires
        ↓
FinalPayment
        ↓
IssueGroupDocuments / individual ETKT as required
        ↓
Normal DCS / servicing / split / disruption / accounting
```

## 24.2 Group deadline behaviour

Use separate `OrderTimeLimit` entries for:

```text
Deposit
PassengerName
FinalPayment
Ticketing
```

No single group TTL is invented.

## 24.3 Group changes

Supported final scenarios:

- quantity decrease/increase subject to source agreement/capacity authority;
- name addition/substitution subject to group terms;
- split subgroup when itinerary diverges;
- deposit forfeiture/refund from source decision;
- group cancellation;
- group disruption/reaccommodation;
- closed-charter group lifecycle.

---

# 25. Charter end-to-end

## 25.1 Open charter

Behaves like standard retail air transport after the source identifies the segment as `OpenCharter`. Individual passengers can hold/pay/issue/deliver normally.

## 25.2 Closed charter

```text
Charter commercial contract / accepted offer
    ↓
Order + PassengerGroup(ClosedCharter)
    ↓
Group-priced OrderItem(s)
    ↓
ReservationGroupSpace / whole-aircraft or contracted capacity evidence
    ↓
Names later if contract permits
    ↓
Delivery documents according to carrier/regulatory policy
    ↓
DCS / accounting / disruption
```

The airline/customer contract reference is source-backed. Ordering never derives a public fare or per-passenger price when the contract is whole-aircraft/group priced.

---

# 26. Commands — final business command catalogue

This is the domain/use-case catalogue. It does not prescribe controllers, handlers, files or mediator patterns.

## Sale / customer record

```text
CreateOrderFromOffer
CreateGroupOrder
AddGroupPassengers
UpdateTravellerProfile
UpdateContact
AddRemark
```

## Reservation

```text
ReserveOrder
ReserveServices
ReserveGroupSpace
ReconcileReservation
ReleaseReservation
ExtendReservation          // only when provider supports it
ConfirmReservedCapacity    // HoldThenConfirm providers
```

## Payment

```text
CreateCheckoutPayment
RefreshCheckoutPayment
CaptureCheckoutPayment     // manual-capture profile only
CancelCheckoutPayment
RecordPaymentEvidence
```

## Documents / issue

```text
IssueOrder
IssueServices
ReconcileIssue
VoidDocuments
```

## Ancillary

```text
AddAncillaryFromOffer
ReserveAncillary
IssueAncillaryDocument
CancelAncillary
```

## Servicing

```text
CancelOrder
CancelServices
QuoteRefund
CommitRefund
ReconcileRefundSettlement
QuoteExchange
CommitExchange
ReconcileExchange
RevalidateTicket
SplitOrder
```

## Delivery / DCS

```text
RecordDeliveryObservation
RecordDocumentUsageObservation
```

These are source-observation commands, not commands to run DCS operations unless a future DCS provider contract explicitly grants that responsibility.

## Disruption

```text
RecordDisruptionImpact
ApplyInvoluntaryReaccommodation
AcceptDisruptionRefund
```

## Group

```text
ChangeGroupCapacity
AddOrReplaceGroupNames
RecordGroupDepositCoverage
RecordGroupFinalPaymentCoverage
```

No command is implemented merely because it appears here. A Stage prompt explicitly authorizes its subset.

---

# 27. Cross-domain eligibility rules

No single `OrderStatus` is sufficient for servicing eligibility.

## 27.1 Reserve

Requires:

- active reservation-requiring Services or group space;
- provider capability;
- no positive/unresolved conflicting reservation for same scope;
- hard commercial ticketing/order deadline not passed;
- current AirPrice validation for the scope;
- source-complete fare/resource facts.

## 27.2 Confirm capacity

Requires:

- reservation is Held;
- provider mode is HoldThenConfirm;
- resource hold not expired;
- current reservation/fare validation still valid or has been freshly revalidated;
- payment/guarantee gate satisfies airline policy;
- no conflicting Unknown effect.

## 27.3 Issue

Requires:

- active commercial Service scope;
- required capacity Confirmed/committed according to provider profile;
- applicable hard ticketing deadline open;
- current payment coverage/issue authority;
- complete ticket/EMD issuance data;
- no unresolved competing servicing operation;
- document number authority/stock.

## 27.4 Refund/Exchange

Requires:

- authoritative current AirPrice servicing decision;
- eligible coupon/control/delivery scope;
- no Unknown conflicting document/provider effect;
- exact predecessor pricing/document/service lineage;
- actor authority and any source waiver.

## 27.5 Delivery-aware rules

`InProgress` delivery or unknown coupon control can block refund/exchange. `NotClaimed`/NoShow is an input to AirPrice servicing policy, not an automatic local penalty rule.

---

# 28. Canonical state ownership matrix

| Aspect | State owner | Ordering writes |
|---|---|---|
| Commercial service | Order | Active/Replaced/Cancelled/Transferred |
| Capacity reservation | FulfillmentReservation/provider evidence | Pending/Held/Confirmed/etc. |
| Execution attempt | FulfillmentTask | Pending/InProgress/Succeeded/Failed/Unknown |
| Payment | payment orchestrator | evidence snapshot only |
| ETKT coupon | ElectronicTicket/issuer evidence | Open/Used/Void/Exchanged/Refunded/Suspended |
| EMD coupon | ElectronicMiscDocument/issuer evidence | OpenForUse/Void/Refunded/Exchanged |
| Delivery | DCS/delivery provider | IATA-mapped append-only observations |
| Disruption | disruption/operations source | impact evidence + involuntary OrderChange |
| Revenue recognition | Revenue Accounting | never set locally |

This matrix is a guardrail against the old `OrderService.IsFulfilled/IsPaid/IsUsed/...` anti-pattern.

---

# 29. Mandatory end-to-end scenario suite

The complete domain must be able to represent and service all scenarios below without changing aggregate ownership.

## Sale / fare topology

**S01** 1 ADT, one-way, one flight.  
**S02** 1 ADT, connecting itinerary; one FareComponent spans multiple segments.  
**S03** round trip represented by one PricingUnit with outbound+inbound FareComponents.  
**S04** outbound/inbound represented by two independent OneWay PricingUnits.  
**S05** open-jaw/multi-city source topology when AirOffer/AirPrice supplies it.  
**S06** multi-passenger ADT/CHD/INF with exact traveller reconciliation.  
**S07** agency/corporate sale with source agency/PCC/office/commission facts.  
**S08** multi-currency sale with immutable source ROE and tax code/jurisdiction.

## Reservation

**S09** FlightFlow atomic air hold.  
**S10** Air + selected seat in the same reservation unit.  
**S11** multi-passenger/multi-segment reservation.  
**S12** multiple providers reserve independently.  
**S13** Unknown CreateHold -> exact same request/idempotency recovery.  
**S14** definitive rejection -> new reservation/idempotency on later retry.  
**S15** provider hold expires before ticketing deadline -> Order remains re-reservable.  
**S16** AirPrice validation expires while hold remains -> fresh validation before Confirm/Issue, not permanent Order expiry.  
**S17** hard ticketing deadline passes -> no new reserve/confirm/issue; held resources safely released/reconciled.  
**S18** provider returned later expiry never extends fare/ticketing authority.  
**S19** lap infant domain coverage with no independent seat consumption; FlightFlow wire mapping remains provider-contract controlled.  
**S20** waitlist provider scenario when a real provider supports it.

## Checkout/payment

**S21** automatic payment capture of full order.  
**S22** requires-customer-action -> processing -> captured.  
**S23** payment failure leaves reservation truth intact; no implicit release unless deadline/policy says so.  
**S24** authorization-only/manual capture profile.  
**S25** two-tender/split payment coverage in a future JetPay contract.  
**S26** group deposit then final payment.  
**S27** paid-but-unapplied/stale commercial version does not satisfy current issue gate.

## Issue/documents

**S28** one passenger, multi-segment ETKT with ordered coupon sequence.  
**S29** multiple travellers -> independent ticket documents.  
**S30** EMD-A for paid baggage/seat linked to ticket coupon.  
**S31** EMD-S for standalone fee/deposit/residual value.  
**S32** document stock idempotent allocation and no recycling.  
**S33** external issuer timeout/Unknown recovery without duplicate document numbers.  
**S34** issue after payment but before ticketing deadline.  
**S35** late hold-expiry event after ticket issue must not expire ticketed Order.

## Cancel / void / ancillary

**S36** unissued full cancellation with reservation release.  
**S37** partial unissued service cancellation.  
**S38** ticket void within issuer deadline.  
**S39** void no longer eligible -> refund path.  
**S40** add paid baggage after initial issue -> pricing change + payment + EMD.  
**S41** third-party hotel/ground service with independent provider reservation outcome.

## Split

**S42** split one adult and all its services into child Order.  
**S43** lap infant cannot be split away from responsible adult.  
**S44** provider split returns authoritative unit mapping; never array-order inference.  
**S45** documents/EMD association remain coherent across split.

## Refund

**S46** full refund of wholly unused ticket.  
**S47** partial refund of selected open coupon(s).  
**S48** refund paid ancillary/EMD.  
**S49** refund after partial use with source-authoritative calculation.  
**S50** no-show/delivery evidence supplied to AirPrice; Ordering does not invent penalty.  
**S51** commercial refund committed but cash refund Pending; history remains intact.

## Exchange/reissue/revalidation

**S52** date/flight voluntary exchange with additional collection.  
**S53** exchange producing residual value.  
**S54** exchange after one coupon used, remaining coupon scope only.  
**S55** EMD-A disassociate/reassociate to successor ticket coupon.  
**S56** revalidation when source authority confirms no fare change.  
**S57** exchange Unknown after provider/issuer effect -> reconcile before old-resource destructive cleanup.

## Group

**S58** unnamed 20-passenger group with group space and deposit deadline.  
**S59** add names progressively without fake unnamed travellers.  
**S60** name substitution under source group terms.  
**S61** group quantity decrease/increase with capacity authority.  
**S62** final payment/ticketing deadline and individual ticket issue.  
**S63** group splits because itinerary ceases to be homogeneous.

## Charter

**S64** open-charter flight sold to individual retail passenger.  
**S65** closed-charter whole-group/whole-aircraft contract without invented per-passenger public fare.  
**S66** closed-charter named passengers delivered through DCS.

## DCS/delivery

**S67** check-in/ReadyToDeliver/InProgress then Delivered.  
**S68** operational seat reassignment does not overwrite purchased seat history.  
**S69** no-show -> `NotClaimed` delivery observation.  
**S70** service `InProgress` blocks unsafe refund until resolved.  
**S71** coupon Used/Flown source evidence drives document/accounting disposition.  
**S72** ancillary delivered/consumed separately from flight coupon.

## Disruption

**S73** flight cancellation creates impact evidence; no silent segment overwrite.  
**S74** airline protection accepted -> involuntary successor Services/reservation/document servicing.  
**S75** customer rejects protection and takes allowed disruption refund.  
**S76** schedule change revalidation when source says no fare reissue.  
**S77** involuntary reissue with waiver.  
**S78** interline disruption with explicit retailer/supplier responsibility and settlement value.

## Revenue accounting / partner settlement

**S79** sale/cash before delivery remains distinct from revenue recognition.  
**S80** Delivered service provides source event for revenue recognition.  
**S81** agent-role service recognizes commission/internal value rather than gross value downstream.  
**S82** partner service carries source supplier cost/settlement amount.  
**S83** refund/exchange/void sends complete document/pricing/delivery lineage to accounting.  
**S84** interline Order uses upfront internal/settlement value when source supplies it; Ordering does not recompute proration.

A future Stage is not accepted if its design makes any later scenario structurally impossible without replacing the aggregate ownership defined here.

---

# 30. Stage-by-stage materialization plan

The whole domain above is the design authority. Coding remains controlled by stages.

| Stage | Materialize / complete | Explicitly do not materialize yet |
|---|---|---|
| **S1 Create** | Order, travellers, contacts, sold itinerary, Air/Seat services, fare topology, accepted pricing/history | reservation/payment/documents/DCS |
| **S2 Reserve** | RecordLocator, FulfillmentReservation/Unit, reserve/release/recovery, reservation FulfillmentTask/Interaction, deadline semantics | payment/documents/generalized task target beyond reservation |
| **S3 Checkout + Confirm** | OrderPaymentCoverage, provider-neutral payment ACL/mock, ConfirmHeldCapacity | ETKT/EMD |
| **S4 Issue** | ElectronicTicket, TicketCoupon, DocumentPriceLink, DocumentStock, document-oriented FulfillmentTask target generalization | refund/exchange/group/DCS |
| **S5 Cancel/Void** | unissued commercial cancel + document void + settlement evidence | automated refund/exchange |
| **S6 Ancillary/EMD** | Optional-service subclasses needed by real offers, EMD-A/S, paid baggage/seat/fee flows | generic service catalog replication |
| **S7 Split** | OrderLineage, transferred service/item lineage, provider/document split servicing | group split unless group Stage arrived |
| **S8 Refund** | RefundDecisionSnapshot, document refund records, RefundSettlementEvidence | exchange |
| **S9 Exchange/Reissue/Revalidation** | successor commercial topology/pricing, document exchange/revalidation history, EMD reassociation | disruption automation unless source contract exists |
| **S10 Group + Charter** | PassengerGroup, GroupTerms, ReservationGroupSpace, OrderTimeLimit group scopes, charter operation type | DCS internals |
| **S11 Delivery/DCS** | OrderDeliveryObservation and authoritative document-usage mapping | running DCS itself |
| **S12 Disruption** | OrderDisruptionImpact + involuntary servicing flows | crew/aircraft recovery |
| **S13 Accounting/Interline hardening** | source-backed accounting snapshots, settlement/internal values, downstream fact contract | Revenue Accounting ledger/proration engine |

Before each Stage, re-benchmark the Stage from first principles against current IATA/ATPCO/public Amadeus/Sabre behavior and current AeroTech contracts. Historical donor code is never automatically authoritative.

---

# 31. Current Stage 2 closure audit at HEAD `3e130e0`

## 31.1 Approved behaviour

The latest push closes the important ProviderInteraction/recovery gaps:

- exact mutation request is persisted before first dispatch;
- request and response hashes/evidence are durable;
- ProviderInteraction is a child of FulfillmentTask, not a separate aggregate;
- one attempt may contain read-back plus mutation replay;
- Unknown reservation retry replays the original persisted CreateHold request even when mutable Order facts later change;
- same idempotency/correlation identity is retained for Unknown retry;
- AirPrice `ReservationValidationTimeLimit` is now separately persisted;
- the arbitrary global FlightFlow `HoldMinutes` option has been removed;
- reservation-deadline behaviour now has an explicit owner/use-case rather than an ownerless timestamp;
- provider hold expiry earlier than ticketing deadline leaves the Order re-reservable;
- a late reservation expiry cannot turn a Ticketed Order into Expired.

These are approved domain behaviours.

## 31.2 Mandatory final corrections before `STAGE_2_CLOSED`

Only the following are domain blockers; do not start another broad redesign.

### S2-F1 — validation validity is incorrectly treated as permanent Order expiry

Current code/tests make a passed `ReservationValidationTimeLimit`:

- release the existing hold;
- block a new reserve through `EnsureReplaceableAt`;
- and, if all current service validation limits have passed, mark the whole Order `Expired`.

This is not the canonical time model.

**Required behaviour:**

- validation expiry makes that validation evidence stale;
- stale evidence blocks Confirm/Issue;
- a new definitive reservation operation before `LastTicketingDate` MUST be allowed and MUST execute fresh AirPrice validation;
- a still-Held resource may be revalidated before deciding whether it can remain/use current fare;
- only the applicable hard commercial ticketing/order deadline expires the unissued Order.

Remove acceptance expectations that `ReservationValidationTimeLimit` alone permanently expires the Order.

### S2-F2 — fare PricingUnit source vocabulary is still invented

Current Ordering enum still contains `ThroughOneWay` and `SectorSum`, while the tests proving those values are explicitly synthetic. Synthetic fixtures are not source authority.

**Required final shape:**

```text
OrderFarePricingUnit.SourceKind : string        // exact upstream value
OrderFarePricingUnit.SemanticType : FarePricingUnitType

FarePricingUnitType = Unspecified | OneWay | RoundTrip | OpenJaw | CircleTrip | Other
```

Map semantic type only when the real source/contract meaning is known. Preserve unknown/new source terms verbatim in `SourceKind` instead of rejecting or inventing canonical enum members.

This change prevents future exchange/refund/repricing from being coupled to an invented taxonomy.

### S2-F3 — FlightFlow automatic-expiry semantics must be source-backed

Current provider capability declares `ExpiresAutomatically = true`. That is valid only if the real FlightFlow contract guarantees that a hold is authoritatively no longer reserved at `ExpiresAt`.

**Required behaviour:**

- `ExpiresAt` always ends local eligibility to Confirm/Issue;
- local clock may set business reservation status to `Expired` without read-back/event only when FlightFlow explicitly guarantees automatic expiration;
- otherwise the status remains unresolved/ineligible and Ordering must reconcile/release using the provider contract;
- the ambiguous `FlightReservationExpiredEvent.ReferenceId` must not be wired by guessing its identity.

If FlightFlow confirms the lease guarantee, document the contract and keep the current local-expiry transition. If not, change the behavior to fail-closed/reconcile.

### S2-F4 — lap-infant FlightFlow wire behaviour remains source-gated

The final domain decision is frozen:

- lap infant has Traveller + Air Service;
- it consumes no independent seat;
- its service is covered by the responsible adult ReservationUnit.

What FlightFlow expects on the wire (include infant in `Passengers` without a Seat, or omit it) is provider-contract behaviour and cannot be standardized by IATA/Amadeus/Sabre benchmark.

Until FlightFlow confirms it, do not claim the current omission as canonical provider behaviour. The safe production posture is `BLOCKED_SOURCE` for a FlightFlow reservation containing lap infant, while preserving the correct domain model.

`Revenue=0m` is likewise an adapter/source assumption, not a domain field. It must never be replaced by customer fare by inference.

## 31.3 Stage 2 closure rule

After S2-F1..F4 are applied (or the FlightFlow-specific F3/F4 contracts are explicitly documented/confirmed), Stage 2 is **CLOSED**. Future Stages may extend the already-frozen aggregates only in the places authorized by this Master; they may not redesign reservation ownership.

---

# 32. Non-negotiable no-guess rules for Coding Agent

1. Never add a domain field, entity, enum value or relationship because it makes implementation easier.
2. Never use donor v1/v2/v3 as source authority.
3. Never mutate a real provider/source DTO or captured payload to manufacture data needed by a desired model.
4. Preserve unknown upstream vocabulary verbatim plus a safe semantic mapping; do not force it into an invented enum.
5. Never turn `Unknown` external outcome into failure by timeout alone.
6. Never rebuild an Unknown mutation request from current mutable Order state; replay persisted exact intent.
7. Never use `OrderStatus` as the sole eligibility rule.
8. Never collapse Offer expiry, fare/validation validity, ticketing deadline, provider hold expiry, payment expiry and group deadlines into one timestamp.
9. Never overwrite accepted sold itinerary/price/document history with operational/DCS/disruption observations.
10. Never compute refund/exchange amount or fare rules locally when AirPrice/ATPCO pricing authority owns the decision.
11. Never calculate revenue recognition/interline proration in Ordering without an explicit source-owned contract.
12. If a Stage requires a fact absent from authoritative contracts, return `BLOCKED_SOURCE`; do not silently choose a plausible value.

---

# 33. Stage PRD/ADR template derived from this Master

Every future Stage document must contain exactly the domain/product information needed for implementation:

1. Business capability and customer/airline outcome.
2. Real E2E scenarios in scope.
3. Fresh benchmark conclusions for that Stage.
4. Actual AeroTech source/consumer contracts.
5. Ownership decisions.
6. Aggregate roots touched.
7. Entities/VOs/enums materialized **with every field, type, nullability and meaning**.
8. Identity/cardinality rules.
9. Commands/use cases required.
10. Preconditions and invariants.
11. State transitions and lifecycle.
12. Happy-path E2E flow.
13. partial/unknown/timeout/retry/recovery behaviour.
14. historical truth that must survive later servicing/accounting.
15. explicit non-goals/not-yet-materialized types.
16. `BLOCKED_SOURCE` items.
17. acceptance scenarios.
18. Coding Agent guardrail: no new domain concept outside the Stage PRD.

The Stage PRD must **not** prescribe folders, architecture layers, DI style, EF configuration, table/index naming, controllers or mediator structure unless a technical detail directly determines a domain invariant.

---

# 34. Benchmark evidence used for this Master

The domain decisions were checked against public, non-proprietary industry behavior and standards, including:

## IATA

- **Fulfilment with Orders (ONE Order):** single integrated Order coordinating fulfilment, delivery and accounting; gradual transition away from legacy PNR/ETKT/EMD artifacts.
- **Modern Airline Retailing / Offers & Orders:** order-based platforms interoperating with Offer, Delivery and financial systems.
- **IATA AIDM Service:** distinct Service booking/delivery facts; delivery status and milestones are separate concerns.
- **IATA AIDM Service Delivery Status Code:** READY TO PROCEED, READY TO DELIVER, IN PROGRESS, DELIVERED, NOT CLAIMED, FAILED/UNABLE TO DELIVER, EXPIRED, SUSPENDED, REMOVED.
- **IATA Passenger Group:** intended passenger quantity/group name, homogeneous itinerary, special booking/fare rules, tour group/sales allotment examples.
- **IATA Charter definitions:** Open Charter sells seats to the public; Closed Charter is whole-aircraft transport for a defined contracting group.
- **Interlining with Offers & Orders:** Order/payment/sales accounting + Delivery/check-in + revenue recognition and explicit internal/settlement values demonstrated in pilot flows.
- **Business Reference Architecture:** disruption capability calls Order Management to apply customer Order changes; Revenue Accounting consumes up-to-date Order/Service delivery facts.
- **IATA EMD:** EMD-A vs EMD-S and value coupons for optional services.
- **Revenue Accounting Manual / financial architecture:** interline accounting/currency/settlement remains a specialist accounting capability.

## ATPCO

- **Passenger Tariff / fare construction principles:** journey, Pricing Unit and Fare Component are real fare-construction concepts.
- **Category 5 Advance Reservations and Ticketing:** reservation restrictions and ticketing deadlines are distinct; ticketing can be relative to reservation confirmation and/or departure.
- **Category 31 Voluntary Changes / Category 33 Voluntary Refunds:** automated servicing pricing authority belongs to fare/rule processing, not sale-time flags.
- **Optional Services:** service sub-code, type, group/subgroup, commercial name, EMD type, RFIC and SSR relationships are source-defined semantics.

## Amadeus

- **Nevio:** public capability separation between Order Management, Payment Management and Delivery Management; Order is the single source of truth for order processing/servicing while payments and delivery remain dedicated capabilities.
- **Altéa DCS:** check-in/customer acceptance, seat changes, boarding and disruption handling are Delivery/DCS concerns.
- **Disruption/Reaccommodation:** operational disruption is serviced through passenger recovery/rebooking rather than by rewriting original sale history.

## Sabre

- **SabreMosaic:** end-to-end lifecycle capability families across Offer, Order, Settle and Deliver.
- **SabreSonic:** integrated PSS covers reservations/inventory/departure control while those remain distinct business functions.
- **Revenue Integrity TTL:** unticketed reservation time limits can depend on itinerary/segment/flight/booking-class attributes and exist to release inventory; this supports keeping ticketing/resource time facts explicit instead of one arbitrary fixed global TTL.

These references validate business semantics, not internal vendor aggregate/class design.

---

# 35. Approval statement

If Owner approves this Master:

1. It becomes the full-horizon domain authority above Pack v4.6 where the two conflict.
2. Pack v4.6 remains useful as historical Stage sequencing/guardrails, but must not override corrected domain decisions in this document.
3. Stage 1 is accepted as the commercial baseline plus the approved fare-topology correction.
4. Stage 2 closes after only S2-F1..F4 above; no new broad Stage-2 redesign is permitted.
5. Stage 3 PRD must be derived from Sections 13–15 and must not invent payment/domain semantics.
6. Every later Stage is re-benchmarked before implementation, but aggregate ownership and final field destinations in this Master are changed only by an explicit Owner-approved ADR amendment backed by source/industry evidence.

**End of Master Domain ADR/PRD v1.0.**
