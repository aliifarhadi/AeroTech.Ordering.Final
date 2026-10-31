# 05 — Source contract, selection coverage, accepted money and exact invariants

## 1. Trusted source ACL — one normalized basket

`IAncillarySaleSource.ResolveGeneralAsync(selectionId)`, `ValidateSeatsAsync(source,seatSelectionIds)` are **Application ports**; HTTP endpoint names/versions belong only in the Providers adapter. These calls are **read-only**: no Hold/Prepare/Confirm. Returned data must be parsed strictly into a typed `ResolvedAncillarySaleBasket` with:

- `SourceIdentity`: `{kind: Offer|Order, reference: string, orderCommercialVersion?:int, expiresAtUtc:DateTimeOffset, expectedLineCount:int}`. Existing response lacks several fields (**AC-01**).
- General priced rows grouped into actual current 10 arrays; full detailed definition/provision/pricing/selected; all 24 variant codes allowed when source complete, `travelInsurance[]` is reserved **not a 25th variant**.
- Seat rows with source-bound final price, one accepted source rate and POS for each passenger × flight (**AC-03**). Raw base64 SeatMap selection alone NEVER satisfies it.
- Accepted `sourceOfferItemId` identity, exact quantity, passenger and scope; **no client price, tax, item identity, document routing or POS inference**.

**Crosscheck**: after flattening family arrays, line count equals `expectedLineCount` exactly; all OfferItem IDs unique; exactly one source kind; source quote not expired at final acceptance just before write; order version matches lock; no missing array is treated as zero if the selection snapshot originally contained lines. If source can only return a partial selection, reject the entire basket.

## 2. Actual Ancillary general wire fields

`ServiceResolvedSelectionsDto` at `190aa74` has only `OrderId, OrderCommercialVersion, Baggages, Meals, Pets, AssistedTravel, AirportServices, PriorityServices, Connectivity, Upgrades, ExtraSeats, TravelInsurance`. For each `ServiceResolvedSelectionDto<TDetails>` current fields: `OfferItemId, VariantCode, SelectionScope, Unit, Coverage, SourceBinding, Definition, Provision, Pricing, Details, Selected`. Do **not** claim that it contains source-kind/OfferId/expiry/line count until AC-01 merges.

`Definition`: real `DefinitionId`, `ServiceDefinitionRef`, `VariantCode`, `ServiceTypeCode`, `ServiceSubCode`, `Rfic`, `Rfisc`, `SsrCode`. `Provision`: real `ProvisionId`, `CoverageScope`, `PurchaseStage`, `BookingMethod`, `BookingRequired`, `ConfirmationRequirement`, `FulfillmentProviderKey`, `DocumentRouting`, `DocumentType`, `DocumentRequired`, `RequiresCheckAtReserve`. `Pricing`: real `PricingId`, `PricingRateId`, `PricingVersion`, `CurrencyId`, `Quantity`, `UnitAmount`, `TotalAmount`, `PricingUnit`, `BaseAmount`, `TaxLines[]`, `FeeLines[]`; no `IsUnitTotalComplete` yet. `Selected`: `Quantity`, `Options` typed by family. `Unit` is one of `OrderUnit`, `TravellerUnit`, `TravellerPortionUnit`, `TravellerFlightUnit`. `Coverage` contains traveller, portion and flight refs; **do not assume** every scope currently enumerates every flight.

The source's `SourceBinding.OfferHash`/`SalesHash` is internal integrity evidence. Preserve if needed by the **quote validation adapter**, but do **not** add buyer/channel/agency-specific fields or infer identity from a hash in Ordering Domain.

## 3. Source choice/coverage mapping

| Source `CoverageScope` | `SelectionScope` | Beneficiary | Exact coverage mapping |
|---|---|---|---|
| Sector | PerTravellerPerFlight | one traveller | resolved `unit.flightRef` → 1 active segment/AirService for traveller; reject duplicate/mismatch |
| Portion | PerTravellerPerPortion | one traveller | trusted portion/BoundRef → all AirServices inside that exact portion; require source context proof |
| Journey | PerTraveller | one traveller | Offer source: every flight of the exact accepted Offer for that traveller (decision D13, 2026-10-31). Order source: only the flights the source names |
| Order | PerOrder | no traveller | zero or source-authoritative order-wide covered AirServices; `TravellerId=null` |

Request from client contains **IDs only**. For Create map source `travellerRef` to `OfferReader.GetTravellerRef(Index)` and then stable newly-created OrderTraveller.Id, and `flightId`/source flight refs to Order.Segment and matching AirServices. For Add map Ancillary's order context refs (current Ancillary uses stringified **OrderTravellerId**, not legacy offer `travellerRef`) to current Order Traveller/Segment. **Never use traveller index as OrderTravellerId.** `BoundId` must agree with the source covered portion; a two-flight portion is ONE purchased service with two covered AirServices. An `Order` scoped service is not assigned to first traveller.

