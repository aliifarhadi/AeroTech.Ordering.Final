# AeroTech Ordering — Master Airline Domain ADR/PRD v2.0 FINAL — PROJECT AUTHORITY

**Purpose:** Full-horizon domain authority for AeroTech Airline Ordering / PSS  
**Target:** `aliifarhadi/AeroTech.Ordering.Final`  
**Reviewed source HEAD:** `85f48bb652d96ce155c96ee07045223656db1ecb` (`k8s-stg`, `Stage 2- Final`)  
**Implementation policy:** Full domain is designed now; code is materialized stage-by-stage only.  
**Owner decision:** Ordering core continues from Stage 3 without a Payment/JetPay prerequisite. Financial orchestration is integrated later as an additive boundary, not as an intrinsic Ticket/Reservation domain dependency. This document is the full-horizon domain authority; implementation remains deliberately Stage-by-Stage.  
**In-place amendment — Stage 5 R2 Domain Closure (Owner decision, 2026-09-27):** `OrderChange.ReasonText` (§9.1), canonical `ReservationValidationEvidence` (§13.1, §13.6), reservation root status as summary only with unit-scoped Issue eligibility (§27.3), and the derived `FarePricingAtom` pricing-integrity rule (§7.4). This document remains the single authority; no v2.1/v2.2 exists.

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

### Evidence and no-guess doctrine — FINAL

Every business rule, provider capability and future field in this Master is classified by evidence, not by convenience:

- **CURRENT-CODE-VERIFIED** — observed in the reviewed Ordering source and already part of a closed Stage.
- **AEROTECH-CONTRACT-VERIFIED** — supported by an actual upstream/downstream AeroTech contract or provider source.
- **BENCHMARKED-DOMAIN** — required by the airline domain and supported by public IATA/ATPCO/Amadeus/Sabre semantics; value still comes from an AeroTech source when materialized.
- **OWNER-DECISION** — a deliberate product decision made by the Owner where more than one standard industry workflow is valid.
- **SOURCE-GATED** — the destination/meaning is known, but no value may be populated and no provider behavior may be assumed until the source contract exists.
- **CAPABILITY-CONDITIONAL** — behavior depends on a provider capability that must be verified at the Stage that invokes it.

**Absence of a field in the current Ordering adapter is never proof that the provider does not support that capability.** Likewise, a boolean already present in an adapter is not sufficient evidence that the provider guarantees the semantic behavior.

For every external mutation (`Reserve`, `Confirm`, `Release`, `Cancel`, `Issue`, `Void`, partner fulfillment):

1. inspect the real provider contract/source available at that Stage;
2. establish whether an idempotency identity is accepted and what duplicate semantics it guarantees;
3. establish whether authoritative read-back/reconciliation exists;
4. establish whether provider success/failure/expiry is atomic or per-unit;
5. only then choose the recovery branch below.

```text
if authoritative read-back exists:
    reconcile before replay when outcome is ambiguous

if provider contract guarantees safe idempotent replay for this mutation:
    persist exact request + provider idempotency identity and replay the same effect

if neither capability is verified:
    preserve Unknown; do not blind-replay and do not invent remote state
```

A Stage prompt MUST express provider-specific behavior in this conditional form whenever capability evidence is not already authoritative. Coding Agent must report `BLOCKED_SOURCE` rather than infer the missing branch.

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
- Amadeus and Sabre publicly separate retail Order from Payment/Settlement and Delivery/DCS capabilities while integrating them end-to-end. Therefore Payment is not an intrinsic state of Ticket, Reservation or OrderService.
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
| Payment transaction lifecycle | JetPay / checkout orchestrator | **No current dependency.** Future source-backed financial coverage evidence may be projected into Ordering after the JetPay contract is frozen. |
| ETKT / coupon financial-control history | Ordering | `ElectronicTicket` |
| EMD / coupon association/value history | Ordering | `ElectronicMiscDocument` |
| Document number stock | Ordering / issuer | `DocumentStock` |
| Check-in/boarding/operational consumption | DCS / delivery provider | sourced delivery observations only |
| Flight disruption operational fact | Operations / DCS / disruption owner | `OrderDisruptionImpact` evidence only |
| Automated refund/exchange pricing | AirPrice servicing | quote/decision provenance + committed pricing/document history |
| Revenue recognition / accounting ledger | Revenue Accounting | facts emitted from Order/Documents/Delivery plus JetPay/Ledger financial facts; no RA ledger in Ordering |
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

`OrderStatus` is a useful summary, not the sole source of eligibility. Core Ordering eligibility is derived from commercial service state + reservation state + document state + delivery/control state + current authoritative servicing decision. Future financial orchestration may add an external funding gate without changing these owners.

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
7. Payment lifecycle is not represented by Order status or OrderService flags. Current core commands do not require Payment/JetPay evidence.

### Payment-neutral core ADR (v2.0)

The intrinsic Ordering capabilities `Reserve`, `ConfirmReservedCapacity`, `Issue`, `Cancel`, `Void`, `Refund`, `Exchange`, `Reissue` and `Revalidate` are modeled without a JetPay prerequisite.

This does **not** mean a production airline sale should issue without financial authority. It means financial authority belongs to an external orchestration/policy layer. Once JetPay is frozen, that layer may require payment/credit/commitment evidence **before invoking** the intrinsic Ordering command. The ETKT/EMD/Reservation aggregates and their commands do not change.

Current `OrderStatus` values `Paying`, `PaymentFailed`, `Paid`, and `PaymentUnconfirmed` are legacy/reserved values and MUST NOT be produced by the payment-neutral core. Do not remove them from public contracts merely for cleanup; deprecate behavior first.

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
| `CabinClassId` | `long?` | Yes | CURRENT-SOURCE-AVAILABLE / ADDITIVE CORRECTION |
| `CheckedBaggageAllowance` | `BaggageAllowance?` | Yes | NOW |
| `CabinBaggageAllowance` | `BaggageAllowance?` | Yes | NOW |
| `IsRefundable` | `bool` | No | NOW |
| `IsChangeable` | `bool` | No | NOW |
| `IsUpgradable` | `bool` | No | NOW |

The three boolean terms are accepted-sale snapshots only. Refund/exchange authority comes from the current AirPrice servicing decision.

`CabinClassId` is a confirmed current-source preservation gap: the reviewed `FlightOfferDetailResponse.OfferFlight` carries it, while the current `OfferDetail/OrderAirTransportService` path does not persist it. Preserve it at the earliest next permitted additive schema change (no later than Issue Stage). Existing rows remain null; never infer a cabin from RBD/booking class.

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
| `WeightUnit` | `BaggageWeightUnit?` | Yes |
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

| Field | Type | Null | Stage | Meaning / invariant |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Stable passenger occurrence identity inside the Order. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `Index` | `int` | No | NOW | Stable source/display sequence; never used as provider identity. |
| `SourceTravellerRef` | `string?` | Yes | NOW | Upstream traveller/passenger reference when supplied. |
| `PassengerType` | `PassengerTypeCode` | No | NOW | Accepted ADT/CHD/INF/etc. passenger type. Provider mappings are semantic, never numeric casts. |
| `AgeRange` | `AgeRange` | No | NOW | Accepted passenger age band. |
| `InfantParentTravellerId` | `long?` | Yes | NOW | Parent traveller for lap infant; enforces guardian/split/reservation closure. |
| `CurrentProfileRevisionId` | `long` | No | NOW | Current mutable-profile pointer; documents snapshot the exact revision used at issuance. |
| `Status` | `OrderTravellerStatus` | No | NOW | Traveller lifecycle within the Order. |
| `PassengerGroupId` | `long?` | Yes | FUTURE | Named membership of a PassengerGroup. |

No document uses mutable current name/profile as historical truth; ETKT/EMD stores `TravellerProfileRevisionId` at issue.

### `TravellerProfileRevision`

| Field | Type | Null | Stage | Meaning / invariant |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Immutable revision identity. |
| `TravellerId` | `long` | No | NOW | Parent traveller. |
| `GivenName` | `string` | No | NOW | Normalized accepted given name for this revision. |
| `Surname` | `string?` | Yes | NOW | Null only when `NoSurname=true`. |
| `NoSurname` | `bool` | No | NOW | Explicit no-surname fact. |
| `DateOfBirth` | `DateOnly?` | Yes | NOW | Source passenger DOB when supplied. |
| `Gender` | `Gender?` | Yes | NOW | Source passenger gender when supplied; provider-specific fallback remains adapter behavior. |
| `NationalityId` | `int?` | Yes | NOW | Source nationality. |
| `CountryOfResidenceId` | `int?` | Yes | NOW | Source country of residence. |
| `CreatedByChangeId` | `long` | No | NOW | OrderChange that created the revision. |
| `SupersededByChangeId` | `long?` | Yes | NOW/FUTURE-BEHAVIOR | Change that superseded this revision. Historical revisions are never edited. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Revision creation timestamp. |

### `OrderTravellerDocument`

| Field | Type | Null | Stage | Meaning / invariant |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Traveller-document occurrence identity. |
| `OrderTravellerId` | `long` | No | NOW | Parent traveller. |
| `Type` | `TravellerDocumentType` | No | NOW | Passport/identity/travel-document type from source. |
| `Number` | `string` | No | NOW | Document number; sensitive-data handling follows platform security rules. |
| `ExpiryDate` | `DateOnly?` | Yes | NOW | Source expiry. |
| `IssuanceCountryId` | `int` | No | NOW | Issuing country from source. |
| `Holder` | `string` | No | NOW | Source holder value. |
| `IssueDate` | `DateOnly?` | Yes | FUTURE / SOURCE-GATED | Add only when an authoritative passenger/document contract supplies it. |
| `ApplicableCountryId` | `int?` | Yes | FUTURE / SOURCE-GATED | Visa/document applicability when supplied. |
| `SourceDocumentRef` | `string?` | Yes | FUTURE / SOURCE-GATED | External identity when supplied. |

### `OrderTravellerLoyaltyAccount` — future/source-gated

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | FUTURE |
| `OrderTravellerId` | `long` | No | FUTURE |
| `ProgramOwnerAirlineId` | `int?` | Yes | FUTURE / SOURCE-GATED |
| `ProgramCode` | `string` | No | FUTURE / SOURCE-GATED |
| `MemberNumber` | `string` | No | FUTURE / SOURCE-GATED |
| `TierCode` | `string?` | Yes | FUTURE / SOURCE-GATED |
| `SourceReference` | `string?` | Yes | FUTURE / SOURCE-GATED |

Loyalty identity is a passenger/customer-service fact. Redemption/points-as-payment remains outside Ordering unless a later authoritative commercial/payment contract requires a priced product or external funding correlation.

---

## 6.11 Contacts and remarks

### `OrderContact`

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Contact occurrence identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `Sequence` | `int` | No | NOW | Stable contact order. |
| `Role` | `ContactRole` | No | NOW | Contact purpose/role. |
| `ContactName` | `string?` | Yes | NOW | Source contact name. |
| `ContactPoints` | collection | No | NOW | Typed email/phone/etc. occurrences. |

### `OrderContactPoint`

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Contact-point occurrence identity. |
| `OrderContactId` | `long` | No | NOW | Parent contact. |
| `Type` | `ContactPointType` | No | NOW | Email/phone/etc. |
| `Value` | `string` | No | NOW | Source contact value. |
| `CountryCode` | `string?` | Yes | NOW | Dialing-country/source code when applicable. |
| `IsPrimary` | `bool` | No | NOW | Primary point within the source contact semantics. |

### `OrderRemark`

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Remark occurrence identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `Type` | `OrderRemarkType` | No | NOW | Typed remark category. |
| `Visibility` | `OrderRemarkVisibility` | No | NOW | Visibility/audience. |
| `Scope` | `OrderRemarkScope` | No | NOW | Order/traveller/segment/item/service scope. |
| `TravellerId` | `long?` | Yes | NOW | Scoped traveller when applicable. |
| `SegmentId` | `long?` | Yes | NOW | Scoped segment when applicable. |
| `OrderItemId` | `long?` | Yes | NOW | Scoped item when applicable. |
| `OrderServiceId` | `long?` | Yes | NOW | Scoped service when applicable. |
| `Text` | `string` | No | NOW | Remark text. |
| `CategoryCode` | `string?` | Yes | NOW | Source/business category code. |
| `IsPrintedOnItinerary` | `bool` | No | NOW | Source/output instruction. |
| `IsPrintedOnInvoice` | `bool` | No | NOW | Source/output instruction. |
| `Status` | `OrderRemarkStatus` | No | NOW | Active/Superseded historical state. |
| `SupersedesRemarkId` | `long?` | Yes | NOW | Append/supersede lineage. |
| `CreatedBy` | `long` | No | NOW | Actor identity recorded by current domain. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Creation timestamp. |

Document servicing reason/provenance is recorded on document servicing records, not overloaded into free-text remarks.

---

## 6.12 `OrderJourney`, `OrderSegment`, `OrderSegmentLeg`

Sold itinerary is immutable accepted history. Operational schedule/DCS facts are separate observations; accepted reaccommodation creates successor commercial facts.

### `OrderJourney`

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | NOW |
| `OrderId` | `long` | No | NOW |
| `Sequence` | `int` | No | NOW |
| `BoundId` | `string` | No | NOW |
| `OriginAirportId` | `int` | No | NOW |
| `DestinationAirportId` | `int` | No | NOW |

