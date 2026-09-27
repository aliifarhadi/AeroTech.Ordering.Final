# AeroTech Ordering — ORDERING IMPLEMENTATION PACK v4.6 FINAL

## 0. Status

**FINAL DOMAIN & BEHAVIOR AUTHORITY**

Target repository:

```text
aliifarhadi/AeroTech.Ordering.Final
branch: k8s-stg
```

This Pack is authoritative only for:

```text
domain boundaries
business ownership
business identity
business lifecycle
aggregate/entity/value semantics
source-of-truth rules
domain invariants
cross-aggregate business consistency
PSS behavior
slice acceptance behavior
```

It is deliberately **not** authoritative for:

```text
project/folder/namespace placement
EF mapping strategy
table names
repository implementation pattern
DI / Mediator / pipeline
constructor style beyond domain accessibility needs
migration mechanics
SQL/index naming
where an enum class physically lives
framework conventions
```

The existing repository/framework conventions decide those implementation matters.

All previous Ordering domain Packs are superseded by this file for domain shape and behavior.

---

# 1. The rule that prevents a fourth failed implementation

A domain concept may be implemented in a slice only if at least one is true:

1. a real PSS scenario in that slice needs it;
2. a real upstream/downstream contract supplies or consumes it;
3. it is required to enforce a domain invariant of that slice;
4. it is required to preserve historical truth needed by a known later servicing scenario.

Otherwise:

```text
DO NOT IMPLEMENT IT NOW
```

A future concept whose **ownership/boundary is already known** may be documented as a deferred extension, but it must not become production code merely for completeness.

This Pack prefers:

```text
working airline behavior
over
complete-looking skeletons
```

---

# 2. PSS benchmark principles

The model is benchmarked against the public business behavior of modern airline PSS / Order platforms and IATA Offers & Orders.

The benchmark does **not** claim knowledge of private Amadeus or Sabre internal aggregate implementations.

Publicly observable industry facts used here:

- IATA ONE Order is moving airlines toward one customer-centric Order while legacy PNR/ticket/EMD coexistence is phased out over time.
- Sabre publicly describes modern airline retailing as operating through a hybrid PNR-and-Order transition.
- IATA defines an Order Item as an individually priced item containing one or more Services.
- IATA states that, at Order time, a Service is applied to one passenger on one segment.

AeroTech therefore remains:

```text
Order-centric
+
practical for today's PNR/document world
+
capable of moving toward Offers & Orders without a boundary rewrite
```

Public benchmark references:

```text
IATA ONE Order:
https://www.iata.org/en/programs/airline-distribution/retailing/one-order/

IATA AIDM Order Item:
https://airtechzone.iata.org/aidm_model/25.2/EARoot/EA6/EA1/EA2/EA7/EA10550.htm

IATA AIDM Service:
https://airtechzone.iata.org/aidm_model/25.2/EARoot/EA6/EA2/EA2/EA3/EA11503.htm

SabreMosaic hybrid PNR/Order transition:
https://investors.sabre.com/news-releases/news-release-details/virgin-australia-partners-sabre-pioneer-modern-airline-retailing

Amadeus Nevio:
https://amadeus.com/en/airlines/products/nevio
```

---

# 3. Frozen Aggregate boundaries

These boundaries are frozen unless a **real external contract creates a contradiction**.

## 3.1 `Order`

Owns accepted commercial truth:

```text
what the customer bought
who the ordered travellers are
sold itinerary snapshot
commercial items/services
accepted monetary ledger
commercial mutation history
order-side payment coverage evidence
commercial remarks
```

## 3.2 `FulfillmentReservation`

Owns current reservation/provider booking truth:

```text
hold/reservation identity
reservation expiry
per-service reservation outcome
provider references/status
partial/unknown reservation outcome
release/cancel/split reservation lifecycle
```

## 3.3 `ElectronicTicket`

Owns electronic-ticket/document and coupon truth.

## 3.4 `ElectronicMiscDocument`

Owns EMD and EMD-coupon truth.

## 3.5 `DocumentStock`

Owns accountable document-number allocation when Ordering is the issuing owner.

## 3.6 `FulfillmentTask`

Owns execution/retry/reconciliation of Ordering-owned external effects.

It does not own reservation, payment, commercial or document truth.

---

# 4. Explicitly NOT Aggregate roots

```text
Payment
Traveller
OrderItem
OrderService
Pricing
Refund
Exchange
FareConstruction
ProviderInteraction
OrderRemark
PassengerGroup
```

JetPay owns payment transactions.

