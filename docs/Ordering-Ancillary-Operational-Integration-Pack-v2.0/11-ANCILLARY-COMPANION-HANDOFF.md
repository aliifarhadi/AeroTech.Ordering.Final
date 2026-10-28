# 11 — Ancillary Team Handoff — required verified contract deltas, no security overbuild

**Target:** `aliifarhadi/AeroTech.Ancillary`, verified `k8s-stg@190aa74eadb103d8556a9fab03c469cb937160e8`. **Requester:** Ordering. **Do not silently edit this repository from Ordering Agent.** Record source changes in the established cross-service handoff ledger and verify against a pinned Ancillary commit. This handoff is an **implementation request**, not a claim that its proposed endpoints currently exist.

## 0. Change philosophy

The existing general `Price` → immutable `SelectionSnapshot` → Service `Resolve` path is good and MUST remain. The service's domain should not depend on an Ordering Order or book inventory during Price/Resolve. Add only missing facts needed to **prove a sold commercial selection** (source identity, applied price, variant-selected data), and support repeat shopping from a populated Order.

**DO NOT ADD** `pricedForCustomerId`, `pricedForTravelAgencyId`, `pricedForSurface`, `pricedForOfficeId`, `callerScope`, a 24-variant endpoint family, duplicated per-channel accepted-quote models, provider-level user identity, security credentials, hardcoded PCC, new Pricing/Saga aggregates or unverified EMD numbers. Sales-context authorization belongs to a separately approved policy stage; preserve existing authorization where present. The sole `PointOfSaleId` below is a **source-specific seat pricing/hold commercial fact**, not a security scope field.

## 1. AC-02 — complete price indicator, first priority

**Source:** `AncillaryPricingRate.IsUnitTotalComplete` is already computed; `ServiceResolvedPricingDto` currently omits it. Add `bool IsUnitTotalComplete` directly to each resolved Pricing DTO and mapper. Remain truthful: base + **added** tax + applicable item fee contributes to customer total; included tax already sits in base and is **NOT** added again. An unapplied fee (`FeeApplicationUnit != Item`) or unknown tax treatment makes current quoted total **incomplete**. Do not silently make the flag true, or fill the gap with zero.

**Accept:** A quote with included tax and complete pricing returns true and exact component amounts; a quote with missing/non-item fee handling returns false. Preserve amounts in decimal, currency IDs and selected Quantity exactly. Do not change Price calculation in Ordering.

## 2. AC-04 — populated Order shopping, first priority

**Source:** `OrderingAncillaryContextMapper.ToDomain` currently rejects nonempty `OrderingAncillaryContext.AncillaryServices` at line17 and always constructs `OrderCommercialContext(..., Services:[])`. Remove this deliberate guard and map every purchased/previously ended service to `OrderServiceFacts` with matching:

- `OrderServiceId:long` positive, unique;
- `TravellerRef:string` = source-formatted actual Order traveller id **only for pax-scoped service**; **nullable/empty for PerOrder**, without assigning a fake passenger;
- `ServiceRef:string` = genuine `ServiceDefinitionRef`, not `VariantCode` or `OfferItemId`;
- `PortionRef:string?`, `FlightRefs:string[]` sourced from covered Air Services/Journey/Segment, **not** from assumptions about first flight;
- `Quantity:int` >0 for active purchase; `CommercialState` mapping accepted/cancelled according to actual source enum and duplicate-selection rules.

For `PerOrder`, the current wire `OrderingAncillaryService.TravellerId:long` must become `long?`, and `OrderServiceFacts.TravellerRef`/`ExistingAncillaryServiceFacts` must represent absence accurately. Do not guess a single traveller for cabin Wi-Fi/CIP sold at Order scope. Validate all referred AirServices and travel refs against the same Order. Define meaningful semantics for `Cancelled` so an ended product doesn't wrongly block repurchase. Update Order-based `AncillaryOffers`, `Price`, Seat availability and `OrderSourceFingerprint` tests: after one purchase, second listing returns valid results (no 503), recognizes active selections and can reject duplicates deterministically.

**Accept:** Order with no ancillary, with one, with two different variants, with two segments, PerOrder and with a cancelled ancillary all succeed through Shopping/Price. A broken reference is rejected with correct business status, not mis-mapped.

## 3. AC-01 — General Resolve's source identity and exact basket proof

**Source:** `ServiceResolvedSelectionsDto` today gives `OrderId`, `OrderCommercialVersion` and 10 arrays only. For Offer-based Create it does **not** return OfferId/source kind/expiry; opaque `SelectionId` can be passed from wrong source. Extend Service Resolve **header** with:

- `SourceKind` = actual enum `Offer` or `Order`; map from **persisted** `SelectionSnapshot.SourceKind`;
- `SourceReference`: original trusted OfferId string or OrderId string, from snapshot persisted when Price ran;
- `ExpiresAtUtc`: existing SelectionSnapshot expiry (must be valid at acceptance);
- `SelectedLineCount`: actual `SelectionSnapshot.Lines.Count` (not derived only by counted response groups).

Keep current `OrderId` and `OrderCommercialVersion` for backwards compatibility. With `SourceKind=Order`, actual `OrderId` and `SourceReference` must agree; with `Offer`, source reference must match priced OfferId. The count must equal total returned effective resolved lines, so a mapper that silently drops an unrecognized variant fails rather than partially accepting a basket. Add tests for swapped OfferId, swapped OrderId, stale version, expired snapshot, and unrecognized selection row. No caller-provided source hash overrides persisted facts.

**Do not** include full call channel/agency/customer details in Resolve. Existing SourceBinding and SalesBinding can remain internal evidence; no ad hoc second authorization layer.

## 4. AC-03 — Seat acceptance quote, not Seat Hold

**Source:** `SeatMaps/Resolve` currently resolves raw `SeatMapSelectionId` with `TravellerRef`, `FlightId`, `PricingId`, `SeatNumber` and rate *choices*, not **one final source-bound accepted money amount**. The live `Service/v1/Ancillaries/SeatMaps/Holds` requires `PointOfSaleId`, `CurrencyId`, `OrderCommercialVersion`. Offer source-based seat IDs do not currently carry a complete OrderVersion/expiry proof.

Design a **read-only** `POST /Service/v1/Ancillaries/SeatMaps/ValidateForAcceptance` (PROPOSED endpoint; it DOES NOT EXIST at this source SHA) with caller's original source reference (`OfferId` or `OrderId`+CommercialVersion) and `seatSelectionIds[]`. Source resolves canonical offer/order shopping data and computes one final rate for each `(TravellerRef, FlightId, SeatNumber)` under actual sales/point-of-sale context. Required output per seat:

`selectionId`, `variantCode` A07/A08, `travellerRef`, `flightId`, `seatNumber`, `serviceDefinitionRef`, `definitionVersionId`, `provisionId`, `pricingId`, `pricingRateId`, `pricingVersion`, `pointOfSaleId`, `currencyId`, `quantity=1`, `completeUnitTotal`, `baseAmount`, `taxLines[]`, `feeLines[]`, `isTotalComplete`, `expiresAtUtc`, source binding to Offer or Order/CommercialVersion, documentRouting and booking metadata when source actually has it. The names are **proposed design**, align them with actual typed source without losing information.

**Invariants:** one and only one eligible priced rate for that traveller/POS/seat; no ambiguity, no expired selection; source gives exact chosen segment; no physical Hold in this endpoint, no seat allocation, no Ancillary Reservation. If current price engine cannot prove uniqueness and final price, return business conflict, not default cheapest rate or zero. Current valid `PointOfSaleId` must be retained as sold commercial fact for later seat Hold.

**Accept:** two travellers get distinct quotes for same map seat if rules vary; same traveller selects two different flights; variants A07 and A08 map to correct source; expired/mismatched source, insufficient POS and dual-rate ambiguity fail. Retry pure validation is side-effect free.

## 5. AC-05 — truthful source coverage and identity (required with AC-04 and AC-01)

`ServiceResolvedSelectionDto` already contains `SelectionScope`, `unit`, `coverage`, `sourceBinding`, selected values. For `Sector`, choose exactly one real Order Flight; for `Portion` include *all sold flights of that bound*; for `Journey` map accepted **actually covered** flights consistent with the definition; for `Order` no mandatory pax and potentially no covered AirService. Do not infer airline-wide itinerary from a single bound/airport. Expose enough immutable source identity to let Ordering validate `0..N CoveredAirServiceIds` without independently recalculating Ancillary eligibility. Under-source/no mapping => `BLOCKED_SOURCE`, not null refs or invented FlightId. Verify `SourceFingerprint` covers actual commercial version but does not mislabel a changed version as valid. Scope ≠ SelectionScope; preserve both.

## 6. AC-06 — support 24 variants **without** fabricating the currently absent wire data

The **domain registry** defines A01–A24. This does not mean all are currently selectable/fulfillable. `ServiceResolvedSelectionsMapper` currently always returns `extraSeats=[]`, `travelInsurance=[]`; the latter isn't an A01–A24 product. The current General Price model does not carry all fields of A17 medical/Oxygen and A19 UMNR free-text selections. Requested corrections:

| Variant | Current gap | Exact source-constrained correction |
|---|---|---|
| A09 ExtraSeat | no `extraSeats` selections in Resolver output; no general Hold member | Define *actual* acceptance quote+typed source choice only after ExtraSeat physical/ticket obligations can be fulfilled; don't pretend regular seat is ExtraSeat |
| A17 MedicalEquipment | Price `AssistedTravelSelectionOptions` lacks fields from `CustomerSelectionContract` e.g. EquipmentCode, EvidenceDocumentRefs, OxygenUnits, OxygenFlowRateLitersPerMinute, FrequentTravellerMedicalCardNumber | Extend typed Price, selected snapshot, Resolve and accepted typed DTO with **exact Source CustomerSelectionContract** semantics; treat sensitive medical data with existing privacy rules; no invented empty text or fake equipment |
| A19 UnaccompaniedMinor | CustomerSelectionContract requires `GuardianHandoffContact` and `GuardianPickupContact` **text**; current Price/Resolve missing | Supply exact required strings in typed selection/Resolve; never map `...Provided:bool` into contact text |
| A13/A14 Pet | General Hold has no `Pets` member; may require supplier confirmation | Do not advertise `OperationalSellable` until a genuine provider/approval/booking route exists. Price/Resolve can be modelled distinctly from Hold. |
| A18 Bassinet | infant/guardian selection in current Price, source approval/physical feasibility may differ | Return proven INF/ADT linkage; no auto guaranteed bassinet if supplier confirmation required |
| A10 Upgrade quote | ExternalQuote/DynamicQuote not equivalent to Filed Base | Only enable `FixedAncillary` with complete true price unless real dynamic quote/exchange path exists |

Each A-code must have an **explicit per-stage** `DomainSupported / SourceQuoteSupported / HoldSupported / DocumentSupported` status from evidence. Product-specific sellability is policy derived from these verified facts; no variant-global "yes because in enum" default. Never create zero fares solely for tests to represent a live paid product.

## 7. AC-07 — downstream operational support, not a hidden scope shrink

Ordering may safely accept priced general ancillaries only when it either rejects Reserve/Issue early before any provider effect or has a real `IReservationProvider` adapter. A live airline launch for a product additionally needs:

- actual `POST Hold`, `GET Hold`, `Confirm`, `Release`, `CancelConfirmed` semantics including timeout/readback, per-unit matching, expiry and atomicity;
- SeatMap commercial Hold **alongside physical FlightFlow authority** with no duplicate physical inventory consumption;
- ability to identify whether product is SSR/service only, EMD-A, EMD-S or ticket/exchange; no guessed RFIC/RFISC or fake EMD number;
- supplier manual approval, pet, assistance, ExtraSeat and dynamic upgrade *fail closed* until actual providers and documents exist.

**No implementation work in Ordering is permission to rewrite Ancillary's operational hold model.** Hand off only proven missing source semantics and test cases.

## 8. Cross-service merge sequence and proof

1. Merge **AC-02 + AC-04** first, pin Ancillary SHA, and test both including populated Order shopping. This unblocks **6A.1 Domain/fixture integration**, NOT safe live Add by itself: Add also needs proven source OrderId/version/expiry and correct coverage.
2. Merge **AC-01 + AC-05**, pin SHA, test both source-bound **Order Add** and **Offer-based Create** with exact 0..N coverage; only **now** can 6A.1 and 6A.2 reach full live acceptance (per supported product). No shortcut based on opaque selectionId alone.
3. Merge **AC-03**, pin SHA, test paid seats source quote/POS; establish 6A.3.
4. Merge **AC-06** per source truth, with variant-by-variant support / block; establish 6A.4 only when all source gaps actually implemented; gated products may be excluded from **live** sale but never from Domain model or tests.
5. **AC-07/6B**: real Hold/Confirm/recovery/ETKT/EMD/Cancel and channel payment policy for any product to be declared airline-operational. No 6B certification from mock HTTP.

## 9. Handoff acceptance table (each must include an actual HTTP trace)

| Contract | Positive | Negative | Code evidence | Source SHA |
|---|---|---|---|---|
| AC-02 | Complete including included-tax | Unknown tax fee unapplied false | `ServiceResolvedPricingDto` + tests | Required |
| AC-04 | Repeat shopping with 0,1,N purchased services | Corrupt service coverage ref | `OrderingAncillaryContextMapper` | Required |
| AC-01 | Correct source and line count | Cross-offer, cross-order, stale/expired | `ServiceResolvedSelectionsDto` + `SelectionSnapshot` | Required |
| AC-03 | Unique paid seat/POS price | Multiple rates or missing POS | Seat acceptance quote controller/service | Required |
| AC-05 | Sector/Portion/Journey/Order coverage | Mismatch traveller/flight set | `OfferCoverageDto`, source mapper | Required |
| AC-06 | Typed choices A09/A17/A19 when real | Missing selections, hidden fields | Price/Resolve roundtrip | Required |
| AC-07 | Real provider success/recovery | Unknown, provider refusal, expiry | Service Hold + FlightFlow + EMD authority | Required |

**No claim that AC-01…07 are implemented until each pinned source SHA and live trace exist.**