### `OrderSegment`

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Segment occurrence identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `OrderJourneyId` | `long` | No | NOW | Parent journey. |
| `Sequence` | `int` | No | NOW | Sold segment order. |
| `FlightId` | `long` | No | NOW | Source flight identity. |
| `FlightVersion` | `int` | No | NOW | Accepted source flight version. |
| `FlightNumber` | `string?` | Yes | NOW | Sold flight number. |
| `OriginAirportId` | `int` | No | NOW | Sold origin. |
| `OriginAirportTerminalId` | `int?` | Yes | NOW | Sold origin terminal. |
| `DestinationAirportId` | `int` | No | NOW | Sold destination. |
| `DestinationAirportTerminalId` | `int?` | Yes | NOW | Sold destination terminal. |
| `OperatingAirlineId` | `int` | No | NOW | Sold operating carrier. |
| `MarketingAirlineId` | `int` | No | NOW | Sold marketing carrier. |
| `SoldDeparture` | `DateTimeOffset` | No | NOW | Accepted departure snapshot. |
| `SoldArrival` | `DateTimeOffset` | No | NOW | Accepted arrival snapshot. |
| `Duration` | `int` | No | NOW | Accepted duration using current source unit. |
| `AircraftId` | `int?` | Yes | NOW | Accepted aircraft identity when supplied. |
| `OperationType` | `FlightOperationType` | No | FUTURE / SOURCE-GATED | `Scheduled/OpenCharter/ClosedCharter/Other`; source operation context is required. |
| `SourceSegmentRef` | `string?` | Yes | FUTURE / SOURCE-GATED | External dated-segment identity if supplied. |

### `OrderSegmentLeg`

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | NOW |
| `OrderSegmentId` | `long` | No | NOW |
| `Sequence` | `int` | No | NOW |
| `LegId` | `long` | No | NOW |
| `OriginAirportId` | `int` | No | NOW |
| `OriginAirportTerminalId` | `int?` | Yes | NOW |
| `DestinationAirportId` | `int` | No | NOW |
| `DestinationAirportTerminalId` | `int?` | Yes | NOW |
| `DepartureDateTime` | `DateTimeOffset` | No | NOW |
| `ArrivalDateTime` | `DateTimeOffset` | No | NOW |
| `StopType` | `StopType?` | Yes | NOW |
| `StopDurationMinutes` | `int?` | Yes | NOW |
| `StopPassengersCanBoardOrLeave` | `bool?` | Yes | NOW |

### `FlightOperationType`

```text
Scheduled
OpenCharter
ClosedCharter
Other
```

Open/Closed charter semantics follow the source operation/contract context. `Other` does not authorize inventing a proprietary subtype.

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

`SpecialServiceRequestStatus = Requested | Confirmed | Rejected | Cancelled | Unknown`. Provider-specific codes remain raw evidence.


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

## 7.4 `FarePricingAtom` — derived pricing-integrity rule (Stage 5 R2)

A commercial partial cancellation does not create a new fare. Ordering therefore distinguishes an **intact** accepted pricing atom from a **fractured** one. `FarePricingAtom` is a derived concept computed from the accepted fare topology. It is **not a persisted entity**, not an aggregate and not a table.

### Atom derivation

| `OrderFarePricingUnit.SemanticType` | Pricing atom |
|---|---|
| `OneWay` | Each `OrderFareComponent` is one atom: all Air service IDs in its `CoveredOrderServiceIds`. A through fare component spanning several segments is one indivisible atom, even when it is the only component of its pricing unit. |
| `RoundTrip`, `OpenJaw`, `CircleTrip` | The whole pricing unit is one atom: the union of all its fare components' `CoveredOrderServiceIds`. |
| `Unspecified`, `Other` | The whole pricing unit is one conservative atom. Partial-pricing independence is never inferred. |

### Intact and fractured atoms

- An atom is **intact** when all of its Air services are commercially `Active`.
- An atom whose Air services are all ended is irrelevant to new issuance.
- An atom is **fractured** when it holds at least one `Active` Air service **and** at least one ended (non-`Active`) Air service.

A fractured atom means the original accepted pricing no longer authorizes issuing its remaining services. Until a source-authoritative repricing / exchange / servicing decision creates successor pricing topology, Issue of any service in a fractured atom fails with `REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION` (Ordering code 2820, HTTP 409), evaluated **before** any AirPrice call, document-stock lock or allocation, Issue `FulfillmentTask` or document. No new fare is invented and no invalidated partial fare is reused.

| Accepted topology | Partial cancellation | Remaining scope |
|---|---|---|
| OUT = OneWay unit A, IN = OneWay unit B | cancel IN | OUT atom intact → issueable (reservation root may be `Mixed`) |
| One RoundTrip unit over OUT + IN | cancel IN | fractured → OUT blocked pending repricing |
| One through fare component over Segment 1 + Segment 2 | cancel Segment 2 | fractured → Segment 1 blocked |
| One OneWay unit, component A = Segment 1, component B = Segment 2 | cancel all of component B | component A intact → Segment 1 issueable |

### Validation scope

A reservation-validation request for a service scope expands that scope to every pricing atom it touches; at Issue those atoms are always intact, because a fractured atom fails before validation. The persisted `ReservationValidationEvidence.ValidatedOrderServiceIds` (§13.6) records exactly the expanded Air service scope represented in the request.

---

# 8. Pricing ledger and Revenue-Accounting-preserving facts

## 8.1 `PricingLine`

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Pricing occurrence identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `CreatedByChangeId` | `long` | No | NOW | Commercial change that created the line. |
| `Reason` | `OrderPricingReason` | No | NOW | Why this line exists. |
| `Scope` | `PricingLineScope` | No | NOW | Pricing scope. |
| `Category` | `OrderPricingLineCategory` | No | NOW | Fare/tax/fee/etc. classification. |
| `SubCategory` | `OrderPricingLineSubCategory` | No | NOW | Source/domain subcategory. |
| `Direction` | `OrderPricingLineDirection` | No | NOW | Debit/credit economic direction. |
| `Treatment` | `PricingLineTreatment` | No | NOW | Customer-price vs settlement-only treatment. |
| `Code` | `string?` | Yes | NOW | Source fare/tax/fee/YQ-YR/etc. code. |
| `Description` | `string?` | Yes | NOW | Source description. |
| `Reference` | `string?` | Yes | NOW | Source pricing/fare/tax reference. |
| `Amount` | `decimal` | No | NOW | Source amount in `CurrencyId`. |
| `CurrencyId` | `int` | No | NOW | Source currency. |
| `EquivalentAmount` | `decimal` | No | NOW | Accepted equivalent amount. |
| `EquivalentCurrencyId` | `int` | No | NOW | Equivalent currency. |
| `ExchangeRateSnapshot` | `ExchangeRateSnapshot?` | Yes | NOW | Immutable conversion evidence. |
| `Refundability` | `RefundabilityRule?` | Yes | NOW | Sale-time line term snapshot; not servicing authority. |
| `CreatedAt` | `DateTimeOffset` | No | NOW | Creation timestamp. |
| `Allocations` | collection | No | NOW | Exact attribution of this line. |
| `OwnerCarrierId` | `int?` | Yes | FUTURE / SOURCE-GATED | Carrier owning the line when supplied. |
| `TaxJurisdiction` | `TaxJurisdictionSnapshot?` | Yes | FUTURE / SOURCE-GATED | Country/station tax attribution. |
| `CommissionDetail` | `CommissionSnapshot?` | Yes | FUTURE / SOURCE-GATED | Recipient/rate/basis. |
| `SourceAccountingReference` | `string?` | Yes | FUTURE / SOURCE-GATED | Accounting/settlement reference supplied by source. |

`Code`/`Reference` remain the generic lossless source slots. Typed tax/commission structures add facts; they do not replace source values.

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

`CommissionRecipientType = Agency | Corporate | Airline | Partner | Other`. Recipient is never inferred from SalesContext.

## 8.2 `PricingAllocation`

| Field | Type | Null | Stage |
|---|---|---:|---|
| `Id` | `long` | No | NOW |
| `PricingLineId` | `long` | No | NOW |
| `OrderItemId` | `long?` | Yes | NOW |
| `OrderServiceId` | `long?` | Yes | NOW |
| `OrderJourneyId` | `long?` | Yes | NOW |
| `OrderSegmentId` | `long?` | Yes | NOW |
| `TravellerId` | `long?` | Yes | NOW |
| `Amount` | `decimal` | No | NOW |
| `CurrencyId` | `int` | No | NOW |
| `EquivalentAmount` | `decimal` | No | NOW |
| `EquivalentCurrencyId` | `int` | No | NOW |

Allocation preserves economic attribution without creating a generic fare-construction graph.

## 8.3 `ExchangeRateSnapshot`

| Field | Type | Null | Stage |
|---|---|---:|---|
| `FromCurrencyId` | `int` | No | NOW |
| `ToCurrencyId` | `int` | No | NOW |
| `Rate` | `decimal` | No | NOW |
| `DecimalPlaces` | `int` | No | NOW |
| `PeriodId` | `string?` | Yes | NOW |
| `RoundingFactor` | `decimal?` | Yes | SOURCE-GATED / CURRENT-WIRE-SEEN | Current Offer wire exposes a rounding factor, but the reviewed domain contract drops it and the wire type is not precision-safe enough to declare final semantics. Preserve only after the upstream contract confirms type/meaning. |

Historical conversion is immutable. Persist source precision; do not fall back to low-precision defaults. The already accepted `EquivalentAmount` remains authoritative even when `RoundingFactor` is not materialized.

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

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Stable commercial change identity. |
| `OrderId` | `long` | No | NOW | Parent Order. |
| `ChangeType` | `OrderChangeType` | No | NOW | Typed committed business change. |
| `CommercialVersion` | `int` | No | NOW | Resulting commercial version. |
| `ActorContext` | `SalesContext` | No | NOW | Actor/channel context that committed the change. |
| `SourceReference` | `string?` | Yes | NOW | Upstream decision/offer/reference when available. |
| `CommittedAt` | `DateTimeOffset` | No | NOW | Commit timestamp. |
| `SourceSystem` | `string?` | Yes | FUTURE / SOURCE-GATED | AirOffer/AirPrice/Disruption/etc. when sourced. |
| `ReasonCode` | `string?` | Yes | FUTURE / SOURCE-GATED | Source business reason. |
| `ReasonText` | `string?` | Yes | NOW (Stage 5 R2) | Human-readable reason detail / annotation preserved with the change. |
| `IsInvoluntary` | `bool` | No | FUTURE | Voluntary vs disruption/involuntary servicing. Default may only be set by the committing use case. |
| `WaiverCode` | `string?` | Yes | FUTURE / SOURCE-GATED | Source-approved waiver. |

`ReasonCode`, `ReasonText`, `SourceSystem`, `SourceReference`, `IsInvoluntary` and `WaiverCode` are separate facts; none is folded into another.

### `ReasonText` invariants

- maximum length 500 characters, after trimming leading and trailing whitespace;
- whitespace-only → `null`;
- not a source policy code, not a waiver, not a replacement for `ReasonCode`;
- never used to derive business eligibility.

Backoffice cancellation maps `request.Reason → OrderChange.ReasonCode` and `request.ReasonDetail → OrderChange.ReasonText`. Historical `OrderChange` rows have `ReasonText = null`; no value is backfilled. `DocumentVoidRecord.ReasonText` is a separate document fact and is unchanged.

Final semantic vocabulary must be able to represent: `Create, AddProduct, RemoveService, Cancel, ChangeService, TravellerCorrection, ContactCorrection, Split, Refund, Exchange, Reissue, Revalidation, InvoluntaryReaccommodation, GroupNameUpdate, GroupCapacityChange`.

Do not create one aggregate per change type. New Items/Services/Pricing/Documents plus `OrderChange` provenance are the durable history.

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
| `DepositAmount` | `decimal?` | Yes | Accepted contractual deposit amount when source supplies an absolute amount. |
| `DepositCurrencyId` | `int?` | Yes | Required when `DepositAmount` is present. |
| `DepositPercentage` | `decimal?` | Yes | Accepted contractual percentage when source supplies percentage rather than amount. |
| `DepositDispositionCode` | `string?` | Yes | Source rule/reference for forfeiture/refundability; never inferred. |

At most one of `DepositAmount` or `DepositPercentage` is required by a source contract; both may coexist only when the source explicitly defines both. Name/deposit/final-payment deadlines are `OrderTimeLimit` rows scoped to PassengerGroup, not fields duplicated here.

## 11.3 Group names and travellers

Named passengers become normal `OrderTraveller` records with `PassengerGroupId`. Their final `OrderService` occurrences are still traveller-specific.

Before names exist, the group may have commercial item(s) and capacity reservation without individual services. This is why group capacity is not forced into fake travellers.

---

# 12. Charter support

No `Charter` aggregate exists.

## 12.1 Open charter

- `OrderSegment.OperationType = OpenCharter`.
- Seats are sold to individual customers/travel agencies like normal retail services.
- Normal Air Service / reservation / document / DCS lifecycle applies. Financial orchestration is external and may be added later.

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

`FulfillmentReservation` is the durable Ordering-side truth for a reservation/resource outcome owned by one fulfillment provider. It is not a provider request log and it is not the commercial Order.

## 13.1 Root fields

