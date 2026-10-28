# 03 — Canonical DDD model, fields, invariants and enum policy

**Authority:** live Master v2.0 on the actual local branch. Modify §§6.1,6.4,6.6,6.7,6.8,7–9 and Stage6 in place as directed in doc 13. The old 7c1ddaaa snapshot is **not** authority.

## 1. Roots — no change to ownership

`Order` owns commercial `OrderItem`, `OrderService`, `OrderChange`, `PricingLine` and accepted fact snapshots. `FulfillmentReservation` owns provider reservation/confirmation truth and recovery. `FulfillmentTask` owns external operation history. `ElectronicTicket`, `ElectronicMiscDocument`, `DocumentStock` own traffic documents. **Ancillary microservice** owns definition, product rule, quote/selection, ancillary capacity/hold; **FlightFlow** owns physical aircraft/seat and flight capacity; **AirOffer** owns flight offers, not the ancillary catalog. No new root is justified.

## 2. `OrderService` beneficiary correction — explicit exception to old Master

Change `OrderService.TravellerId: long` to `long?` **once** (EF, domain, query DTO, projections and commands). Keep `long` for Air/Seat subtype constructor arguments; `OrderAncillaryService` can pass `null` only if **actual source `SelectionScope=PerOrder` AND `CoverageScope=Order`**. All other selections require exactly one traveller. An Order-level product is one commercial service with beneficiary the Order, **not** a made-up primary passenger. Amend the Master §6.1 invariant and §6.4: passenger-specific grain applies to air and passenger-specific services; `PerOrder` optional service is an explicitly allowed exception. Do not silently leave an old contradictory sentence.

Reasons: Ancillary `ServiceCoverageScope.Order` and `AncillaryReservationUnit` use no traveller; fixed “exactly one beneficiary traveller” in old Master cannot represent it correctly. The current production catalog's availability of Order-level products is not verified (dev DB not accessible) but the **model is source-required**. If raw source does not emit one, gate its live tests, not its Domain shape.

## 3. Commercial occurrence grain / root invariants

- **One resolved priced purchase unit → one `OrderItem` and one `OrderService`** (quantity can exceed one). Do not create a new Item for each kilogram, piece, flight leg, passenger reference or supplier hold unit unless the actual offer separately prices them.
- A01–A06 use `OrderBaggageService : OrderAncillaryService`, distinct from the **included** `OrderAirTransportService.Checked/CabinBaggageAllowance`.
- A07/A08 use existing `OrderSeatService`, **one passenger × one flight**, linked to the matching Air Service; purchased Seat gets its **own `ProductType.Seat` OrderItem** and exact Seat pricing, never a second shadow Ancillary service.
- A09 is a **different `OrderAncillaryService`** (ExtraSeat) with `ProductType.Seat` but not `SeatAssignment`. Not a normal seat and not eligible for current Seat physical hold.
- A10, A11–A24 non-baggage/nonregular-seat use `OrderAncillaryService`. Preserve family/variant even when `ServiceType` is generic.
- `Order.Change.Create` when Create-with-selection; `OrderChangeType.AddProduct` on subsequent Add; one immutable provenance Change per successful batch; `CommercialVersion` unchanged by source reads or provider observations.
- `CustomerTotal` = accepted *historical* customer-facing signed PricingLines in Order currency. Do not turn cancellation into silent editing of original prices; financial refund/rebalance is a different sourced decision.

## 4. Stable fields on `OrderAncillaryService` (snapshot is Order-owned, immutable)

