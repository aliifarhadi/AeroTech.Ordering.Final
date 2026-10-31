# 06 — A07/A08 commercial SeatMap and actual POS provenance

## 1. The root flaw in the legacy path

Current `Order.Create.BuildSeatServices` uses `(TravellerIndex, BoundId, SeatNumber)` and creates the **same seat on every flight** in the Bound; it attaches each new Seat Service to an existing AirFare `OrderItem` and does not accept independent seat price. Actual Ancillary `SeatMapSelectionId.Encode` = base64url(`travellerRef|flightId|pricingId|seatNumber`); current `SeatMaps/Resolve` loads **active pricing definition/rates** but not a unique selected, trusted final price/expiry for a given source context. **Neither** constitutes a commercial charge authorization.

## 2. Canonical Seat commercial invariants

- A07/A08 -> exactly **one** traveller × actual flight ID × seat number × verified rate/POS quote. Same traveller flight cannot have two active seats. Two travellers cannot own the same physical seat for that flight inside one Order.
- Same `OrderSeatService` must be both the commercial purchase and the physical seat assignment reference. DO NOT create `OrderAncillaryService` shadow or treat A09 ExtraSeat as a normal seat.
- New paid/free accepted seat has `OrderItem.Kind=Seat`, independent `AcceptedTotal`, correct Base/Tax/Fee, `SourceSeatSelectionId`, Variant A07/A08, Definition/Provision/Pricing, rate version, `PointOfSaleId` from **the selected authoritative source quote**.
- `FulfillmentProviderKey=FlightFlow` remains for seat physical reserve. Ancillary definition/provision and commercial hold semantics stay independent; no unverified assumption that its Seat Hold reduced aircraft occupancy.
- Superseded by decision D15 (2026-10-31): legacy `SeatSelections` is removed from Create. Original text: Legacy `SeatSelections` remains supported **only** in the legacy Create route, with historic semantics untouched for compatibility, while new source SeatMap uses the per-flight path. A mixed nonempty old/new seat input is a typed error.

## 3. Required **AC-03** Ancillary read-only selected quote contract

**Superseded by decision D14 (2026-10-31):** the seat quote is `SeatMaps/Price` plus `SeatMaps/Resolve {selectionId}`. `ValidateForAcceptance` is not built.

**Proposed route** (not present at verified `190aa74`):

`POST /Service/v1/Ancillaries/SeatMaps/ValidateForAcceptance`

```json
{
  "source": { "kind": "Offer", "reference": "flight-offer-id" },
  "seatSelectionIds": ["opaque-existing-seat-id"]
}
```

For Order Add, `source = {kind:"Order", reference:"123456", commercialVersion:3}`; **no** `pricedForTravelAgencyId`, `pricedForCustomerId`, `pricedForSurface`, `callerScope` or agency-specific DTO. Read-only, no Hold, no material inventory mutation. Strictly resolve and verify **same authoritative shopping context** and all selections; return one row for every requested ID:

```json
{
  "data": {
    "source": { "kind": "Offer", "reference": "flight-offer-id" },
    "expiresAtUtc": "2026-10-28T10:30:00Z",
    "seats": [{
      "selectionId": "opaque-existing-seat-id",
      "travellerRef": "pax-source-ref",
      "flightId": 1234,
      "seatNumber": "12A",
      "variantCode": "A07",
      "serviceDefinitionRef": "SEAT_STD",
      "definitionVersionId": 111,
      "provisionId": 222,
      "pricingId": 333,
      "pricingRateId": 444,
      "pricingVersion": 1,
      "pointOfSaleId": 555,
      "currencyId": 70,
      "quantity": 1,
      "unitAmount": 125000,
      "totalAmount": 125000,
      "isPriceComplete": true,
      "baseAmount": 125000,
      "taxLines": [],
      "feeLines": [],
      "documentRouting": "source-value",
      "documentRequired": false
    }]
  }
}
```

**Illustrative JSON:** enum symbols/price values/source IDs are not claims of current on-wire source. The implementing team must use actual Ancillary enum serialization and return the rest of **exact source definition/provision** fields used in ordinary Resolve (codes/SSRs/EMD routing/confirmation). `pointOfSaleId` is mandatory because `HoldSeatSelectionsRequest` requires it. It must come from a **single applied and unambiguous POS-specific quote**, not be guessed from the customer's agency/office or defaulted. If multiple POS match with different price but source ID cannot distinguish them, respond `SEAT_QUOTE_AMBIGUOUS`, not the cheapest price.

## 4. Acceptance validation step by step

1. Read and parse Seat selection ID; re-evaluate original source Offer/Order context with existing eligible passenger/flight and actual seat pricing/availability; `selectionId` alone cannot authorize price.
2. Confirm source matches request, all ids distinct, no omissions or extras, passenger and flight exist, variant A07/A08 only. POS selected unambiguously by source rules; currency equals order currency and price complete.
3. Seat pricing decomposes like doc 05, respecting included taxes, additional taxes, applied fees. Expiry must be at or after final write acceptance; no auto-repricing a changed source ID.
4. `OrderSeatService` gains a standalone Seat item and exact immutable priced snapshot; keep Air's original fare/allowance/price untouched.
5. Do **not** call Ancillary Seat Hold or FlightFlow at acceptance time. **Operational** test in doc 07 must later confirm FlightFlow actually reserves that exact selected seat and that its capacity was not double-held.

## 5. Relation to existing Ancillary Seat Hold and FlightFlow

The current Ancillary `POST /Service/v1/Ancillaries/SeatMaps/Holds` requires `OrderId`, `OrderCommercialVersion`, `PointOfSaleId`, `CurrencyId`, `Seats`, etc. It is **not** a direct substitute for FlightFlow's physical seat Hold. The current commercial Seat Hold verifier/allocator must be audited in Stage6B and its idempotence/read-back proved. Canonical physical owner stays FlightFlow; exactly one physical reservation per passenger+flight+seat must be evidenced. **Do not** accidentally reserve a paid seat in two providers or produce a second Seat Service.

## 6. Seat tests

- One bound two flight segments; passenger chooses `12A` on first and `18C` on second → two independent Seat Items/Services, never copied across flights.
- Two travellers, two seats on same flight; no collision. Duplicate seat/flight or two seats for one passenger rejected before write.
- One source id can correspond to multiple POS prices → quote refused, no guess. One unambiguous POS recorded and survives SQL reload, even if sales office changes.
- Modified seat price or definition after acceptance does not change historic sale; expired or wrong Offer/Order quote rejected.
- A07 free genuinely zero, A08 paid, both must be source-complete.
- A09 ExtraSeat is not saleable through this endpoint, not counted as ordinary passenger seat, must use its later approved special inventory/ticket path.