| Field | Type | Null | Stage | Meaning / invariant |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Logical reservation-operation identity. A definitive terminal retry creates a new reservation. Unknown retry keeps this identity. |
| `OrderId` | `long` | No | NOW | Parent commercial Order. |
| `FulfillmentProviderKey` | `string` | No | NOW | Provider responsible for the resource. |
| `Mode` | `ReservationMode` | No | NOW | `HoldThenConfirm`, `ImmediateConfirm`, or `None`. `None` does not create a reservation. |
| `IdempotencyKey` | `string` | No | NOW | Stable **Ordering logical-effect identity** for this reservation operation. It is sent to a provider only when that operation's real contract accepts an idempotency identity; do not equate presence of this field with provider duplicate guarantees. |
| `CorrelationReference` | `string` | No | NOW | Technical/business correlation; never substitutes for OrderReference/PNR/provider refs. |
| `ProviderOperationRef` | `string?` | Yes | NOW | Provider operation/resource reference such as FlightFlow HoldId. Immutable once known. |
| `ProviderRecordLocator` | `string?` | Yes | FUTURE / SOURCE-GATED | Supplier PNR/booking locator when distinct from operation/hold reference. Never overwrite `Order.RecordLocator`. |
| `Status` | `FulfillmentReservationStatus` | No | NOW | Business reservation summary derived from unit/resource evidence. **Summary only:** it is never by itself an Issue gate (§27.3); a truthful `Mixed` root is never rewritten to pass Issue. |
| `RequestedExpiresAt` | `DateTimeOffset?` | Yes | NOW | Exact requested provider hold expiry persisted as part of external intent. Never recomputed on Unknown replay. |
| `ValidationEvidence` | `ReservationValidationEvidence?` | Yes | NOW (Stage 5 R2) | Canonical latest AirPrice reservation-validation evidence (§13.6): commercial version, validity, observation time and exact validated service scope. The only business authority for validation currency. |
| `ReservationValidationTimeLimit` | `DateTimeOffset?` | Yes | NOW | **Compatibility projection only.** For evidence recorded since Stage 5 R2 it equals `ValidationEvidence.ValidUntil`. Historical rows keep their timestamp for history, but a timestamp alone never proves which scope and commercial version were validated and is never eligibility authority. It is not a hard Order expiry. |
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
| `CoveredJourneyIds` | `IReadOnlyCollection<long>` | No |
| `CoveredSegmentIds` | `IReadOnlyCollection<long>` | No |
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
4. `Unknown` preserves the same logical business operation and exact persisted request. Replay with the same provider idempotency identity is allowed **only when that mutation's provider contract verifies safe idempotent replay**. If read-back exists, reconcile first. If neither is verified, keep `Unknown` and do not blind-replay.
5. Definitive `Rejected/Released/Expired/Cancelled` followed by a new customer/business retry creates a new FulfillmentReservation and re-runs current eligibility/validation.
6. Held/Confirmed services are never re-reserved as a new resource without first reaching a conclusive terminal state or provider-supported change/split operation.
7. Partial/mixed provider truth is retained. It is never collapsed to a boolean.
8. Multi-provider reservation operations are independent; one provider failure does not automatically release another provider's successful resource unless an explicit business compensation policy says so.
9. `ExpiresAt`, `ValidationEvidence` (with its `ReservationValidationTimeLimit` projection), and `LastTicketingDate` remain distinct facts.
10. Validation evidence that does not cover the required scope at the current `CommercialVersion` and time (§13.6) blocks Confirm/Issue until refreshed; it does not permanently expire the Order while the hard ticketing deadline remains open.

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

## 13.6 `ReservationValidationEvidence` — canonical owned evidence (Stage 5 R2)

A bare timestamp does not say which service scope and which commercial version were validated. The canonical latest validation evidence is an owned value of `FulfillmentReservation`, persisted atomically as one logical snapshot. It is not an aggregate root.

| Field | Type | Null | Meaning |
|---|---|---:|---|
| `CommercialVersion` | `int` | No | `Order.CommercialVersion` the validation request was built from. Always > 0. |
| `ValidUntil` | `DateTimeOffset` | No | AirPrice-returned validity of the accepted validation. |
| `ValidatedAt` | `DateTimeOffset` | No | Local observation time of the accepted validation. |
| `ValidatedOrderServiceIds` | `IReadOnlyCollection<long>` | No | Exact, distinct, non-empty Air service scope represented in the accepted validation request, after any pricing-atom expansion (§7.4). Ordering-known request evidence, never guessed afterwards and never fabricated as provider output. |

Meaning: at `ValidatedAt`, the pricing/reservation authority accepted validation for exactly `ValidatedOrderServiceIds` against `CommercialVersion`, until `ValidUntil`.

Evidence is current for a required service scope `S` only when:

```text
Evidence != null
Evidence.CommercialVersion == Order.CommercialVersion
now < Evidence.ValidUntil
S ⊆ Evidence.ValidatedOrderServiceIds
```

Any failed condition means validation must be refreshed. A commercial cancellation increments `CommercialVersion`, so pre-cancellation evidence is stale even when its timestamp is still in the future.

- **Reserve** captures evidence from the AirPrice validation performed while preparing the reservation.
- **Confirm** (Held): when evidence does not cover the reservation's Air services at the current version and time, refresh through AirPrice, persist the new evidence, then confirm. Safe provider Confirm replay is unchanged.
- **Issue**: refresh is **validation-only** (§27.3). It never recreates or re-plans the reservation, never calls FlightFlow Reserve/Confirm/Cancel, never changes reservation units, provider references or resource state, and never requires the current active service plan to equal the historical reservation plan.
- **Historical rows:** evidence is never backfilled with a guessed scope or version. A row that holds only the legacy `ReservationValidationTimeLimit` has `ValidationEvidence = null` and is revalidated before its next Confirm/Issue.

---

# 14. Aggregate: FulfillmentTask

`FulfillmentTask` owns execution/recovery of an external effect. It does **not** own the business resource state; that belongs to FulfillmentReservation, documents, payment owner, etc.

## 14.1 Final root shape

| Field | Type | Null | Stage | Meaning |
|---|---|---:|---|---|
| `Id` | `long` | No | NOW | Execution task identity. |
| `OrderId` | `long` | No | NOW | Commercial Order. |
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

The aggregate remains one concept across Reserve, Confirm, Release, Issue, EMD issue, provider ancillary fulfilment and document recovery. Issue/document operations use their `FulfillmentTask.Id` as the stable external-effect operation identity; do not invent a parallel generic business-operation aggregate.

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
ReservationGroupSpace
```

Materialize new target kinds only with the Stage that needs them.

## 14.3 `FulfillmentTaskAttempt`

Keep:

`Id, FulfillmentTaskId, AttemptNumber, StartedAt, CompletedAt?, Outcome?, FailureKind?, FailureReason?, Error?`.

One attempt can contain 1..N ProviderInteractions, e.g. read-back then same-key mutation replay **only when the provider contract supplies safe idempotent replay semantics**.

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
| `IdempotencyKey` | `string?` | Yes | Provider-level idempotency identity **only when verified for that operation**. Null does not mean the provider is non-idempotent; it means no authoritative idempotency identity is recorded for this interaction. Reads normally need none. |
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

## 14.5 Provider operation capability gate — mandatory for every Stage

The generic domain does **not** assume that a provider mutation is idempotent or non-idempotent. Each operation is verified independently. A provider may, for example, support idempotent CreateHold but use different semantics for Confirm, Release, Split or Cancel.

For each provider operation used by a Stage, the Stage PRD must record:

| Capability fact | Allowed values | Rule |
|---|---|---|
| `AcceptsIdempotencyIdentity` | VerifiedYes / VerifiedNo / Unknown | `Unknown` is not treated as `No`. |
| `DuplicateMutationSemantics` | SameEffect / Conflict / NewEffect / Unknown | Must come from contract/source or executable provider test. |
| `SupportsAuthoritativeReadBack` | VerifiedYes / VerifiedNo / Unknown | Name the read operation and identity when Yes. |
| `SupportsSafeReplayAfterAmbiguousOutcome` | VerifiedYes / VerifiedNo / Unknown | May be Yes only from provider semantics, not because local code has an idempotency field. |
| `ResultGranularity` | AtomicOperation / PerUnit / Mixed / Unknown | Governs partial outcome modeling. |
| `ExpiryAuthority` | ProviderClockGuaranteed / EventEvidence / ReadBackRequired / Unknown | Governs when local time can mark remote resource expired. |

Recovery decision:

```text
ambiguous mutation outcome
    ├─ authoritative read-back verified -> reconcile
    ├─ no/read-back inconclusive + safe same-effect replay verified -> replay exact persisted request
    └─ neither verified -> remain Unknown; manual/source reconciliation required
```

### Current FlightFlow evidence boundary at reviewed Ordering HEAD

The reviewed Ordering adapter proves only the following local facts:

- CreateHold currently sends a persisted `IdempotencyKey` and expects echoed identity.
- ConfirmHold currently sends a HoldId in the Ordering adapter.
- Current Ordering code does not expose a Confirm read-back operation.

These observations **do not prove** that FlightFlow itself lacks confirm idempotency, resource-level duplicate protection or another read API. Stage 3 must inspect the actual FlightFlow contract/source. If available capability says safe retry/read-back exists, implement that branch. If it says it does not, implement the no-replay branch. If source cannot establish either answer, report `BLOCKED_SOURCE`; do not infer from the Ordering adapter.

The same rule applies to FlightFlow automatic expiry, INF wire shape and the meaning of any provider-only monetary/request field.

---

# 15. Financial / Payment boundary — DEFERRED INTEGRATION

## 15.1 Final ownership decision

Payment transaction lifecycle, tender eligibility, provider routing, customer action, authorization, capture, credit commitment, wallet movement, refund rail and reconciliation belong to JetPay/payment capabilities, not Ordering.

**Current Ordering core has no JetPay dependency and no payment precondition.**

This is a deliberate compatibility ADR:

```text
Intrinsic Ordering command
    Reserve / Confirm / Issue / Refund / Exchange / ...
            ↑
Future external orchestration/policy gate
            ↑
JetPay financial authority
```

The future gate is additive. It may prevent a caller from invoking `ConfirmReservedCapacity`, `IssueOrder`, `CommitExchange`, etc. until the airline's financial policy is satisfied, but it must not alter ETKT/EMD/Reservation ownership or lifecycle semantics.

## 15.2 What is frozen now

1. `Order`, `OrderItem`, `OrderService`, `FulfillmentReservation`, ETKT/EMD and servicing aggregates do not own payment state.
2. `OrderStatus` is not a payment state machine.
3. No current Stage creates `PaymentIntent`, `PaymentSession`, `FundingObligation`, wallet/credit entities, or a dummy/mock payment flag.
4. No current command calls a fake `IPaymentProvider` returning success.
5. A future JetPay integration may add an Order-child **financial coverage evidence projection** if Ordering needs local issue/servicing policy evaluation or order-view evidence.
6. The exact projection fields are **JETPAY-CONTRACT-GATED**. The previous `OrderPaymentCoverage`/`PaymentIntent` field list is not authoritative and is intentionally removed from this Master.
7. Revenue Accounting may consume payment/settlement facts directly from JetPay/LedgerFlow; Ordering is not required to duplicate the full payment ledger.

## 15.3 Future stable correlation requirements

Even before JetPay exists, Ordering commercial facts already provide stable correlation inputs:

- `OrderId` / `OrderReference`;
- `CommercialVersion`;
- command/business purpose (`InitialSale`, `AddService`, `ExchangeAdditionalCollection`, `GroupDeposit`, `FinalPayment`, etc.) when future integration is designed;
- exact amount/currency from accepted pricing;
- `OrderChange.Id` for servicing deltas where applicable.

No synthetic `PaymentIntentId` or dummy payment identifier is stored now.

## 15.4 Refund and exchange money movement

Commercial/document servicing and money movement are distinct truths:

```text
AirPrice / Ordering servicing decision
    -> refund/additional-collection/residual-value commercial fact

JetPay / financial rail
    -> actual money movement / credit / commitment / settlement
