# 08 — Ordering→Ancillary OrderContext and actual repeat shopping

## 1. Do NOT recreate an existing local route

Owner's 2026-10-28 review reports **unpublished local Ordering `6a7bb71` already implements**:

`GET /Service/v1/Orders/{orderId:long}/AncillaryContext`

in `src/AeroTech.Ordering.RestApi/V1/OrderAggregate/Controllers/ServiceController.cs` with acceptance tests `tests/AeroTech.Ordering.Application.AcceptanceTests/AncillaryContext/OrderAncillaryContextTests.cs`. **Check and extend this exact implementation**. If working tree lacks it, **STOP/BASELINE_NOT_AVAILABLE**, do not create parallel `/Bookings` Service controller or a second redundant DTO. The previously bundled stale Master reference is forbidden.

## 2. Required context header and source-of-truth

- `orderId` (long), `commercialVersion` (int), `ticketedAt?` (DateTimeOffset? from actual documents), `currencyId`, `customerId`, `sales.travelAgencyId?` (existing fact; **not** a new priced-for guard), `travellers[]`, `journeys[].segments[].legs[]`, `airServices[]`, `ancillaryServices[]`.
- `acceptsChanges`: **only if** current Order is Created, no historical Reservation/Task/Documents, source can honestly price Add, and applicable ticketing deadline valid. Return `false` otherwise. Source-shopping may later support broader states but must not present this field as permission to mutate them today.
- Preserve exact accepted itinerary/flight versions/cabin/fare/coupon facts, not renewed live AirOffer/FlightFlow data. `FareType` should use local `AirFareType?` evidence (do not revert to old stale string). 
- Order SourceOfferId is historical traceability. **Ancillary order shopping must NOT fetch source AirOffer again**; it constructs context from committed Order.

## 3. `ancillaryServices[]` — minimum lossless existing-service summary

Current Ancillary input class has `OrderServiceId:long`, `TravellerId:long`, `ServiceDefinitionRef`, `CoveredAirServiceIds:long[]`, `Quantity:int`, `CommercialState?`. Ancillary at `190aa74` refuses **any nonempty** list. We need a small compatible addition to the actual wire, without copying the whole accepted price:

```json
{
  "orderServiceId": 3211,
  "travellerId": null,
  "serviceDefinitionRef": "WIFI_PACKAGE",
  "variantCode": "A24",
  "profile": "Connectivity",
  "selectionScope": "PerOrder",
  "coverageScope": "Order",
  "coveredAirServiceIds": [],
  "quantity": 1,
  "commercialState": "Active"
}
```

This example is a possible **PerOrder** item, not proof that current live catalog offers PerOrder Wi-Fi. Actual `Source.SelectionScope` controls it. For passenger items `travellerId` is a real OrderTraveller.Id. For A07/A08 seat, source may need `orderSegmentId`, `flightId`, `seatNumber` and `associatedAirServiceId` for real conflict/seat-map re-shopping; define just the precise fields used by the existing conflict algorithm. `CoveredAirServiceIds` must reference current genuine Air OrderService.Id; no provider physical Hold identifiers and no invented `Ref` from a variant.

**Changes on both teams:**
1. Ordering extends existing context read/query DTO and projections, including `ancillaryServices` when commercially accepted; persists no second copy of upstream catalog.
2. Ancillary changes `OrderingAncillaryContext.OrderingAncillaryService.TravellerId` to `long?`, removes `Require(source.AncillaryServices.Count==0)`, maps real active services into `OrderCommercialContext.Services`/`ExistingAncillaryServiceFacts`, validating traveler/coverage IDs and ref consistency. `CommercialState` maps accurately — **Cancelled is NOT Refunded**.
3. Source `OrderShoppingContextMapper` currently maps order Services into `ExistingAncillaryServiceFacts`; test that repeated purchase offers depend on actual active services. With `Services=[]` this is currently wrong.

## 4. Mapping details and backwards compatibility

- Legacy Order (air only): `ancillaryServices:[]` and **exact same** response/order shopping.
- First Add: following `GET AncillaryContext` returns all active new services and exact source refs, qty, scope.
- Second list/price: succeeds with nonempty services and enforces source's duplicate/quantity eligibility; never 503 on nonempty array.
- PerOrder: `travellerId:null`; no arbitrary default first traveller; source accepts nullable type.
- Portion: multi-flight coverage is preserved; passing order AirServiceIds source must correctly map back to FlightRefs/PortionRef using real `OrderCommercialContext` mapping.
- Historical inactive services: retained in Order database; shipping active-only summary to shopping is okay **if** pricing/eligibility engine only needs current active purchases. Do not call a cancelled service Refunded or remove its accepted history.
- `acceptsChanges` true/false is advisory for shopping, not the permission system. Add rechecks under lock before committing.

## 5. Necessary cross-service cases

OC01 new Created Air-only Order context; OC02 two travellers/three flights/two bounds; OC03 infant parent; OC04 nonempty baggage A01 purchased quantity 2; OC05 two-flight bound A02 coverage; OC06 A07/A08 seat conflicts; OC07 Order-scoped service with null traveller; OC08 active+cancelled service history; OC09 ticketed/coupon facts (read only); OC10 second Add actually price/resolve succeeds; OC11 stale version rejects Add; OC12 no external Offer reload; OC13 `acceptsChanges=false` after reservation starts; OC14 no phantom cabin/FareType conversion; OC15 full JSON long IDs and DateTimeOffset; OC16 authorization unchanged.