| Name | Type | Required / when | Source / meaning |
|---|---|---|---|
| `Id,OrderId,OrderItemId,CreatedByChangeId,CommercialStatus,CreatedAt` | inherited | always | Ordering generated |
| `TravellerId` | long? | nonnull except `PerOrder` | resolved traveller → OrderTraveller.Id; never index as ID |
| `FulfillmentProviderKey` | string | always | **AeroTech port key `Ancillary`** for non-seat; **not** supplier's free-form `fulfillmentProviderKey` |
| `ServiceType` | enum | always | mapping below, never mislabel purchased baggage |
| `ServiceDefinitionRef` | string | always (validated nonblank) | Resolve `definition.serviceDefinitionRef` |
| `DefinitionVersionId` | long | always | Resolve `definition.id` (local source code uses versioned definition id) |
| `VariantCode` | string A01..A24 | always | Resolve, not client-invented |
| `Profile` | canonical string/enum | always | source family, match registry; not sales channel |
| `ServiceTypeCode`, `ServiceSubCode`, `SsrCode` | string? | source-backed | authoritative Resolve definition |
| `Rfic`, `Rfisc`, `DocumentRouting`, `DocumentType`, `DocumentRequired` | typed | routing present where source requires; codes nullable | source definition and provision; EMD is **not** issued here |
| `ProvisionId` | long | always | Resolve |
| `CoverageScope` | Sector/Portion/Journey/Order | always | source provision |
| `SelectionScope` | PerOrder/PerTraveller/PerTravellerPerPortion/PerTravellerPerFlight | always | source selection, validated mapping |
| `CoveredAirServiceIds` | distinct IReadOnlyList<long> | `0..N` | source-backed flight/portion coverage → exact active Air services; no synthetic traveler or segment |
| `Quantity` | **int** | `>=1` always | resolved `selected.quantity=pricing.quantity`, **not decimal** |
| `PricingId`, `PricingRateId`, `PricingVersion`, `PricingUnit` | typed | always | Resolve pricing, immutable provenance |
| `OfferItemId`, `SourceSelectionId` | string | always | user-visible source selection correlation (not authority itself) |
| `SourceKind`, `SourceReference`, `SourceOrderCommercialVersion` | typed | source-bound | Offer+OfferId or Order+OrderId+commercialVersion; never seller-scope-specific |
| `SelectedChoice` | **typed family VO** | determined by Variant | exact customer choices, no mutable catalog lookup |
| `DefinitionDetailsSnapshot` | **typed family VO** | source-defined required values | actual product identity/terms from quote; no invented limits |
| `PointOfSaleId` | long? | **required for quoted seat purchase**; null for general absent source | source price provenance, **not customer/agency security field** |
| `Refundability,Commissionable,Reusable,FormOfRefundCode` | typed nullable | only if actually returned by authoritative source | do not default unknown to true/false |
| `CommercialName`, `GroupCode`, `SubGroupCode`, `InterlineSettlementAllowed` | nullable | SOURCE-GATED | do **not** require/source-fabricate fields absent in current Resolve |

`OrderBaggageService` can add only truly sourced specific `ChargeKind`, allowance/weight/package and physical descriptors; **do not** use default 23kg/1 piece if not specified. Family-level typed VOs in §8 avoid 24 redundant TPT tables. `Quantity` is a count of purchased units; **not automatically** pieces or kilograms. Source pricing unit (`PerKilogram`, `PerItem`…) stays a separate fact.

## 5. Minimal append-only `OrderServiceType` policy (Q8 resolved)

Keep all existing enum values frozen. **Append exactly one generic `Ancillary`** if missing. Map as follows (the concrete subclass and `VariantCode` remain authoritative for source identity):

| Variants | `OrderServiceType` | Why |
|---|---|---|
| A01–A06 | `Ancillary` | `BaggageAllowance` means included allowance in this design, not paid bags |
| A07/A08 | `SeatAssignment` | existing Seat service |
| A09 | `ExtraSeat` | existing code, distinct fulfillment |
| A10 | `Ancillary` | No existing upgrade service type |
| A11/A12 | `Meal` | Existing matching type |
| A13/A14 | `Ancillary` | No Pet type; Profile=Pet |
| A15–A19 | `Ancillary` | No AssistedTravel type |
| A20 | `LoungeAccess` | Existing type |
| A21 | `Ancillary` | No FastTrack type |
| A22 | `Cip` | Existing type |
| A23 | `Priority` | Existing type |
| A24 | `WiFi` | Existing type |

Do not append 5 new types merely to repeat `VariantCode`; do not create a `BaggageAllowance` sale row or use `OrderServiceType.Other` for all. Existing legacy enum members keep their numeric values and meanings. The source `AncillaryProfile` is separate and retained even when type is `Meal`/`Cip`.

## 6. Coverage — strong invariants

`SelectionScopes.Of` source mapping is exact: `Order→PerOrder`, `Journey→PerTraveller`, `Portion→PerTravellerPerPortion`, `Sector→PerTravellerPerFlight`.

- **Sector**: one TravellerRef, one source FlightRef; map exact active `OrderAirTransportService` and one Segment; no duplicate active purchase of same exact product/seat slot beyond source policy.
- **Portion**: one TravellerRef and source PortionRef/BoundId; map all active AirServices for that traveller **in that source-bound portion**, once each. If mapping ambiguous, reject instead of guessing.
- **Journey**: one TravellerRef, explicit source contract describing included flights across whole journey. Where source does not identify whether all current flights or subset, **AC-05 coverage clarification is mandatory**; don't blindly attach all segments.
- **Order**: `TravellerId=null`, no made-up air coverage; `CoveredAirServiceIds=[]` unless a source specifies an exact supported order-wide flight coverage. Product remains one occurrence.
- Every covered AirService must be in same Order, commercially active and match selected traveller when scoped per-traveller. No derived `AirFare` or source `FlightId` as persistent `OrderServiceId`. Store stable sorted distinct `OrderAirTransportService.Id` references.
- For partially cancelled multi-flight coverage, do **not** arbitrarily prorate or duplicate a single indivisible ancillary purchase. Define dependency closure in doc 07; where source change/credit decision is missing, fail closed for partial cancel.

## 7. Seat (`OrderSeatService`) source snapshot and compatibility