```

Current Ordering stages may complete commercial/document history without executing payment rail movement. Future JetPay integration records or correlates financial settlement evidence without rewriting the original commercial/document history.

## 15.5 Future integration acceptance rule

When JetPay is ready, integration is accepted only if adding it requires:

- new ACL/orchestration code;
- optional financial-evidence projection;
- new workflow/policy gates;

and **does not require redesigning** `OrderService`, `FulfillmentReservation`, `ElectronicTicket`, `ElectronicMiscDocument`, `DocumentStock`, refund/exchange decision snapshots, DCS or disruption ownership.

---

# 16. Aggregate: ElectronicTicket

ElectronicTicket remains a real aggregate while AeroTech interoperates with PNR/ETKT/EMD ecosystems. Payment is not part of this aggregate.

## 16.1 Root fields

| Field | Type | Null | Meaning |
|---|---|---:|---|
| `Id` | `long` | No | Internal document identity. |
| `OriginalOrderId` | `long` | No | Order that originally issued the ticket. |
| `CurrentServicingOrderId` | `long` | No | Current Order responsible for servicing after split/transfer. |
| `TravellerId` | `long` | No | Passenger beneficiary. |
| `TravellerProfileRevisionId` | `long` | No | Immutable passenger profile revision used for issuance; later name correction does not rewrite ticket history. |
| `IssueFulfillmentTaskId` | `long` | No | Stable Issue external-effect identity; replaces ambiguous generic `OperationId`. |
| `DocumentNumber` | `string` | No | Accountable ticket number. |
| `IssuanceContext` | `DocumentIssuanceContext` | No | Frozen issuer/seller context. |
| `Authority` | `DocumentAuthority` | No | `Local` or `External`. |
| `IssuedAt` | `DateTimeOffset` | No | Authoritative issue time. |
| `VoidDeadline` | `DateTimeOffset?` | Yes | Issuer/source-authoritative void deadline when supplied. |
| `IssuedTotal` | `decimal` | No | Document value in document currency. |
| `CurrencyId` | `int` | No | Document currency. |
| `ProviderReference` | `string?` | Yes | External issuer/GDS reference when supplied. |
| `StatusSummary` | `ElectronicTicketStatus` | No | Summary derived from document/coupon history. |
| `DocumentVersion` | `int` | No | Optimistic/stale-decision guard for servicing. |
| `PredecessorElectronicTicketId` | `long?` | Yes | Reissue/exchange lineage. |
| `PredecessorExchangeChangeId` | `long?` | Yes | OrderChange that produced this successor when applicable. |
| `Coupons` | collection | No | Ordered coupon occurrences. |
| `PriceLinks` | collection | No | Immutable ticket-price attribution. |
| `VoidRecord` | `DocumentVoidRecord?` | Yes | Append-once void history. |
| `RefundRecords` | collection | No | Refund history. |
| `ExchangeRecords` | collection | No | Exchange/reissue history. |
| `RevalidationRecords` | collection | No | Revalidation history. |

### `DocumentIssuanceContext`

| Field | Type | Null |
|---|---|---:|
| `IssuerCarrierId` | `int` | No |
| `ValidatingCarrierId` | `int?` | Yes / SOURCE-GATED |
| `IssuingOfficeId` | `long?` | Yes |
| `IssuedByActorId` | `long?` | Yes |
| `TravelAgencyId` | `long?` | Yes |
| `AgencyIataNumber` | `string?` | Yes / SOURCE-GATED |
| `Pcc` | `string?` | Yes / SOURCE-GATED |
| `SalesChannel` | `SalesChannel?` | Yes |
| `SourceFormOfPaymentCode` | `string?` | Yes / FUTURE PAYMENT-SOURCE-GATED |

The final field exists only for legacy/interline/document-source traceability if a real issuer supplies it; Ordering never manufactures a form-of-payment from JetPay state.

## 16.2 `TicketCoupon`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `TicketId` | `long` | No |
| `CouponNumber` | `int` | No |
| `OriginalOrderServiceId` | `long` | No |
| `CurrentOrderServiceId` | `long` | No |
| `OrderSegmentId` | `long` | No |
| `OrderFareComponentId` | `long?` | Yes / SOURCE-LINK |
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
| `NotValidBefore` | `DateOnly?` | Yes / SOURCE-GATED |
| `NotValidAfter` | `DateOnly?` | Yes / SOURCE-GATED |
| `UsedAt` | `DateTimeOffset?` | Yes / DCS-SOURCE |
| `UsageReference` | `string?` | Yes / DCS-SOURCE |

`FinancialStatus`: `Open, Used, Void, Exchanged, Refunded, Suspended`.  
`ControlStatus`: `Local, External, ReleasePending, Unknown`.

## 16.3 `IssuedSegmentSnapshot`

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

It is immutable after issue. Operational schedule changes never rewrite it.

## 16.4 `TicketPriceLink`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicTicketId` | `long` | No |
| `TicketCouponId` | `long?` | Yes |
| `PricingLineId` | `long` | No |
| `PricingAllocationId` | `long?` | Yes |
| `AttributedValue` | `decimal` | No |
| `CurrencyId` | `int` | No |

This typed link avoids an ambiguous generic `DocumentId` and is required for refund/exchange/revenue-accounting traceability.

## 16.5 Ticket servicing records

### `DocumentVoidRecord`

| Field | Type | Null |
|---|---|---:|
| `VoidFulfillmentTaskId` | `long` | No |
| `ReasonCode` | `string?` | Yes |
| `ReasonText` | `string?` | Yes |
| `ProviderReference` | `string?` | Yes |
| `ActorId` | `long?` | Yes |
| `VoidedAt` | `DateTimeOffset` | No |

### `DocumentRefundRecord`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicTicketId` | `long` | No |
| `RefundChangeId` | `long` | No |
| `QuotedRefundId` | `string` | No |
| `PricingSource` | `string` | No |
| `SourcePricingReference` | `string?` | Yes |
| `RefundType` | `DocumentRefundType` | No |
| `ApprovedAmount` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `PenaltyAmount` | `decimal?` | Yes |
| `WaiverCode` | `string?` | Yes |
| `DispositionCode` | `string?` | Yes / SOURCE-GATED |
| `ProviderReference` | `string?` | Yes |
| `RefundedAt` | `DateTimeOffset` | No |
| `CouponIds` | `IReadOnlyCollection<long>` | No |

No money-return identifier is required until JetPay/financial settlement integration is activated.

### `DocumentExchangeRecord`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `PredecessorElectronicTicketId` | `long` | No |
| `SuccessorElectronicTicketId` | `long` | No |
| `ExchangeChangeId` | `long` | No |
| `QuotedExchangeId` | `string` | No |
| `SourcePricingReference` | `string?` | Yes |
| `AdditionalCollection` | `decimal` | No |
| `ResidualValue` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `PenaltyAmount` | `decimal?` | Yes |
| `WaiverCode` | `string?` | Yes |
| `ProviderReference` | `string?` | Yes |
| `ExchangedAt` | `DateTimeOffset` | No |

### `DocumentRevalidationRecord`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicTicketId` | `long` | No |
| `RevalidationChangeId` | `long` | No |
| `CouponIds` | `IReadOnlyCollection<long>` | No |
| `SourceDecisionReference` | `string` | No |
| `ProviderReference` | `string?` | Yes |
| `RevalidatedAt` | `DateTimeOffset` | No |

---

# 17. Aggregate: ElectronicMiscDocument

IATA EMD-A/EMD-S semantics remain separate from ETKT and from payment rails.

## 17.1 Root fields

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `OriginalOrderId` | `long` | No |
| `CurrentServicingOrderId` | `long` | No |
| `TravellerId` | `long?` | Yes |
| `TravellerProfileRevisionId` | `long?` | Yes |
| `IssueFulfillmentTaskId` | `long` | No |
| `DocumentNumber` | `string` | No |
| `Type` | `ElectronicMiscDocumentType` | No |
| `ReasonForIssuanceCode` | `string` | No |
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

Types: `Associated` (EMD-A), `Standalone` (EMD-S).

If `TravellerId` is present, `TravellerProfileRevisionId` is required for immutable passenger history.

## 17.2 `EmdCoupon`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicMiscDocumentId` | `long` | No |
| `CouponNumber` | `int` | No |
| `Purpose` | `EmdCouponPurpose` | No |
| `OriginalOrderServiceId` | `long?` | Yes |
| `CurrentOrderServiceId` | `long?` | Yes |
| `PricingLineId` | `long?` | Yes |
| `ServiceSubCode` | `string?` | Yes / SOURCE-GATED |
| `ReasonForIssuanceSubCode` | `string` | No |
| `ExternalValueReference` | `string?` | Yes / SOURCE-GATED |
| `AssociatedTicketCouponId` | `long?` | Yes |
| `IssuanceValue` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `Status` | `EmdCouponStatus` | No |
| `ProviderCouponStatusCode` | `string?` | Yes |
| `PredecessorEmdCouponId` | `long?` | Yes |

At least one of `OriginalOrderServiceId`/`CurrentOrderServiceId` or `PricingLineId` is required for a value-bearing coupon according to its purpose.

Purposes: `Service, Fee, Deposit, ResidualValue`.  
Statuses: `OpenForUse, Void, Refunded, Exchanged`.

## 17.3 `EmdPriceLink`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `ElectronicMiscDocumentId` | `long` | No |
| `EmdCouponId` | `long?` | Yes |
| `PricingLineId` | `long` | No |
| `PricingAllocationId` | `long?` | Yes |
| `AttributedValue` | `decimal` | No |
| `CurrencyId` | `int` | No |

## 17.4 `EmdAssociationHistory`

| Field | Type | Null |
|---|---|---:|
| `Id` | `long` | No |
| `EmdCouponId` | `long` | No |
| `Action` | `EmdAssociationAction` | No |
| `TicketCouponId` | `long?` | Yes |
| `ChangeId` | `long` | No |
| `ProviderReference` | `string?` | Yes |
| `OccurredAt` | `DateTimeOffset` | No |

Actions: `Associate, Disassociate, Reassociate`.

## 17.5 Optional-service source semantics

ATPCO/source data may supply Service Sub Code, service type, Group/SubGroup, commercial name, EMD type, RFIC/RFISC and SSR relationship. Preserve only sourced values. No ATPCO code is synthesized.

---

# 18. Aggregate: DocumentStock

DocumentStock owns local accountable-document number allocation. External issuer authority may bypass local stock, but external document identity must still be retained.

## 18.1 Root fields

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

`AccountableDocumentKind`: `ElectronicTicket, ElectronicMiscDocument`.  
`DocumentStockStatus`: `Active, Suspended, Exhausted, Closed`.

## 18.2 `DocumentStockAllocation`

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

`StockNumberState`: `Reserved, Issued, Retired`.

Rules:

1. Same `(IssueFulfillmentTaskId, DocumentRole)` replay returns the same allocation.
2. Issued/retired numbers are never recycled, including after Unknown issuer outcome.
3. Random accountable-document numbers are forbidden.
4. Provider/external document authority uses `DocumentAuthority.External`; local stock is not forced where the issuer owns numbering.

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
| `AffectedSegmentIds` | `IReadOnlyCollection<long>` | No |
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

Payment/settlement facts are **not required to be duplicated in Ordering while JetPay is deferred**. Revenue Accounting may consume them directly from JetPay/LedgerFlow. If a future source-backed Ordering projection is introduced, it must preserve stable Order/CommercialVersion/OrderChange correlation and must not become the payment ledger.

Ordering still preserves seller/partner settlement references and internal/settlement values that originate from the accepted commercial/service source.

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
Payment/cash evidence from JetPay/LedgerFlow or a future source-backed Ordering projection
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
5. Append `OrderChange(Cancel)` with `ReasonCode` from the request reason and the normalized request reason detail as `ReasonText` (§9.1).
6. Mark affected Items/Services `Cancelled`; never delete them.
7. Append penalty/refund pricing lines from source decision.
8. Record any source commercial credit/refund entitlement without requiring a payment-rail call.
9. Preserve PNR, provider refs and all history. Future financial orchestration handles money movement additively.

`OrderStatus.Cancelled` is only a full-order summary when all active commercial scope is cancelled and no issued value remains requiring document servicing.

A partial cancellation may leave a truthful `Mixed` reservation root. The remaining active scope stays issueable only while its pricing atoms are intact (§7.4) and its target reservation units are `Confirmed` (§27.3).

## 22.2 Void — issued document reversal

### Command

```text
VoidDocuments
```

Rules:

- within issuer/source void authority/deadline;
- coupons must remain void-eligible and not consumed;
- ETKT/EMD state becomes Void/Voided;
- any future payment reversal is external financial evidence, not part of document-void truth;
- void does not delete original sale/issue history.

## 22.3 Refund

### Commands

```text
QuoteRefund
CommitRefund
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
| `DocumentVersions` | `IReadOnlyCollection<DocumentVersionReference>` | No |
| `ApprovedTicketCouponIds` | `IReadOnlyCollection<long>` | No |
| `ApprovedEmdCouponIds` | `IReadOnlyCollection<long>` | No |
| `ApprovedServiceIds` | `IReadOnlyCollection<long>` | No |
| `RefundAmount` | `decimal` | No |
| `CurrencyId` | `int` | No |
| `PenaltyAmount` | `decimal?` | Yes |
| `WaiverCode` | `string?` | Yes |
| `DispositionCode` | `string?` | Yes | Source-provided refund-value disposition; do not invent payment-rail semantics. |
| `ValidUntil` | `DateTimeOffset?` | Yes |

### `DocumentVersionReference`

| Field | Type | Null |
|---|---|---:|
| `DocumentKind` | `AccountableDocumentKind` | No |
| `DocumentId` | `long` | No |
| `DocumentVersion` | `int` | No |

Do not use initial-sale `IsRefundable` as final automated refund authority.

### Commit

- append `OrderChange(Refund)`;
- append refund/penalty PricingLines;
- mark exact ETKT/EMD coupons Refunded;
- close commercial Services only when no surviving document/service entitlement remains;
- preserve refund entitlement/document disposition independently of whether money movement has been integrated yet;
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
DocumentVersions: IReadOnlyCollection<DocumentVersionReference>
PredecessorTicketCouponIds: IReadOnlyCollection<long>
AffectedOrderServiceIds: IReadOnlyCollection<long>
TargetOfferId: string?
TargetSelectionReference: string?
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
4. preserve `AdditionalCollection` / `ResidualValue` from the accepted AirPrice decision; current core does not execute or gate on financial movement;
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
| `ChangeId` | `long` | No | OrderChange(Split) that created this lineage. |
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
Deposit contractual deadline/amount remains commercial evidence (no current payment execution)
        ↓
Add/ReplacePassengerNames
   materialize OrderTravellers
        ↓
Materialize passenger-specific OrderServices when airline policy requires
        ↓
Final-payment contractual deadline remains commercial evidence (no current payment execution)
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

Behaves like standard retail air transport after the source identifies the segment as `OpenCharter`. Individual passengers can hold/confirm/issue/deliver normally. Financial orchestration is external.

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
ConfirmReservedCapacity    // all eligible HoldThenConfirm reservations on the Order
ConfirmReservations        // targeted subset; same rules/planner
```

## Financial integration — FUTURE / not materialized