`ProviderInteraction` belongs to `FulfillmentTask`.

Refund/exchange are servicing operations represented by commercial changes + pricing/document histories, not separate business aggregates.

---

# 5. Ownership matrix — never mix these truths

| Truth | Owner |
|---|---|
| accepted sale/commercial composition | `Order` |
| sold flight snapshot | `OrderSegment` |
| current provider reservation outcome | `FulfillmentReservation` |
| current reservation outcome for one ordered service | `ReservationService` |
| payment transaction / tender lifecycle | JetPay |
| order-side payment coverage evidence | `OrderPaymentCoverage` |
| issued ETKT/coupon truth | `ElectronicTicket` |
| issued EMD/coupon truth | `ElectronicMiscDocument` |
| document-number stock | `DocumentStock` |
| retry / timeout / unknown external effect | `FulfillmentTask` |
| check-in / boarded / flown / no-show | DCS — source contract not yet frozen |
| automated refund/exchange fare authority | AirPrice servicing contract — not current Offer snapshot |

A schedule change can therefore legitimately produce:

```text
OrderSegment          = originally sold flight
ReservationService    = currently advised/booked flight
TicketCoupon          = currently issued document image
```

Those facts must not silently overwrite each other.

---

# 6. PNR decision — Owner decision is frozen

AeroTech's current single-carrier model uses:

```text
Order.RecordLocator
```

as the host PNR/business locator.

It is:

```text
null after sale
generated by Ordering during Reserve
set when the Reserve flow obtains at least one confirmed reservation outcome
stable afterwards
```

It is distinct from:

```text
OrderReference
FlightFlow HoldId
FlightFlow Reference/correlation
ticket number
EMD number
payment intent
```

Current scope does not invent external GDS/interline locator collections.

If a real future interline/GDS contract introduces external booking references, their ownership is added then without moving the host RecordLocator away from Order.

---

# 7. Order model — final business shape

## 7.1 Order Item / Service grain

The core relationship is:

```text
Order
  -> OrderItem
       -> one or more OrderService occurrences
```

`OrderItem` is the priced commercial grouping.

`OrderService` is the passenger-delivery occurrence.

For air at Order time:

```text
one Service = one traveller × one segment
```

## 7.2 Stage-1 air grouping

Current AeroTech Offer source does not provide a trustworthy cross-bound OfferItem identity.

Therefore Stage 1 uses:

```text
one AirFare OrderItem per traveller × bound
    -> one OrderAirTransportService per segment of that bound
```

Do not create one AirFare Item per segment.

Do not reconstruct a cross-bound OfferItem from `CoveredBoundOfferIds`.

If the provider later exposes a trustworthy OfferItem identity, a later slice may use it without changing the Order aggregate boundary.

---

# 8. Service model

## 8.1 `OrderService`

The base commercial occurrence contains only facts common to every ordered service:

```text
OrderItemId
TravellerId
ServiceType
CommercialStatus
CreatedByChangeId
CreatedAt
```

Later servicing slices add lineage fields when first required:

```text
ClosedByChangeId?
PredecessorOrderServiceId?
```

Do **not** persist generic:

```text
reservation status
provider retry status
ticket status
DCS delivery status
generic service metadata JSON
generic TargetType/TargetId relationships
```

Those truths have stronger owners.

## 8.2 `OrderAirTransportService`

Stage 1 materializes it because the actual Offer contains unique air facts.

It carries:

```text
SegmentId
FlightCapacityId
AirFareId?
BookingClass?
FareBasis?
FareFamily?
FareType?
CheckedBaggageAllowance?
CabinBaggageAllowance?
IsRefundable
IsChangeable
IsUpgradable
```

Important:

```text
IsRefundable / IsChangeable / IsUpgradable
```

are accepted-sale snapshots only.

They are **never sufficient authority** for refund/change/exchange servicing.

Automated servicing uses the real AirPrice servicing/quote contract.

## 8.3 `OrderSeatService`

Stage 1 creates a Seat Service only when the create request contains a non-empty selected seat.

Current request grain is:

```text
BoundId + TravellerIndex + SeatNumber
```

Therefore:

- no Seat Service when seat number is absent;
- the Seat Service belongs to the same AirFare OrderItem as its associated air service;
- the current bound-level request is expanded to the air-service segments in that bound according to existing request semantics;
- no independent paid-seat OrderItem/PricingLine is invented in Stage 1;
- paid-seat commercial behavior belongs to the ancillary slice.

## 8.4 Future ancillary service types

The aggregate boundary is frozen, but unused product structures are not implemented early.