Keep `SegmentId`, `AssociatedAirServiceId`, `SeatNumber` and `FulfillmentProviderKey=FlightFlow` for A07/A08; add optional immutable commercial facts on **newly purchased Seat**: `VariantCode` (A07/A08), `ServiceDefinitionRef`, `DefinitionVersionId`, `ProvisionId`, `PricingRateId`, `PricingVersion`, `AcceptedPointOfSaleId`, `SourceSeatSelectionId`, `AcceptedPriceEvidence` (typed). Legacy free `seatSelections[]` keep original records and old behavior but cannot be mistaken for priced seats; optional new fields remain null on history. Seat quantity =1. Same traveller+flight cannot have two simultaneously active seat assignments; one physical seat in a flight cannot be assigned to two Order travellers. A09 cannot use this class.

**`OrderSeatService` Stage5 air association remains to matching AirService**; future cancellation dependency always follows that Air reference. The A07/A08 quoted POS must be persisted so a later Ancillary Seat Hold can reuse it without re-derivation.

## 8. Typed selected customer data (not arbitrary JSON/EAV)

Use **only the following bounded, typed family VOs** as selected/commercial snapshots. They can be owned EF types/typed children; no arbitrary `Dictionary<string,object>`, 24-class TPT or store-and-hope raw JSON as sole historical truth:

- `BaggageSelection`: BagRef?, MeasuredWeightKg?, Dimensions?(length,width,height), WeightKg?; fixed PackageProductRef?, EquipmentKind?, BaggageChargeKind and numeric limits from source.
- `MealSelection`: MealCode?, MenuItemRef?, MealKind, DietaryCode?, CateringLeadTimeMinutes, exclusive family?; selected quantity lives at service-level.
- `PetSelection`: AnimalType, OtherAnimalCode?, AnimalAgeWeeks?, CombinedWeightKg, CarrierDimensions, DocumentAcknowledgements[]?, SizeBracket?, declared TransportMode and limits.
- `AssistanceSelection`: AssistanceSsrCode?; medical EquipmentCode?/EvidenceDocumentRefs[]?/OxygenUnits?/OxygenFlowRateLitersPerMinute?/FrequentTravellerMedicalCardNumber?; Bassinet InfantRef?/GuardianRef?; UMNR GuardianHandoffContact?/GuardianPickupContact? — source-gated where current wire omits them.
- `AirportSelection`: AirportId, TerminalRef?, FacilityId?, Direction, TimeWithOffset?, GuestCount?, IanaTimeZone?, ServiceWindow, IncludedComponentCodes[] as source actually returned.
- `PrioritySelection`: OptIn bool when required; source zone/group/airports.
- `ConnectivitySelection`: PlanCode, PlanKind, DeviceCount?, DurationMinutes?, IncludedDataMb?, MaxDevices?, DeliveryStage.
- `UpgradeSelection`: FromCabinId, ToCabinId, allowed UpgradeKind, TicketExchangeRequired; unpriced `ExternalQuote` not accepted.
- `ExtraSeatSelection`: SeatNumber?, Purpose?, ExtraOccupiedSeatCount?, adjacency/exit-row/ticket terms **only as exact source-defined facts**; A09 live Source contract missing.

Avoid declaring all of these as new `ValueObject` C# classes if they are never independently constructed; the actual type-set manifest must match the minimal implementation and Master. For gated fields, lock the name/type from `Ancillary.CustomerSelectionContract` in Domain tests **but do not persist fake source records or create placeholder tables purely for missing live Wire**. Validate an explicit complete trusted fixture with its provenance for all 24; live acceptance requires complete source.

## 9. Order totals, pricing & change provenance

- Create with ancillary: exactly **one existing Create Change**, version 1; price lines `InitialSale`. Do not call public Add HTTP within Create.
- Add: exactly **one `OrderChangeType.AddProduct`**, old version+1; price lines `OrderPricingReason.AdditionalSale=9` (append-only).
- Base Ancillary line subcategory `AncillaryBase=6` (append-only). Reuse existing `PricingLine`, `PricingAllocation`; no additional Pricing aggregate/table.
- Each acquired price line is immutable and allocated wholly to the purchased OrderItem and OrderService; one multi-flight occurrence remains one line per price component, not fictitious per-leg prices.
- `ReconcileTotals()` is the only place that computes new `CustomerTotal` and item `AcceptedTotal`; both follow existing signed-line rules.
- Cancel does not rewrite original accepted sale lines. An explicit future refund/reprice/credit decision may introduce financial reversal, not an assumed Stage5 behavior.

## 10. Do not create the following

No `AncillaryOrder`, generic booking Saga, 24 subtype aggregate roots, new pricing ledger, special per-surface channel pricing snapshots, client-produced tax/price values, local copy of Ancillary provisions/prices, fake `FulfillmentProviderKey=FlightFlow` for non-seat products, no `ReservationMode.None` used to bypass real unfulfilled purchase, no duplicate regular Seat occurrence, no anonymous `TravellerId=firstPassenger` shortcut, no fabricated EMD/ticket number.