## 4. Price algorithm — checked against `AncillaryPricingRate.cs:64-74`

**Source algorithm:** `UnitTotal = BaseAmount + taxes NOT IncludedInBase + fees whose FeeApplicationUnit==Item`, and `IsUnitTotalComplete = false` if any unapplied fee or unknown tax treatment. Included taxes are **already inside** Base; don't charge twice.

For each source row:

```text
q = Pricing.Quantity  (int > 0)
assert Selected.Quantity == q
assert Pricing.CurrencyId == Order.CurrencyId
assert every component.CurrencyId == Pricing.CurrencyId
assert Pricing.IsUnitTotalComplete == true  // AC-02 missing on current wire
assert every tax.TaxTreatment in {IncludedInBase, AddedToBase}
assert every fee.FeeApplicationUnit == Item
includedTaxPerUnit = sum(tax.Amount if IncludedInBase)
baseNetPerUnit = Pricing.BaseAmount - includedTaxPerUnit; require >= 0
expectedUnit = baseNetPerUnit + sum(all tax.Amount) + sum(all applied fee.Amount)
assert expectedUnit == Pricing.UnitAmount
assert Pricing.TotalAmount == expectedUnit * q
```

All amounts decimal, no binary floating point/rounded guessing. Source component categories/fee treatment are checked; unsupported Order-level or Booking-level unapplied fee = **reject quote** and ask Ancillary to return a *separately applied, authoritative, complete order-level charge*, not silently omit or put it in another item. No FX fallback: different currencies fail until a source-authoritative conversion snapshot exists.

## 5. Materialization in existing Ordering ledger

Create separate immutable PricingLines with `CustomerPrice` and direction `Credit`, each with exactly one `PricingAllocation(OrderItemId,OrderServiceId)`; `TravellerId` only when real and `OrderSegmentId` only when **exactly one** covered segment. For a multi-flight product, never duplicate the total across segments.

```
ancillary-base-line = baseNetPerUnit * q      Category=Ancillary, SubCategory=AncillaryBase(6)
included-tax-line  = taxIncludedPerUnit * q Category=Tax, SubCategory=Tax
tax-added-line     = taxAddedPerUnit * q    Category=Tax, SubCategory=Tax
applied-fee-line   = feeItemPerUnit * q     Category=Fee, SubCategory=ServiceFee
sum(all) = Pricing.TotalAmount
```

Keep line-level `Code/Description/Reference`, tax CountryId/StationAirportId and source TaxTreatment/FeeApplicationUnit as **small typed source evidence** where available (not source-generated financial authority); preserve `PricingId`, `PricingRateId`, version and `PricingUnit` on accepted service snapshot. No fake tax code, ATPCO/EMD, FX, settlement-only reclassification or inferred Refundability. Existing PricingLine `SignedEquivalentAmount` is positive for `Credit`: DO NOT invert its sign. `ReconcileTotals()` performs the total and allocated Item AcceptedTotal check after **all** accepted rows.

Worked test: Base=100 includes tax=10; added tax=5; item fee=3; qty=2; UnitTotal=108; Total=216. Accepted base=180, included tax=20, added tax=10, item fee=6; Σ=216. A gross base=200 + taxes=30 + fee=6 **must fail** (double-count).

`Zero` valid only when **real** full source says Base=UnitTotal=Total=0 and complete rate, including no missing fee. A product named `Free` does not make its price zero. A03 per-kg incomplete source and unknown non-item fees cannot be accepted.

## 6. Stable identity/conflict rules

- Same source `OfferItemId` in a single basket, duplicate seat id, same passenger+flight with two seats, two passengers with same physical seat in a flight: reject atomically.
- For Add, an already active identical product+scope+selected choice (beyond allowable capacity/quantity) is not blindly duplicated; a valid second purchase is allowed **only if actual Ancillary basket rules and quantity/capacity permit it**. Use source `SelectionBasketRules` where available and Order's existing active services in the verified Order context. Do not invent universal maximum quantity per Variant.
- Keep historical inactive occurrences; never delete/rewrite them. `Active` and `Cancelled` are distinct, do not auto-turn Cancel into Refunded.
- A newer price/definition revision does not overwrite accepted old snapshot.

## 7. Error behavior

Expired quote → 409 named error 2826; identity version mismatch → 409/2825 or 422/2827 depending state; insufficient source contents/fee/FX → 422/2828; ambiguous seat quote → 422/2834; upstream timeout/unavailable → 502; source absence prevents any writes. Errors are Ordering-mapped ACL exceptions with actual source detail retained for diagnostics (no raw sensitive payload exposed to Api).