Create a product-specific Entity only when its slice has a real contract and unique invariants.

Deferred examples:

```text
Baggage
Meal
Lounge/CIP
Hotel
Ground transport
other ancillary
```

No speculative fields are added now.

---

# 9. Stage-1 Offer source truth

Authoritative source:

```text
src/AeroTech.Ordering.Providers/Offer/Wire/FlightOfferDetailResponse.cs
src/AeroTech.Ordering.Providers/Offer/Services/OfferResponseMapper.cs
src/AeroTech.Ordering.Domain/Providers/Offer/OfferDetail.cs
```

## 9.1 Two different source grains must survive

### Fare-component / bound grain

These may be propagated to air services belonging to that accepted bound/fare component:

```text
AirFareId
BookingClass
FareBasis
FareFamily
FareType
```

### Coupon grain — traveller × flight

These must come from the exact matching Offer ticket coupon:

```text
IsRefundable
IsChangeable
IsUpgradable
CheckedBaggageAllowance
CabinBaggageAllowance
```

Do not use the first coupon in a bound as the source for all segments.

The Offer ACL must preserve traveller+flight coupon facts.

This is required for:

```text
connections
different coupon terms by segment
codeshare/segment differences
later partial servicing
```

## 9.2 Flight number

The wire allows missing FlightNumber.

Missing remains:

```text
null
```

Do not normalize it to an empty string.

## 9.3 Stop facts

Preserve the accepted stop snapshot together:

```text
StopType
StopDurationMinutes
PassengersCanBoardOrLeave
```

If source stop is absent, all three are absent.

Use the Offer/AirPrice stop semantics, not the unrelated FlightFlow stop vocabulary.

## 9.4 FareType

The wire contains FareType.

If the current domain Offer mapper drops it, Stage 1 extends the ACL to preserve it.

---

# 10. Traveller reconciliation — mandatory before Order creation

Traveller identity exists in both create input and accepted Offer.

Before committing an Order:

1. request traveller index set must equal Offer traveller index set;
2. each index must resolve exactly once on each side;
3. Offer passenger code must map to the approved Ordering passenger-type semantics;
4. unknown passenger code is rejected — never defaulted;
5. request passenger type must match the accepted Offer passenger type;
6. `SourceTravellerRef` is copied only after successful reconciliation.

Unknown non-empty baggage weight units are also rejected rather than silently defaulted.

---

# 11. Traveller identity and PII

`OrderTraveller` is stable commercial identity.

Mutable PII is represented by `TravellerProfileRevision`.

Initial sale creates exactly one current profile revision for every named traveller.

Name invariant:

```text
GivenName is required
Surname is required unless NoSurname == true
NoSurname == true permits an absent surname
```

Later traveller/name correction:

```text
does not replace Traveller identity
creates a new TravellerProfileRevision
creates OrderChange type TravellerCorrection
does not rewrite already issued document snapshots
```

ADT/CHD/INF parent linkage remains on the Traveller identity.

---

# 12. Contact model

Stage 1 creates Order-level contact data with:

```text
OrderContact
  -> 1..N OrderContactPoint
```

The current create request's contact is treated as the Primary contact.

Do not reduce reservation contact information to one fixed phone and one fixed email column.

Later contact correction appends/replaces the commercial occurrence according to the servicing slice; Stage 1 only needs initial contact + remark behavior.

---

# 13. Sold itinerary

## `OrderJourney`

Stage-1 facts:

```text
Sequence
BoundId
OriginAirportId
DestinationAirportId
```

Do not synthesize Outbound/Inbound direction from sequence.

That breaks general multi-city/open-jaw.

## `OrderSegment`

Stage-1 sold snapshot:

```text
Sequence
FlightId
FlightVersion
FlightNumber?
OriginAirportId
OriginAirportTerminalId?
DestinationAirportId
DestinationAirportTerminalId?
OperatingAirlineId
MarketingAirlineId
SoldDeparture
SoldArrival
Duration
AircraftId?
```

Do not place:

```text
FlightCapacityId
AirFareId
BookingClass
RBD
```

on shared Segment.

Those are service/fare facts.

## `OrderSegmentLeg`

Preserve:

```text
Sequence
LegId
Origin/Destination
terminal facts when present
departure/arrival
optional stop snapshot
```

---

# 14. Commercial change model

`OrderChange` is the accepted commercial mutation record.

It is not a separate aggregate.

Business types:

```text
Create
AddProduct
Cancel
ChangeService
TravellerCorrection
Split
RemoveService
Refund
ContactCorrection
```