No Payment/JetPay command is part of the current Ordering core. When the JetPay contract is frozen, a separate Stage-P ADR may add provider-neutral orchestration/evidence commands without changing the intrinsic commands below.

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
- current `ReservationValidationEvidence` covers the reservation's Air services at the current `CommercialVersion` and time, or has been freshly revalidated (§13.6);
- no conflicting Unknown effect.

## 27.3 Issue

`FulfillmentReservation.Status` is summary only. A `Confirmed` root is sufficient but not necessary for Issue: a `Mixed` root may be issueable for a confirmed target subset. The reservation root status is never by itself an Issue gate, and a truthful `Mixed` root is never changed back to `Confirmed` to pass Issue.

Requires, for every outstanding active ticketable Air service selected by Issue:

1. the service is commercially `Active`;
2. it is not already documented by a surviving non-void ETKT coupon;
3. latest reservation coverage exists when its provider requires reservation;
4. the **exact latest `ReservationUnit` covering that service is `Confirmed`** — required issue-scope ReservationUnit(s) must be Confirmed, not the whole FulfillmentReservation root;
5. required provider unit/reference facts exist;
6. no overlapping unresolved Reserve/Confirm/Release/Cancel/Issue effect exists for the issue targets;
7. the accepted pricing topology still authorizes the scope: no pricing atom touching it is fractured (§7.4);
8. current `ReservationValidationEvidence` covers the issue scope at the current `CommercialVersion` (§13.6);
9. the applicable hard ticketing deadline is open;
10. document number authority/stock and complete ticket/EMD snapshot data exist.

The issuance scope keeps target granularity per reservation — the reservation, its exact target `ReservationUnit` ids and the issue `OrderService` ids — so no later rule can fall back to reservation-root status.

When evidence does not cover a reservation's issue scope, Issue performs a **validation-only** refresh for exactly that scope (expanded to its intact pricing atoms), persists the returned evidence, and re-checks it immediately before document allocation. The refresh never re-plans or recreates the reservation and never mutates provider resources.

`OutstandingServices` remain the Active ticketable Air services minus those covered by surviving non-void ticket coupons; a cancelled Air service is never reintroduced into Issue because it remains in historical fare or reservation structures. `Order.MarkTicketed` evaluates only Active ticketable Air services: once every remaining one is documented the Order is `Ticketed`, even while a historical reservation root stays `Mixed`.

Benchmark basis: IATA reservation procedures issue tickets according to the reservation status of each segment, not one PNR-level status; IATA servicing guidance recognizes full and partial cancellation; Amadeus ticketing accepts segment selection and rejects non-active ticketing segments rather than requiring one common state across all historical segments; changed itineraries are repriced or revalidated rather than silently reusing a fractured fare.

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
| Payment | JetPay/payment orchestrator | no current Ordering state; future source-backed evidence projection only if needed |
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

## Future financial-integration scenarios — structural compatibility only, not current Stage gates

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
**S34** issue after confirmed capacity and before ticketing deadline; future financial orchestration may gate the caller externally.  
**S35** late hold-expiry event after ticket issue must not expire ticketed Order.

## Cancel / void / ancillary

**S36** unissued full cancellation with reservation release.  
**S37** partial unissued service cancellation.  
**S38** ticket void within issuer deadline.  
**S39** void no longer eligible -> refund path.  
**S40** add chargeable baggage after initial issue -> pricing change + ancillary fulfillment + EMD; future financial orchestration handles collection externally.  
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
**S62** final-payment contractual deadline and ticketing deadline remain distinct commercial facts; individual ticket issue is structurally independent of JetPay.  
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

**S79** accepted sale and any external cash/payment fact remain distinct from revenue recognition.  
**S80** Delivered service provides source event for revenue recognition.  
**S81** agent-role service recognizes commission/internal value rather than gross value downstream.  
**S82** partner service carries source supplier cost/settlement amount.  
**S83** refund/exchange/void sends complete document/pricing/delivery lineage to accounting.  
**S84** interline Order uses upfront internal/settlement value when source supplies it; Ordering does not recompute proration.

A future Stage is not accepted if its design makes any later scenario structurally impossible without replacing the aggregate ownership defined here.

---

# 30. Stage-by-stage materialization plan

The whole domain above is the design authority. Coding remains controlled by stages. Payment integration is removed from the linear Ordering critical path.

| Stage | Materialize / complete | Explicitly do not materialize yet |
|---|---|---|
| **S1 Create — CLOSED** | Order, travellers, contacts, sold itinerary, Air/Seat services, fare topology, accepted pricing/history | reservation/documents/DCS/payment |
| **S2 Reserve — CLOSED** | RecordLocator, FulfillmentReservation/Unit, reserve/release/recovery, reservation FulfillmentTask/Interaction, deadline semantics | documents/generalized document task targets/payment |
| **S3 Confirm Reserved Capacity** | First verify per-operation provider idempotency/read-back/result semantics; then `ConfirmReservedCapacity`, stale-validation refresh, `ConfirmInventory` FulfillmentTask/evidence and truthful Held→Confirmed/Unknown behavior using the verified recovery branch | ETKT/EMD, Payment/JetPay, invented provider retry semantics |
| **S4 Issue** | ElectronicTicket, TicketCoupon, TicketPriceLink, ElectronicMiscDocument/EmdCoupon only for documentable current scope, DocumentStock, document-oriented FulfillmentTask target generalization | refund/exchange/group/DCS/payment |
| **S5 Cancel / Void** | unissued commercial cancel, reservation cancellation/release, document void history/recovery | automated refund/exchange/payment rail |
| **S6 Ancillary / EMD servicing** | optional-service subclasses needed by real offers, paid/chargeable baggage-seat-fee commercial/document behavior, EMD-A/S association | generic service catalog replication, payment rail |
| **S7 Split** | OrderLineage, transferred service/item lineage, provider/document split servicing | group split unless group Stage arrived |
| **S8 Refund** | RefundDecisionSnapshot, ETKT/EMD refund records, commercial/document refund history after AirPrice decision | actual money-return rail until JetPay Stage-P |
| **S9 Exchange / Reissue / Revalidation** | successor commercial topology/pricing, document exchange/revalidation history, EMD reassociation, additional-collection/residual-value facts | actual payment collection/refund rail until JetPay Stage-P |
| **S10 Group + Charter** | PassengerGroup/terms/deposit contractual facts, ReservationGroupSpace, OrderTimeLimit group scopes, charter operation type | payment execution |
| **S11 Delivery / DCS** | OrderDeliveryObservation and authoritative document-usage mapping | running DCS itself |
| **S12 Disruption** | OrderDisruptionImpact + involuntary servicing flows | crew/aircraft recovery |
| **S13 Accounting / Interline hardening** | source-backed accounting snapshots, settlement/internal values, downstream fact contract | Revenue Accounting ledger/proration engine; payment ledger |
| **Stage P — JetPay Integration (later, independent)** | external financial orchestration/gates + optional source-backed financial coverage projection/correlation | redesign of Order/Reservation/Document aggregates |

### Stage-P compatibility contract

Stage P is considered additive only if it can be implemented by orchestrating existing commands and optionally adding source-backed financial evidence. It MUST NOT require changes to the intrinsic meaning of `ConfirmReservedCapacity`, `IssueOrder`, ticket/EMD fields, reservation ownership, refund/exchange decision snapshots, DCS or disruption state.

Before each Stage, re-benchmark that Stage against current IATA/ATPCO/public Amadeus/Sabre behavior and actual AeroTech source contracts. Historical donor code is never automatically authoritative.

---

# 31. Stage 2 closure — FINAL

Reviewed current branch: `k8s-stg@85f48bb652d96ce155c96ee07045223656db1ecb` (`Stage 2- Final`).

```text
STAGE_2_CLOSED
```

Frozen outcomes:

- `FulfillmentReservation` is business reservation/resource truth observed by Ordering.
- `ReservationUnit` covers explicit 1..N OrderService occurrences.
- `FulfillmentTask` owns external-effect execution/recovery; ProviderInteraction remains its child evidence.
- Unknown replay uses the exact persisted original request and same external-effect identity.
- definitive terminal retry creates a new reservation/idempotency and fresh AirPrice validation.
- `Order.RecordLocator` is host PNR, distinct from provider operation/unit/supplier locators.
- `ReservationValidationTimeLimit` is stale-able validation evidence, not Order expiry.
- `LastTicketingDate` is the current hard commercial ticketing deadline.
- provider hold `ExpiresAt` is distinct from both.
- source `PricingUnit` vocabulary is preserved losslessly via `SourceKind + SemanticType`.
- FlightFlow automatic expiry/INF/revenue mappings remain provider-contract assumptions as documented by Stage-2 handoff; they do not alter generic Ordering domain semantics.

Stage 2 is not reopened by Stage 3+ unless a real provider/source contradiction is demonstrated.

---

# 32. Non-negotiable no-guess rules for Coding Agent

1. Never add a domain field, entity, enum value or relationship because it makes implementation easier.
2. Never use donor v1/v2/v3 as source authority.
3. Never mutate a real provider/source DTO or captured payload to manufacture data needed by a desired model.
4. Preserve unknown upstream vocabulary verbatim plus a safe semantic mapping; do not force it into an invented enum.
5. Never turn `Unknown` external outcome into failure by timeout alone.
6. Never rebuild an Unknown mutation request from current mutable Order state. If the provider supports idempotent mutation replay, replay the persisted exact intent with the same provider key. If it does not, do **not** blind-retry; keep Unknown and reconcile/apply authoritative evidence.
7. Never use `OrderStatus` as the sole eligibility rule.
8. Never collapse Offer expiry, fare/validation validity, ticketing deadline, provider hold expiry, future payment expiry and group deadlines into one timestamp.
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

# 34. Full-horizon field/scenario sufficiency audit — v2.0

This audit was performed against the complete scenario catalogue through Accounting/Interline, not only S3.

| Domain area | Field-shape result | Later scenarios protected |
|---|---|---|
| Commercial Order / SalesContext | PASS; compact current-field sections expanded field-by-field | B2C/agency/corporate/partner, office/PCC/IATA context |
| Fare topology | PASS; source vocabulary retained losslessly | roundtrip/one-way/open-jaw, refund/exchange source context |
| Pricing ledger | PASS; ROE rounding-factor remains explicitly source-gated until upstream precision semantics are confirmed | tax/YQ-YR/fee/discount/markup/commission/ROE/accounting |
| Reservation | **FIXED** supplier `ProviderRecordLocator`; group coverage restored | multi-provider, partner PNR, group/charter |
| FulfillmentTask | **FIXED** removed undefined generic BusinessOperation; typed targets retained | confirm/issue/void/recovery |
| Payment | **REDESIGNED BOUNDARY** | later JetPay can gate workflows additively without changing core domain |
| ETKT | **FIXED** immutable traveller revision, issuance context, NVA/NVB, baggage/RBD/cabin snapshots, typed price links; sold CabinClass has a confirmed current-source persistence gap to close additively before issue | issue, split, DCS, refund, exchange, RA/interline |
| EMD | **FIXED** immutable traveller revision, source RFIC/RFISC/service link, typed price links, association history | ancillary, exchange, refund, RA |
| DocumentStock | **FIXED** exact accountable kind/status/state + issue-task correlation | lost response/idempotent issue |
| Refund | **FIXED** exact document version refs, separate ticket/EMD coupon scopes, source disposition code | full/partial/no-show/ancillary refund |
| Exchange | **FIXED** exact stale guards and target references; payment delta retained only as pricing fact | voluntary/involuntary exchange/reissue |
| Group | **FIXED** deposit amount/percentage/disposition source facts + separate deadlines | unnamed groups, deposit/final-payment contracts, charter |
| DCS | PASS | check-in/boarding/delivery/no-show/actual seat |
| Disruption | **FIXED** multi-segment impact scope | cancellation/schedule/misconnection/reaccommodation |
| Accounting/interline | PASS with payment facts sourced separately from JetPay/LedgerFlow | RA, partner cost, interline settlement |

## 34.1 Exact enum vocabulary required by future Stages

Current repository values retained where already authoritative:

```text
DocumentAuthority = Local | External
ElectronicTicketStatus = Issued | PartiallyUsed | Used | Voided | Exchanged | Refunded | Suspended
TicketCouponFinancialStatus = Open | Used | Void | Exchanged | Refunded | Suspended
TicketCouponControlStatus = Local | External | ReleasePending | Unknown
ElectronicMiscDocumentType = Associated | Standalone
ElectronicMiscDocumentStatus = Issued | Voided | Refunded | Exchanged
EmdCouponPurpose = Service | Fee | Deposit | ResidualValue
EmdCouponStatus = OpenForUse | Void | Refunded | Exchanged
AccountableDocumentKind = ElectronicTicket | ElectronicMiscDocument
DocumentStockStatus = Active | Suspended | Exhausted | Closed
StockNumberState = Reserved | Issued | Retired
FarePricingUnitType = Unspecified | OneWay | RoundTrip | OpenJaw | CircleTrip | Other
BaggageWeightUnit = Kg | Lbs
```

Future Master enums not yet materialized:

```text
DocumentRefundType = Full | Partial | Involuntary | Other
EmdAssociationAction = Associate | Disassociate | Reassociate
OrderLineageType = SplitParent | SplitChild
ServiceDeliveryStatus = ReadyToProceed | ReadyToDeliver | InProgress | Delivered | NotClaimed | FailedToDeliver | UnableToDeliver | Expired | Suspended | Removed | Unknown
DisruptionEventType = FlightCancellation | ScheduleChange | Delay | FlightNumberChange | AirportChange | EquipmentChange | Misconnection | Other
PassengerGroupType = StandardGroup | TourGroup | SalesAllotment | ClosedCharter | Other
FlightOperationType = Scheduled | OpenCharter | ClosedCharter | Other
OrderTimeLimitType = Ticketing | Payment | PassengerName | Deposit | FinalPayment | Other
OrderTimeLimitScope = Order | OrderItem | PassengerGroup
SpecialServiceRequestStatus = Requested | Confirmed | Rejected | Cancelled | Unknown
CommissionRecipientType = Agency | Corporate | Airline | Partner | Other
```

`OrderTimeLimitType.Payment` is a contractual/order deadline placeholder and does not activate JetPay integration.

## 34.2 Additional completeness scenarios

**S85** supplier reservation returns a supplier PNR/record locator distinct from operation/hold reference.  
**S86** one traveller requires more than one ETKT because issuer/document capability splits coupon scope; no assumption that one traveller always means one ticket.  
**S87** traveller name/profile changes after issue; historical ETKT/EMD continues to reference the issuance profile revision.  
**S88** misconnection/disruption affects more than one sold segment in one impact record.  
**S89** group contract supplies deposit percentage/amount and disposition rule while actual payment execution is external.  
**S90** commercial refund/document disposition completes while money-return rail is not integrated; history remains explicit and later settlement can be correlated additively.  
**S91** exchange has positive additional collection but current payment-neutral core preserves the financial delta and completes only according to the current development policy; future JetPay orchestration can gate invocation without changing exchange records.  
**S92** future JetPay integration is added and no existing Order/Reservation/ETKT/EMD field or ownership must be replaced.
**S93** traveller loyalty/FQTV identity is preserved for servicing/benefit recognition without implying points-as-payment.
**S94** Order contact/phone/email facts can be corrected or extended without rewriting traveller/document history.
**S95** SSR/OSI such as assistance/meal/request is represented separately from a chargeable ancillary unless a real offer makes it a priced Service.
**S96** international traveller document/passport/visa facts are retained from source and remain distinct from mutable passenger profile and issued-document snapshots.
**S97** a `ReservationMode=None` service can proceed to its document/fulfillment Stage without fabricating a reservation.
**S98** an `ImmediateConfirm` provider records confirmed resource truth directly without a fake Held state.
**S99** an external document issuer may supply authoritative ETKT/EMD numbers; local DocumentStock is not forced when authority is external.
**S100** interline/external coupon control transfer changes `TicketCouponControlStatus` without rewriting financial coupon history.

---

# 35. Field-to-scenario traceability — mandatory roadmap guardrail

This appendix is the implementation-safety map requested by the Owner. Every field listed in the canonical field tables above is tied to the scenario families that justify its presence. It does **not** authorize early implementation: `FUTURE` and `SOURCE-GATED` fields remain unmaterialized until their owning Stage/source arrives.

A field may serve more scenarios than listed; the table records the minimum scenarios that justify keeping its destination and semantics stable. No Coding Agent may add a field merely because it appears convenient for a provider payload.