For `ChangeService`, cause is separate:

```text
Voluntary
Involuntary
ScheduleChange
```

Ticket exchange/revalidation is document behavior and is not used as a generic commercial mutation type.

## Commercial version

Initial sale:

```text
Order.CommercialVersion = 1
Create OrderChange.CommercialVersion = 1
```

Every later accepted commercial OrderChange increments exactly once.

Adding/revising a remark does not increment commercial version.

---

# 15. Sales / servicing actor context

Original sale captures the real caller business context supplied by `ICallerContext`:

```text
Channel
ContextType
PrincipalType
ActorId
AirlineUserId?
TravelAgencyId?
TravelAgencyUserId?
IndividualId?
PartnerApiAccessProfileId?
```

Do not invent `AirlineOfficeId` when the actual target contract does not provide one.

Every later `OrderChange` captures the servicing caller context separately.

This preserves:

```text
original sale context
vs
later servicing actor/channel
```

for S46 and agency/audit scenarios.

---

# 16. Pricing model

Pricing is an append-only accepted commercial ledger.

## `PricingLine`

Carries:

```text
CreatedByChangeId
Reason
Scope
Category
SubCategory
Direction
Treatment
Code?
Description?
Reference?
Amount
CurrencyId
EquivalentAmount
EquivalentCurrencyId
ExchangeRateSnapshot?
Refundability?
CreatedAt
```

Later servicing adds predecessor/reversal lineage when first required.

## Treatment

```text
CustomerPrice
SettlementOnly
```

Commission is:

```text
SettlementOnly
```

and does not increase customer total.

Commission recipient/settlement attribution remains:

```text
BLOCKED_SOURCE
```

until the real commercial/agency contract proves it.

## Refundability

Line-level refundability is populated only where the source actually proves it.

Do not turn a default `false` on tax/fee/surcharge into authoritative refund policy.

Final refund authority comes from the servicing contract.

---

# 17. Pricing allocation

`PricingAllocation` attributes accepted money to business occurrences.

Allowed dimensions:

```text
OrderItem
OrderService
Journey
Segment
Traveller
```

One allocation may contain multiple compatible dimensions simultaneously.

It represents one monetary attribution, not one amount per dimension.

Do not create generic:

```text
TargetType
TargetId
```

and do not invent allocation that the source cannot justify.

---

# 18. Monetary invariants

Using the existing PricingLine direction convention:

```text
Credit = positive
Debit  = negative
```

define signed value as:

```text
signed(x) = +x for Credit
            -x for Debit
```

## Customer total

```text
Order.CustomerTotal =
    Σ signed(PricingLine.EquivalentAmount)
    where Treatment == CustomerPrice
```

Every CustomerPrice line contributing to CustomerTotal must use:

```text
EquivalentCurrencyId == Order.CurrencyId
```

SettlementOnly never contributes.

## Allocation reconciliation

If a PricingLine has allocations:

```text
Σ allocation.Amount == line.Amount
Σ allocation.EquivalentAmount == line.EquivalentAmount
```

at accepted money precision.

## Item total

For Stage-1 AirFare Items:

```text
OrderItem.AcceptedTotal =
    Σ signed(allocation.EquivalentAmount)
    for CustomerPrice lines allocated to that Item
```

Order-scoped service fees that have no Item allocation do not enter Item total.

---

# 19. Historical FX

`ExchangeRateSnapshot` stores the accepted historical rate used for the sale:

```text
FromCurrencyId
ToCurrencyId
Rate
DecimalPlaces
PeriodId?
```

It is immutable.

Do not reprice historical accepted money with the current exchange rate during later servicing.

Fields not carried/proven by the actual Offer domain contract are not invented merely because an upstream wire once contained them.

---

# 20. Remark behavior

`OrderRemark` is an Entity inside Order.

Stage 1 supports:

```text
add
revise by appending a successor remark
```

Revision never overwrites old text.

Stage 1 rejects:

```text
Scope = Document
```

because Ticket/EMD targeting does not exist yet.

SSR/OSI are not aliases for OrderRemark.

Their future ownership is the reservation domain and their implementation remains source-gated.

---

# 21. Stage-1 materialized Domain contract

Only these domain types are implemented in Stage 1:

```text
Order
OrderItem
OrderService                    // abstract business base
OrderAirTransportService
OrderSeatService
OrderTraveller
TravellerProfileRevision
OrderTravellerDocument
OrderContact
OrderContactPoint
OrderJourney
OrderSegment
OrderSegmentLeg
OrderChange
PricingLine
PricingAllocation
OrderRemark
```