## 6.1 `Order` root

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `OrderReference` | `Guid` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `OwnerAirlineId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S01-S20, S28-S100 |
| `CustomerId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `SalesContext` | `SalesContext` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `CurrencyId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `SourceOfferId` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `RecordLocator` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `LastTicketingDate` | `DateTimeOffset?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `Status` | `OrderStatus` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `CommercialVersion` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `CustomerTotal` | `decimal` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S100 |
## 6.2 `SalesContext`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Channel` | `SalesChannel` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `ContextType` | `CallerContextType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `PrincipalType` | `CallerPrincipalType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `ActorId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `AirlineUserId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `TravelAgencyId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `TravelAgencyUserId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `IndividualId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `PartnerApiAccessProfileId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `OfficeKind` | `SellingOfficeKind?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `OfficeId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `CorporateAccountId` | `long?` | SOURCE-GATED | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `PartnerOrganizationId` | `long?` | SOURCE-GATED | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `AgencyIataNumber` | `string?` | SOURCE-GATED | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `Pcc` | `string?` | SOURCE-GATED | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
| `PointOfSaleCountryId` | `int?` | SOURCE-GATED | S01, S07-S08, S29, S34, S58-S66, S78-S84 |
## 6.3 `OrderItem`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S36-S66, S79-S84 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S36-S66, S79-S84 |
| `Kind` | `ProductType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S36-S66, S79-S84 |
| `SourceOfferItemRef` | `string?` | SOURCE-GATED | S01-S08, S36-S66, S79-S84 |
| `PassengerGroupId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S01-S08, S36-S66, S79-S84 |
| `AcceptedTotal` | `decimal` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S36-S66, S79-S84 |
| `CommercialStatus` | `OrderItemCommercialState` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S36-S66, S79-S84 |
| `CreatedByChangeId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S36-S66, S79-S84 |
| `EndedByChangeId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S01-S08, S36-S66, S79-S84 |
| `PredecessorOrderItemId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S01-S08, S36-S66, S79-S84 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S36-S66, S79-S84 |
## 6.4 Abstract `OrderService`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `OrderItemId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `TravellerId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `ServiceType` | `OrderServiceType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `SourceServiceRef` | `string?` | SOURCE-GATED | S01-S20, S28-S84, S97-S100 |
| `FulfillmentProviderKey` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `ResponsibleAirlineId` | `int?` | SOURCE-GATED | S01-S20, S28-S84, S97-S100 |
| `ValidatingCarrierId` | `int?` | SOURCE-GATED | S01-S20, S28-S84, S97-S100 |
| `DeliveryProviderReference` | `string?` | SOURCE-GATED | S01-S20, S28-S84, S97-S100 |
| `AccountingSnapshot` | `ServiceAccountingSnapshot?` | SOURCE-GATED | S01-S20, S28-S84, S97-S100 |
| `CommercialStatus` | `OrderServiceCommercialState` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `CreatedByChangeId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
| `EndedByChangeId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S01-S20, S28-S84, S97-S100 |
| `PredecessorOrderServiceId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S01-S20, S28-S84, S97-S100 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S84, S97-S100 |
## 6.5 `OrderAirTransportService`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `SegmentId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `FlightCapacityId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `AirFareId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `BookingClass` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `FareBasis` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `FareFamily` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `FareType` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `RbdId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `CabinClassId` | `long?` | AEROTECH-CONTRACT-VERIFIED source; additive persistence correction required | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `CheckedBaggageAllowance` | `BaggageAllowance?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `CabinBaggageAllowance` | `BaggageAllowance?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `IsRefundable` | `bool` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `IsChangeable` | `bool` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
| `IsUpgradable` | `bool` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S20, S28-S29, S34-S40, S42-S57, S67-S84 |
## 6.6 `OrderSeatService`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `SegmentId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S10, S30, S40, S45, S68, S72 |
| `AssociatedAirServiceId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S10, S30, S40, S45, S68, S72 |
| `SeatNumber` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S10, S30, S40, S45, S68, S72 |
## 6.7 `OrderAncillaryService` — future airline optional service

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `ServiceDefinitionRef` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `ServiceSubCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `ServiceTypeCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `GroupCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `SubGroupCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `CommercialName` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `SsrCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `Quantity` | `decimal` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `UnitCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `CoveredAirServiceIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `Refundability` | `RefundabilityRule?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `Reusable` | `bool?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `FormOfRefundCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `Commissionable` | `bool?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
| `InterlineSettlementAllowed` | `bool?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S30-S31, S40, S45, S48, S55, S72, S78-S84, S95 |
## 6.8 `OrderBaggageService` — future specialization

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Pieces` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S08, S30, S40, S48, S72 |
| `Weight` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S08, S30, S40, S48, S72 |
| `WeightUnit` | `BaggageWeightUnit?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S08, S30, S40, S48, S72 |
| `BaggageCategoryCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S08, S30, S40, S48, S72 |
| `PrepaidIndicator` | `bool?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S08, S30, S40, S48, S72 |
## 6.9 `OrderPartnerService` — hotel/ground/insurance/third-party future service

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `SupplierProductReference` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `ServiceDefinitionRef` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `StartAt` | `DateTimeOffset?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `EndAt` | `DateTimeOffset?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `StartLocationRef` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `EndLocationRef` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `Quantity` | `decimal` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `UnitCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
| `AssociatedAirServiceIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S41, S73-S78, S82 |
## 6.10 `OrderTraveller`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `Index` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `SourceTravellerRef` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `PassengerType` | `PassengerTypeCode` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `AgeRange` | `AgeRange` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `InfantParentTravellerId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `CurrentProfileRevisionId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `Status` | `OrderTravellerStatus` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
| `PassengerGroupId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S01, S06, S19, S28-S29, S42-S43, S58-S66, S87, S93, S96 |
## `TravellerProfileRevision`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `TravellerId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `GivenName` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `Surname` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `NoSurname` | `bool` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `DateOfBirth` | `DateOnly?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `Gender` | `Gender?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `NationalityId` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `CountryOfResidenceId` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `CreatedByChangeId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `SupersededByChangeId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S06, S28-S29, S42-S43, S59-S60, S87, S96 |
## `OrderTravellerDocument`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S28-S29, S67-S72, S87, S96 |
| `OrderTravellerId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S28-S29, S67-S72, S87, S96 |
| `Type` | `TravellerDocumentType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S28-S29, S67-S72, S87, S96 |
| `Number` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S28-S29, S67-S72, S87, S96 |
| `ExpiryDate` | `DateOnly?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S28-S29, S67-S72, S87, S96 |
| `IssuanceCountryId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S28-S29, S67-S72, S87, S96 |
| `Holder` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S28-S29, S67-S72, S87, S96 |
| `IssueDate` | `DateOnly?` | SOURCE-GATED | S28-S29, S67-S72, S87, S96 |
| `ApplicableCountryId` | `int?` | SOURCE-GATED | S28-S29, S67-S72, S87, S96 |
| `SourceDocumentRef` | `string?` | SOURCE-GATED | S28-S29, S67-S72, S87, S96 |
## `OrderTravellerLoyaltyAccount` — future/source-gated

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S93 |
| `OrderTravellerId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S93 |
| `ProgramOwnerAirlineId` | `int?` | SOURCE-GATED | S93 |
| `ProgramCode` | `string` | SOURCE-GATED | S93 |
| `MemberNumber` | `string` | SOURCE-GATED | S93 |
| `TierCode` | `string?` | SOURCE-GATED | S93 |
| `SourceReference` | `string?` | SOURCE-GATED | S93 |
## `OrderContact`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `Sequence` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `Role` | `ContactRole` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `ContactName` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `ContactPoints` | `collection` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
## `OrderContactPoint`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `OrderContactId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `Type` | `ContactPointType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `Value` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `CountryCode` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
| `IsPrimary` | `bool` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S07, S58, S65, S94 |
## `OrderRemark`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `Type` | `OrderRemarkType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `Visibility` | `OrderRemarkVisibility` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `Scope` | `OrderRemarkScope` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `TravellerId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `SegmentId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `OrderItemId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `OrderServiceId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `Text` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `CategoryCode` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `IsPrintedOnItinerary` | `bool` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `IsPrintedOnInvoice` | `bool` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `Status` | `OrderRemarkStatus` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `SupersedesRemarkId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `CreatedBy` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01, S36-S57, S73-S78, S94-S95 |
## `OrderJourney`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S42-S57, S64-S78, S84 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S42-S57, S64-S78, S84 |
| `Sequence` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S42-S57, S64-S78, S84 |
| `BoundId` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S42-S57, S64-S78, S84 |
| `OriginAirportId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S42-S57, S64-S78, S84 |
| `DestinationAirportId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S42-S57, S64-S78, S84 |
## `OrderSegment`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `OrderJourneyId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `Sequence` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `FlightId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `FlightVersion` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `FlightNumber` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `OriginAirportId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `OriginAirportTerminalId` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `DestinationAirportId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `DestinationAirportTerminalId` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `OperatingAirlineId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `MarketingAirlineId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `SoldDeparture` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `SoldArrival` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `Duration` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `AircraftId` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `OperationType` | `FlightOperationType` | SOURCE-GATED | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
| `SourceSegmentRef` | `string?` | SOURCE-GATED | S01-S05, S11, S28, S52-S57, S64-S78, S84, S88 |
## `OrderSegmentLeg`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `OrderSegmentId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `Sequence` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `LegId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `OriginAirportId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `OriginAirportTerminalId` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `DestinationAirportId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `DestinationAirportTerminalId` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `DepartureDateTime` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `ArrivalDateTime` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `StopType` | `StopType?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `StopDurationMinutes` | `int?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
| `StopPassengersCanBoardOrLeave` | `bool?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S02, S11, S28, S67-S78 |
## 6.13 `OrderSpecialServiceRequest` — future SSR/OSI business record

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `OrderId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `TravellerId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `CoveredSegmentIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `Code` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `Text` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `Status` | `SpecialServiceRequestStatus` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `RawStatusCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `CreatedByChangeId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
| `CreatedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S40, S67-S72, S95 |
## 7.2 `OrderFarePricingUnit`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S16, S46-S57, S79-S84 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S16, S46-S57, S79-S84 |
| `CreatedByChangeId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S16, S46-S57, S79-S84 |
| `Sequence` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S16, S46-S57, S79-S84 |
| `SourceKind` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S01-S05, S11, S16, S46-S57, S79-S84 |
| `SemanticType` | `FarePricingUnitType` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S01-S05, S11, S16, S46-S57, S79-S84 |
| `SourcePricingUnitRef` | `string?` | SOURCE-GATED | S01-S05, S11, S16, S46-S57, S79-S84 |
| `CoveredJourneyIds` | `IReadOnlyCollection<long>` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S16, S46-S57, S79-S84 |
| `FareComponents` | `collection` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S16, S46-S57, S79-S84 |
## 7.3 `OrderFareComponent`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `OrderFarePricingUnitId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `Sequence` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `SourceComponentRef` | `string?` | SOURCE-GATED | S01-S05, S11, S28, S46-S57, S79-S84 |
| `AirFareId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `BookingClass` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `FareBasis` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `FareFamily` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `FareType` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
| `CoveredOrderServiceIds` | `IReadOnlyCollection<long>` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S05, S11, S28, S46-S57, S79-S84 |
## 8.1 `PricingLine`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `CreatedByChangeId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Reason` | `OrderPricingReason` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Scope` | `PricingLineScope` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Category` | `OrderPricingLineCategory` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `SubCategory` | `OrderPricingLineSubCategory` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Direction` | `OrderPricingLineDirection` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Treatment` | `PricingLineTreatment` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Code` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Description` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Reference` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Amount` | `decimal` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `CurrencyId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `EquivalentAmount` | `decimal` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `EquivalentCurrencyId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `ExchangeRateSnapshot` | `ExchangeRateSnapshot?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Refundability` | `RefundabilityRule?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `Allocations` | `collection` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `OwnerCarrierId` | `int?` | SOURCE-GATED | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `TaxJurisdiction` | `TaxJurisdictionSnapshot?` | SOURCE-GATED | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `CommissionDetail` | `CommissionSnapshot?` | SOURCE-GATED | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
| `SourceAccountingReference` | `string?` | SOURCE-GATED | S01-S08, S30-S31, S36-S57, S58-S66, S79-S84, S89-S91 |
## `TaxJurisdictionSnapshot`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `CountryId` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S08, S46-S57, S79-S84 |
| `StationId` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S08, S46-S57, S79-S84 |
## `CommissionSnapshot`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `RecipientType` | `CommissionRecipientType` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S07-S08, S79-S84 |
| `RecipientReference` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S07-S08, S79-S84 |
| `Rate` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S07-S08, S79-S84 |
| `BasisAmount` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S07-S08, S79-S84 |
| `BasisCurrencyId` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S07-S08, S79-S84 |
| `SourceReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S07-S08, S79-S84 |
## 8.2 `PricingAllocation`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `PricingLineId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `OrderItemId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `OrderServiceId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `OrderJourneyId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `OrderSegmentId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `TravellerId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `Amount` | `decimal` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `CurrencyId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `EquivalentAmount` | `decimal` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
| `EquivalentCurrencyId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S01-S08, S30-S31, S36-S57, S79-S84 |
## 8.3 `ExchangeRateSnapshot`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `FromCurrencyId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S08, S46-S57, S79-S84 |
| `ToCurrencyId` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S08, S46-S57, S79-S84 |
| `Rate` | `decimal` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S08, S46-S57, S79-S84 |
| `DecimalPlaces` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S08, S46-S57, S79-S84 |
| `PeriodId` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S08, S46-S57, S79-S84 |
## 8.4 `ServiceAccountingSnapshot` — future/source-gated

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `InternalValueAmount` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
| `InternalValueCurrencyId` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
| `SettlementAmount` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
| `SettlementCurrencyId` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
| `SettlementCarrierId` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
| `InterlineSettlementCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
| `AgreementReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
| `SourceReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S78-S84 |
## 9.1 `OrderChange`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S36-S100 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S36-S100 |
| `ChangeType` | `OrderChangeType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S36-S100 |
| `CommercialVersion` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S36-S100 |
| `ActorContext` | `SalesContext` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S36-S100 |
| `SourceReference` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S36-S100 |
| `CommittedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S36-S100 |
| `SourceSystem` | `string?` | SOURCE-GATED | S36-S100 |
| `ReasonCode` | `string?` | SOURCE-GATED | S36-S100 |
| `ReasonText` | `string?` | OWNER-DECISION (Stage 5 R2) | S36-S100 |
| `IsInvoluntary` | `bool` | BENCHMARKED-DOMAIN; materialize in owning Stage | S36-S100 |
| `WaiverCode` | `string?` | SOURCE-GATED | S36-S100 |
## 10.1 `OrderTimeLimit` — future Order child

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `OrderId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `Type` | `OrderTimeLimitType` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `Scope` | `OrderTimeLimitScope` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `OrderItemId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `PassengerGroupId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `DueAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `SourceSystem` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `SourceReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `SourceVersion` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `CreatedByChangeId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `SupersededByTimeLimitId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
| `CreatedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S15-S18, S58-S63, S89 |
## 11.1 `PassengerGroup` — Order child, not aggregate root

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `OrderId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `GroupName` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `IntendedPassengerQuantity` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `GroupType` | `PassengerGroupType` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `AgreementReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `OrganizerCustomerId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `CommercialStatus` | `OrderItemCommercialState` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `CreatedByChangeId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `EndedByChangeId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `CreatedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `Terms` | `PassengerGroupTerms?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
## 11.2 `PassengerGroupTerms`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `MinimumPassengerQuantity` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `MaximumPassengerQuantity` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `TravelTogetherRequired` | `bool?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `SubstitutionAllowed` | `bool?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `IndividualTravelAllowed` | `bool?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `SourcePricingReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `DepositAmount` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `DepositCurrencyId` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `DepositPercentage` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `DepositDispositionCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
## 13.1 Root fields

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `FulfillmentProviderKey` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `Mode` | `ReservationMode` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `IdempotencyKey` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `CorrelationReference` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `ProviderOperationRef` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `ProviderRecordLocator` | `string?` | SOURCE-GATED | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `Status` | `FulfillmentReservationStatus` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `RequestedExpiresAt` | `DateTimeOffset?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `ValidationEvidence` | `ReservationValidationEvidence?` | OWNER-DECISION (Stage 5 R2) / BENCHMARKED-DOMAIN | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `ReservationValidationTimeLimit` | `DateTimeOffset?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE; compatibility projection since Stage 5 R2 | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `ExpiresAt` | `DateTimeOffset?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
| `LastObservedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S78, S85, S97-S98 |
## 13.2 `ReservationUnit`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `FulfillmentReservationId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `UnitCorrelationKey` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `OrderServiceIds` | `IReadOnlyCollection<long>` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `Status` | `ReservationMemberStatus` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `ProviderUnitRef` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `RawStatusCode` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `ObservedSeat` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S34-S45, S52-S57, S73-S78 |
| `ProviderValidUntil` | `DateTimeOffset?` | SOURCE-GATED | S09-S20, S34-S45, S52-S57, S73-S78 |
## 13.3 `ReservationGroupSpace` — future group capacity child

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `FulfillmentReservationId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `PassengerGroupId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `CoveredJourneyIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `CoveredSegmentIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `UnitCorrelationKey` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `RequestedPassengerQuantity` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `ReservedPassengerQuantity` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `ProviderGroupRef` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `CabinClassId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `RbdId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `Status` | `ReservationMemberStatus` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `RawStatusCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
| `ProviderValidUntil` | `DateTimeOffset?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S58-S66, S89 |
## 14.1 Final root shape

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `OrderId` | `long` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `FulfillmentReservationId` | `long?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `TaskType` | `OrderFulfillmentTaskType` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `FulfillmentProviderKey` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `IdempotencyKey` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `CorrelationReference` | `string` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `Status` | `OrderFulfillmentStatus` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `AttemptCount` | `int` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `CreatedAt` | `DateTimeOffset` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `CompletedAt` | `DateTimeOffset?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `LastFailureKind` | `FulfillmentFailureKind?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `LastFailureReason` | `FulfillmentFailureReason?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
| `LastError` | `string?` | CURRENT-CODE-VERIFIED / CLOSED-STAGE | S09-S20, S32-S45, S52-S57, S73-S78, S97-S100 |
## 14.2 `FulfillmentTaskTarget`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S28-S45, S52-S57, S73-S78, S97-S100 |
| `FulfillmentTaskId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S28-S45, S52-S57, S73-S78, S97-S100 |
| `TargetKind` | `FulfillmentTargetKind` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S28-S45, S52-S57, S73-S78, S97-S100 |
| `TargetId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S28-S45, S52-S57, S73-S78, S97-S100 |
| `Action` | `OrderFulfillmentTargetAction` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S28-S45, S52-S57, S73-S78, S97-S100 |
## 14.4 `ProviderInteraction`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `FulfillmentTaskId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `FulfillmentTaskAttemptId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `AttemptNumber` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `Sequence` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `FulfillmentProviderKey` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `InteractionType` | `ProviderInteractionType` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `IdempotencyKey` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `CorrelationReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `RequestPayload` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `RequestHash` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `ResponsePayload` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `ResponseHash` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `ProviderOperationRef` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `Status` | `ProviderInteractionStatus` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `StartedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `CompletedAt` | `DateTimeOffset?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `ProviderStatusCode` | `int?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
| `Error` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S09-S20, S32-S41, S52-S57, S73-S78, S97-S100 |
## 16.1 Root fields

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `OriginalOrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `CurrentServicingOrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `TravellerId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `TravellerProfileRevisionId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `IssueFulfillmentTaskId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `DocumentNumber` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `IssuanceContext` | `DocumentIssuanceContext` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `Authority` | `DocumentAuthority` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `IssuedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `VoidDeadline` | `DateTimeOffset?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `IssuedTotal` | `decimal` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `StatusSummary` | `ElectronicTicketStatus` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `DocumentVersion` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `PredecessorElectronicTicketId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `PredecessorExchangeChangeId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `Coupons` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `PriceLinks` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `VoidRecord` | `DocumentVoidRecord?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `RefundRecords` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `ExchangeRecords` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
| `RevalidationRecords` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S39, S42-S57, S67-S84, S86-S87, S99-S100 |
## `DocumentIssuanceContext`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `IssuerCarrierId` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S28-S35, S65-S66, S78-S84, S99 |
| `ValidatingCarrierId` | `int?` | SOURCE-GATED | S28-S35, S65-S66, S78-S84, S99 |
| `IssuingOfficeId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S28-S35, S65-S66, S78-S84, S99 |
| `IssuedByActorId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S28-S35, S65-S66, S78-S84, S99 |
| `TravelAgencyId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S28-S35, S65-S66, S78-S84, S99 |
| `AgencyIataNumber` | `string?` | SOURCE-GATED | S28-S35, S65-S66, S78-S84, S99 |
| `Pcc` | `string?` | SOURCE-GATED | S28-S35, S65-S66, S78-S84, S99 |
| `SalesChannel` | `SalesChannel?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S28-S35, S65-S66, S78-S84, S99 |
| `SourceFormOfPaymentCode` | `string?` | SOURCE-GATED | S28-S35, S65-S66, S78-S84, S99 |
## 16.2 `TicketCoupon`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `TicketId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `CouponNumber` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `OriginalOrderServiceId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `CurrentOrderServiceId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `OrderSegmentId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `OrderFareComponentId` | `long?` | SOURCE-GATED | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `PredecessorTicketCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `IssuedSegment` | `IssuedSegmentSnapshot` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `FareBasisSnapshot` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `BookingClassSnapshot` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `RbdIdSnapshot` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `CabinClassIdSnapshot` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `BaggageAllowanceSnapshot` | `BaggageAllowance?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `IssuanceValue` | `decimal` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `FinancialStatus` | `TicketCouponFinancialStatus` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `ControlStatus` | `TicketCouponControlStatus` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `ProviderCouponStatusCode` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `NotValidBefore` | `DateOnly?` | SOURCE-GATED | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `NotValidAfter` | `DateOnly?` | SOURCE-GATED | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `UsedAt` | `DateTimeOffset?` | SOURCE-GATED | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
| `UsageReference` | `string?` | SOURCE-GATED | S28-S29, S35, S38-S39, S42-S57, S67-S84, S86-S87, S100 |
## 16.3 `IssuedSegmentSnapshot`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `MarketingAirlineId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `OperatingAirlineId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `FlightNumber` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `OriginAirportId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `DestinationAirportId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `DepartureDateTime` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `ArrivalDateTime` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `BookingClass` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `RbdId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `CabinClassId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
| `SourceSegmentReference` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S52-S57, S67-S78, S87 |
## 16.4 `TicketPriceLink`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S46-S57, S79-S84 |
| `ElectronicTicketId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S46-S57, S79-S84 |
| `TicketCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S46-S57, S79-S84 |
| `PricingLineId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S46-S57, S79-S84 |
| `PricingAllocationId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S46-S57, S79-S84 |
| `AttributedValue` | `decimal` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S46-S57, S79-S84 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S29, S46-S57, S79-S84 |
## `DocumentVoidRecord`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `VoidFulfillmentTaskId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S38-S39, S79-S84 |
| `ReasonCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S38-S39, S79-S84 |
| `ReasonText` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S38-S39, S79-S84 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S38-S39, S79-S84 |
| `ActorId` | `long?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S38-S39, S79-S84 |
| `VoidedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S38-S39, S79-S84 |
## `DocumentRefundRecord`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `ElectronicTicketId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `RefundChangeId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `QuotedRefundId` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `PricingSource` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `SourcePricingReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `RefundType` | `DocumentRefundType` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `ApprovedAmount` | `decimal` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `PenaltyAmount` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `WaiverCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `DispositionCode` | `string?` | SOURCE-GATED | S46-S51, S75, S79-S84, S90 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `RefundedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
| `CouponIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S79-S84, S90 |
## `DocumentExchangeRecord`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `PredecessorElectronicTicketId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `SuccessorElectronicTicketId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `ExchangeChangeId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `QuotedExchangeId` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `SourcePricingReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `AdditionalCollection` | `decimal` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `ResidualValue` | `decimal` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `PenaltyAmount` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `WaiverCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
| `ExchangedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S52-S57, S74, S77-S84, S91 |
## `DocumentRevalidationRecord`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S56, S76-S77, S79-S84 |
| `ElectronicTicketId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S56, S76-S77, S79-S84 |
| `RevalidationChangeId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S56, S76-S77, S79-S84 |
| `CouponIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S56, S76-S77, S79-S84 |
| `SourceDecisionReference` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S56, S76-S77, S79-S84 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S56, S76-S77, S79-S84 |
| `RevalidatedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S56, S76-S77, S79-S84 |
## 17.1 Root fields

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `OriginalOrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `CurrentServicingOrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `TravellerId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `TravellerProfileRevisionId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `IssueFulfillmentTaskId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `DocumentNumber` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `Type` | `ElectronicMiscDocumentType` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `ReasonForIssuanceCode` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `IssuanceContext` | `DocumentIssuanceContext` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `IssuedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `IssuedTotal` | `decimal` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `StatusSummary` | `ElectronicMiscDocumentStatus` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `DocumentVersion` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `PredecessorEmdId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `Coupons` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `PriceLinks` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `RefundRecords` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `ExchangeRecords` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
| `AssociationHistory` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84, S87, S99 |
## 17.2 `EmdCoupon`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `ElectronicMiscDocumentId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `CouponNumber` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `Purpose` | `EmdCouponPurpose` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `OriginalOrderServiceId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `CurrentOrderServiceId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `PricingLineId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `ServiceSubCode` | `string?` | SOURCE-GATED | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `ReasonForIssuanceSubCode` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `ExternalValueReference` | `string?` | SOURCE-GATED | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `AssociatedTicketCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `IssuanceValue` | `decimal` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `Status` | `EmdCouponStatus` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `ProviderCouponStatusCode` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
| `PredecessorEmdCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S45, S48, S55, S72, S79-S84 |
## 17.3 `EmdPriceLink`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S48, S55, S79-S84 |
| `ElectronicMiscDocumentId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S48, S55, S79-S84 |
| `EmdCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S48, S55, S79-S84 |
| `PricingLineId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S48, S55, S79-S84 |
| `PricingAllocationId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S48, S55, S79-S84 |
| `AttributedValue` | `decimal` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S48, S55, S79-S84 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30-S31, S40, S48, S55, S79-S84 |
## 17.4 `EmdAssociationHistory`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30, S45, S55, S79-S84 |
| `EmdCouponId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30, S45, S55, S79-S84 |
| `Action` | `EmdAssociationAction` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30, S45, S55, S79-S84 |
| `TicketCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30, S45, S55, S79-S84 |
| `ChangeId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30, S45, S55, S79-S84 |
| `ProviderReference` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30, S45, S55, S79-S84 |
| `OccurredAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S30, S45, S55, S79-S84 |
## 18.1 Root fields

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `OwnerAirlineId` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `OfficeId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `DocumentKind` | `AccountableDocumentKind` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `Prefix` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `SerialWidth` | `int` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `CheckDigitProfile` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `RangeFrom` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `RangeTo` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `NextNumber` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `Status` | `DocumentStockStatus` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
| `Allocations` | `collection` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40, S99 |
## 18.2 `DocumentStockAllocation`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `DocumentStockId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `IssueFulfillmentTaskId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `DocumentRole` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `Serial` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `DocumentNumber` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `State` | `StockNumberState` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `AllocatedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
| `SettledAt` | `DateTimeOffset?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S28-S33, S38-S40 |
## 19.1 `OrderDeliveryObservation` — append-only Order child

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `OrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `OrderServiceId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `TravellerId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `SegmentId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `TicketCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `EmdCouponId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `Status` | `ServiceDeliveryStatus` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `MilestoneCode` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `RawStatusCode` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `SourceSystem` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `SourceReference` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `SourceVersion` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `OperationalSeatNumber` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `ObservedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
| `ReceivedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S67-S72, S75-S84, S100 |
## 20.1 `OrderDisruptionImpact` — Order evidence child

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `OrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `SourceDisruptionRef` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `SourceSystem` | `string` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `EventType` | `DisruptionEventType` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `RawEventCode` | `string?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `AffectedSegmentIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `AffectedOrderServiceIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `OccurredAt` | `DateTimeOffset?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `ObservedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
| `ResolvedByChangeId` | `long?` | BENCHMARKED-DOMAIN; materialize in owning Stage | S73-S78, S88 |
## Required accepted decision snapshot

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `QuoteId` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `SourceSystem` | `string` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `SourcePricingReference` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `CommercialVersion` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `DocumentVersions` | `IReadOnlyCollection<DocumentVersionReference>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `ApprovedTicketCouponIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `ApprovedEmdCouponIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `ApprovedServiceIds` | `IReadOnlyCollection<long>` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `RefundAmount` | `decimal` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `CurrencyId` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `PenaltyAmount` | `decimal?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `WaiverCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `DispositionCode` | `string?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
| `ValidUntil` | `DateTimeOffset?` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S51, S75, S90 |
## `DocumentVersionReference`

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `DocumentKind` | `AccountableDocumentKind` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S57, S75-S77 |
| `DocumentId` | `long` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S57, S75-S77 |
| `DocumentVersion` | `int` | BENCHMARKED-DOMAIN / OWNER-DECISION as stated | S46-S57, S75-S77 |
## 23.1 `OrderLineage` — Order child

| Field | Type | Evidence class | Minimum scenario coverage |
|---|---|---|---|
| `Id` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S42-S45, S78-S84 |
| `OrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S42-S45, S78-S84 |
| `RelatedOrderId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S42-S45, S78-S84 |
| `RelationType` | `OrderLineageType` | BENCHMARKED-DOMAIN; materialize in owning Stage | S42-S45, S78-S84 |
| `ChangeId` | `long` | BENCHMARKED-DOMAIN; materialize in owning Stage | S42-S45, S78-S84 |
| `CreatedAt` | `DateTimeOffset` | BENCHMARKED-DOMAIN; materialize in owning Stage | S42-S45, S78-S84 |

## 35.1 Traceability acceptance rule

Before a Stage materializes a field, the Stage review must answer all four:

1. Which scenario(s) above require it now?
2. Which owner/source supplies the value?
3. Is the field historical snapshot, current external evidence, or mutable commercial state?
4. If the source is absent, can the Stage complete without the field? If yes, defer it. If no, return `BLOCKED_SOURCE`.

If a proposed field cannot name a scenario and an owner/source, it is not authorized by this Master.

---

# 36. Provider capability evidence register — do not infer from silence

The following register intentionally distinguishes what Ordering currently sends from what an external provider actually guarantees.

| Provider/operation | What is verified in reviewed Ordering source | What is **not** proven by that observation | Stage rule |
|---|---|---|---|
| FlightFlow CreateHold | Ordering persists/sends an idempotency key and validates echoed identity. | Server-side duplicate semantics/read-back guarantees beyond the current adapter are not established here. | Stage 2 closed behavior remains; if provider source contradicts it, raise explicit ADR. |
| FlightFlow ConfirmHold | Current Ordering adapter sends `HoldId` to the confirm endpoint. | This does **not** prove lack of idempotency, resource-level duplicate protection, or confirm read-back. | Stage 3 must inspect actual FlightFlow contract/source and implement the verified branch. |
| FlightFlow automatic expiry | Current Ordering capability currently marks it automatic. | Adapter configuration alone does not prove production provider guarantee/schedule. | Use provider source/event/read-back authority; otherwise expiry remains capability-conditional. |
| FlightFlow INF wire shape | Ordering domain knows lap infant consumes no independent seat. | Whether INF appears in provider `Passengers[]` is provider-specific. | Verify provider contract; otherwise `BLOCKED_SOURCE`. |
| FlightFlow request `Revenue` | Current adapter uses a neutral-looking local value. | Its business meaning is not established by generic airline standards. | Keep adapter-only only with provider authority; never derive from customer price by guess. |
| Future document issuer mutations | Ordering final domain requires recoverable external effects. | Idempotency/read-back behavior differs by issuer/GDS/provider. | Stage 4 must build operation-specific capability matrix before retry policy. |

This table is not a permanent claim about FlightFlow internals. It is an evidence register for the reviewed source state and must be updated when authoritative provider contracts are available.

---

# 37. Benchmark evidence used for this Master

The domain decisions were checked against public, non-proprietary industry behavior and standards, including sources re-verified in September 2026. Vendor sources are used for public business semantics only, never to infer private aggregate/class designs:

## IATA

- **Fulfilment with Orders (ONE Order):** https://www.iata.org/en/programs/airline-distribution/retailing/one-order/ — single integrated Order coordinating fulfilment, delivery and accounting; gradual transition away from legacy PNR/ETKT/EMD artifacts.
- **Modern Airline Retailing / Offers & Orders:** https://www.iata.org/en/programs/airline-distribution/retailing/ — order-based platforms interoperating with Offer, Delivery and financial systems.
- **IATA AIDM Service:** distinct Service booking/delivery facts; delivery status and milestones are separate concerns.
- **IATA AIDM Service Delivery Status Code:** READY TO PROCEED, READY TO DELIVER, IN PROGRESS, DELIVERED, NOT CLAIMED, FAILED/UNABLE TO DELIVER, EXPIRED, SUSPENDED, REMOVED.
- **IATA Passenger Group:** intended passenger quantity/group name, homogeneous itinerary, special booking/fare rules, tour group/sales allotment examples.
- **IATA Charter definitions:** Open Charter sells seats to the public; Closed Charter is whole-aircraft transport for a defined contracting group.
- **Interlining with Offers & Orders:** Order/payment/sales accounting + Delivery/check-in + revenue recognition and explicit internal/settlement values demonstrated in pilot flows.
- **Business Reference Architecture:** disruption capability calls Order Management to apply customer Order changes; Revenue Accounting consumes up-to-date Order/Service delivery facts.
- **IATA EMD:** https://portal.iata.org/faq/s/article/What-is-an-Electronic-Miscellaneous-Document-EMD-1415811054748 — EMD-A vs EMD-S and value coupons for optional services.
- **Revenue Accounting Manual / financial architecture:** interline accounting/currency/settlement remains a specialist accounting capability.

## ATPCO

- **Passenger Tariff / fare construction principles:** journey, Pricing Unit and Fare Component are real fare-construction concepts.
- **Category 5 Advance Reservations and Ticketing:** reservation restrictions and ticketing deadlines are distinct; ticketing can be relative to reservation confirmation and/or departure.
- **Category 31 Voluntary Changes / Category 33 Voluntary Refunds:** automated servicing pricing authority belongs to fare/rule processing, not sale-time flags.
- **Optional Services:** service sub-code, type, group/subgroup, commercial name, EMD type, RFIC and SSR relationships are source-defined semantics.

## Amadeus

- **Nevio:** https://amadeus.com/en/airlines/products/nevio — public capability separation between Order Management, Payment Management and Delivery Management; Order is the single source of truth for order processing/servicing while payments and delivery remain dedicated capabilities.
- **Altéa DCS:** check-in/customer acceptance, seat changes, boarding and disruption handling are Delivery/DCS concerns.
- **Disruption/Reaccommodation:** operational disruption is serviced through passenger recovery/rebooking rather than by rewriting original sale history.

## Sabre

- **SabreMosaic:** https://investors.sabre.com/news-releases/news-release-details/virgin-australia-partners-sabre-pioneer-modern-airline-retailing — end-to-end lifecycle capability families across Offer, Order, Settle and Deliver.
- **SabreSonic:** integrated PSS covers reservations/inventory/departure control while those remain distinct business functions.
- **Revenue Integrity TTL:** unticketed reservation time limits can depend on itinerary/segment/flight/booking-class attributes and exist to release inventory; this supports keeping ticketing/resource time facts explicit instead of one arbitrary fixed global TTL.

These references validate business semantics, not internal vendor aggregate/class design.

---

# 38. Approval statement — v2.0

Upon Owner approval, this Master becomes the project domain authority:

1. It supersedes v1.0/v1.1 Master candidates and conflicting Pack v4.6 decisions.
2. Stage 1 and Stage 2 are CLOSED baselines.
3. Ordering continues linearly without Payment/JetPay as a prerequisite. No mock payment service, dummy success flag, PaymentIntent or PaymentSession is added to current Ordering stages.
4. Stage 3 is `Confirm Reserved Capacity` only. Stage 4 is `Issue`.
5. Future JetPay integration is independent `Stage P`; it may add workflow gates/evidence but cannot redefine Reservation/ETKT/EMD/servicing ownership.
6. Commercial/document refund and exchange deltas remain valid Ordering history even when actual money movement is deferred.
7. Every later Stage is freshly benchmarked before implementation, but aggregate ownership and final field destinations change only through an explicit Owner-approved ADR amendment backed by source/industry evidence.
8. A later review may refine a provider-specific branch after new source evidence appears; it may not retroactively claim that a previously unverified capability was known.
9. Stage prompts must use conditional capability branches (`if verified yes / if verified no / if unknown -> BLOCKED_SOURCE`) whenever this Master does not contain authoritative provider evidence.

**End of Master Domain ADR/PRD v2.0 FINAL — PROJECT AUTHORITY.**