Stage-1 Value Objects:

```text
SalesContext
BaggageAllowance
ExchangeRateSnapshot
```

Do not implement in Stage 1 merely because ownership is already known:

```text
PassengerGroup
FulfillmentReservation
ReservationGroupSpace
FulfillmentTask
OrderPaymentCoverage
ElectronicTicket
ElectronicMiscDocument
DocumentStock
baggage/meal/lounge/hotel/ground product entities
split/change predecessor lineage not used by Initial Sale
DCS delivery observations
SSR/OSI
FareConstruction
```

They enter when their behavior slice begins.

---

# 22. Stage-1 field semantics

This section is domain-level. It intentionally omits SQL, indexes and physical mapping.

## `Order`

```text
OrderReference: Guid                      required
CustomerId: long                         required
SalesContext: SalesContext                required
CurrencyId: int                          required
SourceOfferId: string                     required
LastTicketingDate: DateTimeOffset?        optional
Status: OrderStatus                       required; Created after Initial Sale
CommercialVersion: int                    required; 1 after Initial Sale
CustomerTotal: decimal                    required
CreatedAt: DateTimeOffset                 required

Items
Services
Travellers
Contacts
Journeys
Segments
PricingLines
Changes
Remarks
```

`RecordLocator` is added in the Reserve slice because that is when it first has behavior.

## `OrderItem`

```text
OrderId
Kind: ProductType
AcceptedTotal
CommercialStatus
CreatedByChangeId
CreatedAt
```

Initial AirFare item:

```text
Kind = AirFare
CommercialStatus = Active
```

## `OrderService`

```text
OrderId
OrderItemId
TravellerId
ServiceType
CommercialStatus
CreatedByChangeId
CreatedAt
```

Initial services are Active.

No persisted FulfillmentProfile is required in Stage 1.

Reservation/document/payment requirements are domain behavior of the service slice, not speculative sale-time state.

## `OrderAirTransportService`

```text
SegmentId
FlightCapacityId
AirFareId?
BookingClass?
FareBasis?
FareFamily?
FareType?
CheckedBaggageAllowance?
CabinBaggageAllowance?
IsRefundable
IsChangeable
IsUpgradable
```

## `OrderSeatService`

```text
SegmentId
AssociatedAirServiceId
SeatNumber
```

`SeatNumber` is required because the service is not created when selection is absent.

## `OrderTraveller`

```text
OrderId
Index
SourceTravellerRef?
PassengerType
AgeRange
InfantParentTravellerId?
CurrentProfileRevisionId
Status
Documents
ProfileRevisions
```

## `TravellerProfileRevision`

```text
TravellerId
GivenName
Surname?
NoSurname
DateOfBirth?
Gender?
NationalityId?
CountryOfResidenceId?
CreatedByChangeId
SupersededByChangeId?
CreatedAt
```

`SupersededByChangeId` remains unused until traveller-correction behavior begins but is part of the history mechanism; if the team wants zero dormant fields, it may be introduced with that slice without changing ownership.

## `OrderTravellerDocument`

```text
OrderTravellerId
Type
Number
ExpiryDate?
IssuanceCountryId
Holder
```

## `OrderContact`

```text
OrderId
Sequence
Role
ContactName?
ContactPoints
```

Stage 1 uses:

```text
Role = Primary
```

## `OrderContactPoint`

```text
OrderContactId
Type
Value
CountryCode?
IsPrimary
```

## `OrderJourney`

```text
OrderId
Sequence
BoundId
OriginAirportId
DestinationAirportId
```

## `OrderSegment`

```text
OrderId
OrderJourneyId
Sequence
FlightId
FlightVersion
FlightNumber?
OriginAirportId
OriginAirportTerminalId?
DestinationAirportId
DestinationAirportTerminalId?
OperatingAirlineId
MarketingAirlineId
SoldDeparture
SoldArrival
Duration
AircraftId?
Legs
```

## `OrderSegmentLeg`

```text
OrderSegmentId
Sequence
LegId
OriginAirportId
OriginAirportTerminalId?
DestinationAirportId
DestinationAirportTerminalId?
DepartureDateTime
ArrivalDateTime
StopType?
StopDurationMinutes?
StopPassengersCanBoardOrLeave?
```

Stop fields are all absent or all present according to the source stop object.

## `OrderChange`

```text
OrderId
ChangeType
CommercialVersion
ActorContext: SalesContext
SourceReference?
CommittedAt
```

Initial sale:

```text
ChangeType = Create
SourceReference = accepted OfferId
CommercialVersion = 1
```

Later servicing slices may add Cause/Reason when first used.

## `PricingLine`

```text
OrderId
CreatedByChangeId
Reason
Scope
Category
SubCategory
Direction
Treatment
Code?
Description?
Reference?
Amount
CurrencyId
EquivalentAmount
EquivalentCurrencyId
ExchangeRateSnapshot?
Refundability?
CreatedAt
Allocations
```

## `PricingAllocation`

```text
PricingLineId
OrderItemId?
OrderServiceId?
OrderJourneyId?
OrderSegmentId?
TravellerId?
Amount
CurrencyId
EquivalentAmount
EquivalentCurrencyId
```

No duplicate FX snapshot is necessary on Allocation; the authoritative accepted FX snapshot belongs to the PricingLine.

## `OrderRemark`

```text
OrderId
Type
Visibility
Scope
TravellerId?
SegmentId?
OrderItemId?
OrderServiceId?
Text
CategoryCode?
IsPrintedOnItinerary
IsPrintedOnInvoice
Status
SupersedesRemarkId?
CreatedBy
CreatedAt
```

Deletion-specific fields enter when deletion behavior is implemented.

---

# 23. Stage-1 behavior oracle — Create Order from Offer

Stage 1 is complete only when this actual flow works.

## 23.1 Read and validate

1. load the actual Offer from the current Offer provider;
2. reconcile request travellers with Offer travellers;
3. reject mismatched/unknown passenger type;
4. reject unknown non-empty baggage unit;
5. validate ADT/CHD/INF parent rules;
6. do not mutate or reprice the Offer.

## 23.2 Create accepted commercial snapshot

Create:

```text
Order
initial OrderChange
Travellers + initial profile revisions + documents
Contact + contact points
Journeys
Segments
Legs
AirFare Items
Air Services
optional Seat Services
PricingLines
PricingAllocations
Remarks only when explicitly requested
```

## 23.3 Air Item/Service construction

For every:

```text
traveller × bound
```

create one AirFare Item.

For every segment of that bound create one AirTransport Service under that Item.

Map:

```text
fare-component facts from bound/fare grain
coupon flags/baggage from matching traveller × flight coupon
```

## 23.4 Seat construction

For every non-empty seat selection:

- resolve traveller and bound;
- resolve the air services of that traveller+bound;
- create the requested Seat Service occurrence(s) according to current bound-level request semantics;
- associate each Seat Service with its Air Service;
- keep them under the same AirFare Item;
- create no independent price unless the source actually supplies one.

## 23.5 Pricing

Create accepted:

```text
fare
tax
fee
surcharge
discount/credit when supplied
order-level service fee
settlement-only commission when supplied
```

Pricing is not recomputed by Ordering.

Order-level fee:

```text
Scope = Order
```

and does not create a fake Item/Service.

Commission:

```text
Treatment = SettlementOnly
```

and does not enter CustomerTotal.

## 23.6 Finalize

```text
Order.Status = Created
Order.CommercialVersion = 1
initial OrderChange.CommercialVersion = 1
CustomerTotal reconciles
OrderItem totals reconcile
Pricing allocations reconcile
accepted snapshot persists
OrderCreated integration event is emitted through the repository's existing mechanism
```

No reservation/payment/ticketing is executed in Stage 1.

---

# 24. Stage-1 acceptance behavior

The implementation is not considered complete merely because it compiles.

The following must pass:

### Core booking topology

```text
1 passenger direct
multiple passengers
round trip
connection
multi-city / open-jaw
codeshare marketing != operating
missing FlightNumber remains null
technical/intermediate stop snapshot preserved
```

### Traveller

```text
ADT
CHD
INF linked to valid parent
request/Offer passenger mismatch rejected
unknown passenger code rejected
NoSurname accepted correctly
invalid missing surname rejected
accepted source traveller references preserved
```

### Offer truth

```text
source Offer changed after creation -> stored Order unchanged
FareType survives ACL
connection coupons with different flags/baggage remain different per Service
unknown baggage unit rejected
```

### Commercial grain

```text
one AirFare Item per traveller × bound
one Air Service per traveller × segment
connection therefore has one Item with multiple Services for that traveller/bound
selected seat creates Seat Service
no seat selection creates no Seat Service
```

### Pricing

```text
fare/tax/fee/surcharge
order-scoped fee without fake Item
commission excluded from CustomerTotal
historical FX snapshot includes DecimalPlaces
CustomerTotal exact reconciliation
allocation exact reconciliation
Item AcceptedTotal exact reconciliation
```

### Contact / remark

```text
contact with multiple contact points
AddRemark
revise remark without overwriting old text
Document-scoped remark rejected in Stage 1
remark revision does not increment CommercialVersion
```

### Persistence/read usability

```text
created Order can be read back with the same commercial semantics
no later provider call is needed to reconstruct the accepted sale
```

---

# 25. Stage 2 — Reserve / PNR / recovery

Stage 2 begins only after Stage 1 works.

Materialize only now:

```text
Order.RecordLocator
FulfillmentReservation
ReservationService
FulfillmentTask
FulfillmentTaskTarget
FulfillmentTaskAttempt
ProviderInteraction
```

`ReservationGroupSpace` is deferred until group behavior because it has no Stage-2 source scenario today; its ownership is already frozen under `FulfillmentReservation`.

## Reserve behavior

1. use current Order air services as reservation targets;
2. call FlightFlow idempotently;
3. preserve HoldId/Reference/ExpiresAt/provider target results;
4. persist per-service reservation outcome;
5. successful/partial/unknown outcomes are never collapsed into one boolean;
6. when at least one required reservation is confirmed, generate/set the Order host RecordLocator according to the Owner-approved rule;
7. retry/reconcile uses original external-effect identity;
8. retry must not create a second logical reservation;
9. `LastTicketingDate` remains Order sale/pricing fact;
10. reservation expiry remains reservation fact.

`OrderSegment` is never overwritten by provider advice.

---

# 26. Stage 3 — Payment coverage

JetPay remains payment transaction authority.

Add only the Order-side evidence needed for order gating:

```text
OrderPaymentCoverage
```

based on actual:

```text
PaymentCompleted
PaymentIntentGuaranteed
PaymentPaidUnapplied
PaymentIntentStatus
```

Support:

```text
multiple payment intents
captured coverage
guaranteed coverage
guarantee expiry
duplicate-event idempotency
paid-unapplied evidence
```

No local Payment aggregate.

No tender/provider transaction duplication.

---

# 27. Stage 4 — Ticket issue

Add:

```text
DocumentStock
DocumentStockAllocation
ElectronicTicket
TicketCoupon
TicketPriceLink
```

Issue only after the real reservation/payment gates are met.

Ticket coupon always retains original issuance references.

Unknown provider result is recovered through FulfillmentTask; it is not treated as failure and blindly retried.

Do not implement refund/exchange/revalidation histories until those slices.

---

# 28. Stage 5 — Cancel / void / release

Support separately:

```text
pre-ticket cancellation
reservation release/cancel
ticket void when applicable
pricing reversal
unknown external outcome recovery
```

Final Order cancellation occurs only when required external effects are known or the business explicitly enters an unconfirmed/manual state.

Cancellation is not the same as refund.

---

# 29. Stage 6 — Ancillary + EMD

Introduce product-specific service entities only when the slice's real contract needs unique fields/invariants.

Required airline behaviors include:

```text
post-sale baggage
paid seat
included service with no independent price
standalone ancillary
through baggage represented by multiple per-segment Services under one Item
EMD-A
EMD-S
fee/residual EMD without fake Service
```

Do not prebuild hotel/lounge/ground-transfer schemas without their contracts.

---

# 30. Stage 7 — Split

Now add commercial lineage fields first used by split/change:

```text
RootOrderId / ParentOrderId
Predecessor Item/Service/Traveller/Journey/Segment IDs
ClosedByChangeId fields
```

Split rules:

```text
source historical occurrences remain
child gets successor occurrences
old TicketCoupon issuance links never re-point
PNR behavior follows real FlightFlow split contract
shared/unallocated monetary value is never fabricated
```

If allocation is insufficient for a safe split:

```text
BLOCKED / domain exception
```

rather than guessed money.

---

# 31. Stage 8 — Refund

Before implementation, read the actual AirPrice servicing/refund contract.

If it does not supply the required authoritative calculation/context:

```text
BLOCKED_SOURCE
```

Do not resurrect speculative FareConstruction simply because refund needs more context.

Add only the document/pricing history proven necessary.

Support:

```text
full refund
partial refund
after partial travel
ancillary refund
```

at authoritative scope.

---

# 32. Stage 9 — Change / exchange / reissue / revalidation

Use:

```text
OrderChange(ChangeService + Cause)
successor commercial occurrences
append-only pricing deltas
TicketExchange / TicketExchangeCoupon
TicketRevalidation
EMD association history when required
```

Keep these distinct:

```text
commercial change
ticket exchange/reissue
ticket revalidation
```

They are not synonyms.

---

# 33. Stage 10 — disruption / group / broader PSS

Implement only against real contracts:

```text
schedule change
reaccommodation
seat/baggage impact
group/name-later
group reservation space
SSR/OSI
DCS delivery/consumption
hotel/ground supplier-specific behavior
interline/GDS booking references
```

Ownership is already frozen:

```text
PassengerGroup        -> Order
group reservation     -> FulfillmentReservation
SSR/OSI               -> reservation domain
DCS observations      -> external DCS truth
```

No aggregate-boundary redesign should be required.

---

# 34. Cross-aggregate consistency

All local business aggregates belong to the same Ordering bounded context.

Rule:

> Never keep an external provider call open inside a local database transaction.

Pattern:

```text
persist/identify intended external effect
-> execute provider effect idempotently
-> once outcome is known, commit the necessary local business truths
-> publish integration events through the existing outbox mechanism
```

Unknown outcome:

```text
FulfillmentTask = Unknown/recovery state
do not fabricate commercial/reservation/document success
reconcile original effect before retrying
```

A local business operation may update multiple local aggregates atomically when that is the correct recording of one known provider outcome.

Do not invent internal sagas merely because two local aggregate roots participate.

---

# 35. OrderStatus policy

Keep the existing coarse Order workflow projection because it is operationally useful.

It is not the source of truth for lower-grain state.

Examples:

```text
Created
Confirmed
Paying
Paid
Ticketing
Ticketed
ReservationUnconfirmed
PaymentUnconfirmed
TicketingUnconfirmed
Cancelled
Refunded
Expired
```

Detailed facts remain with their owners.

---

# 36. Deferred source gates

These remain intentionally unresolved until their real source contracts are available.

## Commission recipient

```text
BLOCKED_SOURCE
```

The price ledger can record SettlementOnly commission now, but do not invent the receiving agency/party identity.

## Refund/exchange fare context

```text
BLOCKED_SOURCE until AirPrice servicing contract
```

## DCS consumption

```text
BLOCKED_SOURCE until DCS contract
```

Do not invent flown/boarded/no-show state on OrderService.

## Hotel/ground/third-party product fields

```text
BLOCKED_SOURCE until supplier/product contract
```

## SSR/OSI

```text
BLOCKED_SOURCE until reservation contract/use cases are available
```

---

# 37. What Coding Agent may decide

Coding Agent may choose implementation details already governed by the target repository:

```text
folder/file placement
namespace according to repo rules
EF mapping strategy
table names
indexes needed to implement stated uniqueness/invariants
repository method shape
DI registrations
migration mechanics
test fixture structure
```

Coding Agent may **not** decide:

```text
new aggregate
new entity
new business field
new business status axis
new monetary allocation rule
new PNR ownership rule
new refund/change authority
new service grain
new history semantics
```

If one of those is genuinely missing:

```text
BLOCKED
```

with the exact scenario and source gap.

---

# 38. Stage-1 Agent instruction

Use this instruction for the first implementation session:

```text
TARGET:
aliifarhadi/AeroTech.Ordering.Final / k8s-stg

AUTHORITY:
ORDERING-IMPLEMENTATION-PACK-v4.6-FINAL.md for domain shape and behavior.
Repository CLAUDE/framework rules remain authority for implementation architecture.

DO NOT:
- audit or redesign the framework;
- create architecture/report documents;
- implement future aggregates/products merely because the Pack names their ownership;
- invent fields/statuses/contracts;
- copy V1/V2/V3 domain shapes wholesale.

IMPLEMENT:
Stage 1 Initial Sale only, from Domain outward, until CreateOrderFromOffer works end-to-end.

The Stage is not complete until the acceptance behavior in section 24 passes.

Important:
- one AirFare Item per traveller x bound;
- one Air Service per traveller x segment;
- preserve coupon facts at traveller x flight grain;
- reconcile request travellers with Offer travellers;
- keep accepted pricing/FX immutable;
- no reservation/payment/ticket behavior in this Stage.

If a missing fact requires a domain decision rather than an implementation decision:
report BLOCKED with the exact scenario and source contract gap.
```

---

# 39. Final principle

This Pack is complete when it lets the team build a useful airline capability without guessing business semantics.

It is **not** complete by having the most fields.

The implementation sequence is deliberately:

```text
working sale
-> working reservation
-> working payment coverage
-> working issue
-> working cancel
-> working ancillary/document servicing
-> split/refund/change/disruption
```

Every merged slice must leave behind a usable PSS capability.

No large dead skeleton is an acceptable deliverable.
