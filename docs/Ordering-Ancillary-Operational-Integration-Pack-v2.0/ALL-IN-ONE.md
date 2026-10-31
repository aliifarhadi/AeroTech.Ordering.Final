# AeroTech Ordering — Stage 6A/6B Operational Integration — ALL-IN-ONE v2.0

**Superseded in part, 2026-10-31:** decision D13 changes Offer-sourced Journey coverage, D14 replaces the Create inputs and the seat quote route, and D15 removes legacy `seatSelections`. See `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md`.

**Compiled from the 15 numbered source documents in this same pack.** Read their source-level evidence and the owner review. This is an implementation authority proposal to be reconciled with the **live local Master**, not proof of tested upstream integration.

---

# AeroTech Ordering — Stage 6 Commercial + Operational Ancillary Integration Pack v2.0 FINAL DESIGN

**Date:** 2026-10-28  
**Repository:** `aliifarhadi/AeroTech.Ordering.Final`, branch `k8s-stg`  
**Companion:** `aliifarhadi/AeroTech.Ancillary`, branch `k8s-stg`  
**Owner scope:** all **A01–A24** Ancillary variants, including **A07/A08 SeatMap**, in **Create Order** and **Add to existing Order**, plus the minimal necessary protection/integration of **Reserve, Confirm, Cancel and Issue** so an airline Order is not operationally inconsistent.  
**Status:** final **implementation design**, **NOT proof of passing live end-to-end**. Source-contract and unpushed-local-code gates are explicit and release-blocking.  
**Authoritative implementation order:** `00 → 01 → 02 → 03 → 04 → 05 → 06 → 07 → 08 → 09 → 10 → 11 → 12 → 13 → 14`.

## 0. What is approved versus what is not yet provable

1. **Ordering remote baseline verified:** `cd50a2a5fd18a372f32f2d4efac08dee0510f6ba` on `k8s-stg` as accessed 2026-10-28. **Owner review reports an unpublished local HEAD `6a7bb71`**, including a working `GET /Service/v1/Orders/{orderId}/AncillaryContext`, `AirFareType?` update and Master amendment `Quantity:int`. We **cannot inspect that unpushed commit** through GitHub. **Do not recreate or overwrite its work**; the coding agent working in the local repository must first verify the exact HEAD and use that source as authoritative. No false claim of rebasing on unobserved code.
2. **Ancillary baseline independently verified:** `190aa74eadb103d8556a9fab03c469cb937160e8`, latest committed `k8s-stg`. Earlier pack used stale `c873971`.
3. **Master:** the live working-tree `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md` wins. Do **not** use prior ZIP's `reference/ORDERING-MASTER-V2.0-READONLY.md`: its SHA-256 is `7c1ddaaa…`, while the owner's review reports local updated Master `1e51a948…`. This pack intentionally does **not** bundle the stale Master.
4. **Source contracts AC-01…AC-06 still pending.** No Agent shall claim general Create, Paid Seat, repeated shopping or all 24 live variants are integrated until actual Ancillary endpoints and HTTP tests prove them.
5. **24/24 means Domain representability and exactly accounted acceptance for any complete valid source selection.** It does **not** mean all 24 can currently be sold, held or ticketed. A09, A17, A19, some pet/medical/quote variants remain source-/approval-gated. No fabricated rate, fake Hold or substitute selected data.

## 1. Frozen architectural decisions

| ID | Decision | Why |
|---|---|---|
| D01 | Retain all six Master Aggregate Roots, **no new generic Order/Saga/Pricing aggregate** | Master authority, correct bounded contexts |
| D02 | **One canonical commercial acceptance pipeline**, used by Create and Add; surface mapping only at controllers | IATA Offer→Order and OrderChange, avoid duplicate business logic |
| D03 | `selectedAncillaries` canonical internal basket = `{selectionId?, seatSelectionIds?}`. Public API/OTA Panel use separate `services[]` **wire adapter**, preserving existing contracts | Amadeus/IATA-style separation of public API and internal model, no domain coupling to a channel |
| D04 | `OrderService.TravellerId` **nullable** only for true `PerOrder`; explicit Master §6.1/§6.4 amendment | Source Ancillary supports Order coverage with no traveller |
| D05 | `OrderAncillaryService.Quantity` **int**; `CoveredAirServiceIds` **0..N**; `SelectionScope` and `CoverageScope` retained independently | Actual Ancillary DTO + local Master amendment |
| D06 | `OrderServiceType`: reuse meaningful existing members; append **one** generic `Ancillary` for missing categories. Never mislabel purchased baggage as `BaggageAllowance` | Minimum additions, no 24 enum values |
| D07 | Paid/free A07/A08 = **one** `OrderSeatService` for each passenger × flight and **one Seat Item**. A09 is a *distinct* ExtraSeat product, **not** a regular seat | Accurate identity and future seat-capacity semantics |
| D08 | No `AncillaryAcceptanceReceipts`, **no new client idempotency key**. Add uses existing per-Order lock + `expectedCommercialVersion`; Create duplicate policy remains a *separately identified cross-Create production blocker* | `CLAUDE.md:76–82`; no special-case policy split |
| D09 | Preserve historical accepted PricingLines and calculate accurate included tax/excluded fee; no imaginary FX, tax, ATPCO or refund | Actual `AncillaryPricingRate` and Master PricingLine |
| D10 | Until all active ancillary services, **including newly source-priced A07/A08 seats**, have verified required provider/fulfillment/document routes, whole-Order Reserve and Issue **fail early with typed business errors**, before external effects. Never register a fake `ReservationMode.None` to bypass them | B1; prevent Flight reserved / ancillary unfulfilled discrepancy |
| D11 | **Cancel** includes all active ancillary/seat services in whole-Order cancellation; partial Air cancellation is dependency-aware and fail-closed for indivisible multi-flight ancillaries | Prevent orphan services, lossless commercial history |
| D12 | **Stage 6B** implements real ancillary Reserve/Confirm/Release/Cancel + Seat physical authority + EMD when applicable, using existing `IReservationProvider`/`FulfillmentTask` and Master EMD roots; live sale is gated until this works for each offered variant | A real airline cannot end at commercial acceptance |
| D13 | Keep current Service/API authorization unchanged. **No** `pricedForTravelAgencyId`, `pricedForSurface`, `pricedForCustomerId` or new channel-specific security schema | Owner decision: security/entitlement enhancement is separate |
| D14 | Keep Master updated **in place**, update `CLAUDE.md`, CI type manifest, `reports/` paths. Do not create new contradictory design authorities | Stage guardrail and owner reporting conventions |

## 2. Critical constraint: no false “production ready” claim

- **Domain-ready** means a trusted, typed, complete fixture for every A-code can roundtrip into Order, persisted pricing and the correct read model.
- **Commercial-HTTP-ready** means genuine Ancillary selection/quote and both Ordering Create/Add routes pass against the actual upstream; not just mocks.
- **Operational-ready** means Reserve/Confirm, Seat physical capacity, Cancel/release, payment authority at the channel boundary, and mandatory ETKT/EMD/SSR behavior have proven success, rejection, timeout/Unknown recovery and compensated cancellation where required.
- Production rollout must be **capability gated by real sellable variant/product**, not by whether its domain class exists. A variant with missing supplier fulfillment, a pending approval, or an incomplete price is not available for sale.

## 3. Required reading and final artifacts

Read the current Master on the agent's checkout, `CLAUDE.md`, `.canonical-stage`, local `AncillaryContext` source/tests, the source-contract analysis in this pack, and the review in `reference/REVIEW-v1.1-2026-10-28.md`. Report actual Git SHAs, tested cases and blocked contracts under **repository-root `reports/`**. Agent instructions in `12-ORDERING-CODING-AGENT-PROMPT.md` and companion instructions in `11-ANCILLARY-COMPANION-HANDOFF.md` are implementation-ready but **never authorization to modify the other repository**.


---

# 01 — Verified source audit, stale-baseline correction and explicit uncertainty

## A. Known source of truth and evidence levels

| Subject | Exact evidence | Status |
|---|---|---|
| Ordering GitHub `k8s-stg` | `cd50a2a5fd18…`, Stage5 R2, 2026-09-27 | **Directly verified** |
| Ordering local `6a7bb71` | Review file 2026-10-28; not on remote | **Reported by owner's coding agent, NOT independently inspectable** |
| Ordering local Master `1e51a948…` | Review SHA-256, includes `Quantity:int`, `FareType:AirFareType?` | **Reported**, must verify in working checkout |
| Ancillary `k8s-stg` | `190aa74eadb1…`, "Performance Fix", 2026-10-28 | **Directly verified** |
| Old embedded Master | SHA-256 `7c1ddaaab9b18f2c…` | **STALE — DELETED from final reference set** |
| Ancillary 24 variants | `AncillaryVariant.cs`: A01…A24, 9 `AncillaryProfile`s | **Directly verified** |

**First coding command** in actual local environment: `git status --short; git rev-parse HEAD; git branch --show-current; git log -6 --oneline; git show 6a7bb71 --stat` (or exact equivalent). If the commit was rebased/squashed, first confirm that **equivalent local AncillaryContext source, tests, migrations and Master amendments exist**. Stop only if that local functionality is genuinely absent or contradictory: report `BASELINE_NOT_AVAILABLE`. Do not reconstruct source from old remote or from an imagined API. Check Ancillary current SHA likewise before any DTO mapping.

## B. Ordering source-backed impact map

| Area | Remote source evidence | Required response |
|---|---|---|
| `Order.Create` | `Order.Create.cs`: original SeatSelections `TravellerIndex + BoundId + SeatNumber` applied to **all flights in bound** | Maintain legacy only for legacy clients; new SeatMap path is **traveller × actual flight** |
| `OrderService` | `Entities/OrderService.cs`: `long TravellerId` nonnullable | change to `long?` with subtype invariants; Master amendment |
| `OrderAncillaryService` | absent from remote Stage5 | materialize from §6.7, anchored to source |
| `OrderBaggageService` | absent from remote Stage5 | materialize §6.8; distinguish paid baggage from included allowance |
| `OrderChange` | `AddProduct=2` exists | reuse, one version increment |
| `PricingLine` | existing lines and allocations with `SignedEquivalentAmount` | reuse, derive CustomerTotal from genuine accepted lines |
| `OrderPricingReason` | 1–8 (`Cancel=8`) | append `AdditionalSale=9` only if still free in local |
| `OrderPricingLineSubCategory` | 1–5 (`Promo=5`) | append `AncillaryBase=6` only if still free in local |
| `OrderServiceType` | `SeatAssignment`, `BaggageAllowance`, `Meal`, `LoungeAccess`, `Cip`, `ExtraSeat`, `Priority`, `WiFi` exist | type table in 03; append generic `Ancillary` only if not present |
| `OrderReadModelSnapshotFactory` | supports Air/Seat only, other subtype throws | add variant-aware service projections |
| `OrderRepository` | loads all collections, current TPT | add new subtype+owned collections to eager-loaded aggregate/query mappings |
| `ReserveService` | required services consult provider resolver before any downstream action; missing key 2505/500 | typed **pre-provider** guard, not fake `None` capability |
| `ReservationProviderResolver` | `RequiresReservation` resolves `FulfillmentProviderKey` | distinguish no adapter/inactive product from business readiness |
| `Order.CancellationScopeOf` | only Air + dependent infants + their Seat | include accepted ancillary and whole-Order cancellation, enforce dependency/partial rules |
| `Order.Cancel` | marks service/item commercial states; **does not write price reversal or recompute CustomerTotal** | preserve history; never claim Stage5 automatic financial reversal |
| `OrderingUnitOfWork` | command DB `SaveChangesAsync`, then query DB `SaveChangesAsync`, **no shared transaction** | command rows atomic; projection is **not** jointly atomic; test recovery path |
| `IssueOrderService` / `IssuancePlanner` | issue **ETKT only** for Air; does not issue EMD | explicit outstanding-required-service/document policy; EMD Stage6B gate |
| `.canonical-stage`, `CLAUDE.md` | stage5 manifest/type-enum authority and old reports ban | update guard, activate Stage6 only at incremental close; `reports/` exception |

## C. Ancillary source-backed gaps (at verified `190aa74`)

| Code | Actual source | Missing proof / result |
|---|---|---|
| AC-01 | `ServiceResolvedSelectionsDto` has OrderId, OrderCommercialVersion and 10 family arrays | **NO** `sourceKind`, `sourceOfferId`, `expiresAtUtc`, selected-line count; pre-Order Create cannot prove the quote belongs to its Offer |
| AC-02 | `ServiceResolvedPricingDto` includes source pricing version, quantity/unit/total/base/taxes/fees | **NO** `isUnitTotalComplete` even though `AncillaryPricingRate` computes it |
| AC-03 | `SeatMaps/Resolve` returns pricing definitions/rates and bare decoded `(travellerRef, flightId, pricingId, seatNumber)` | **NO** unique source-bound final accepted price, expiry, POS identity; cannot use it for a paid Seat sale |
| AC-04 | `OrderingAncillaryContextMapper:17`: requires `AncillaryServices.Count == 0`, returns `services: []` | **Second Ancillary shopping after first Add breaks** |
| AC-05 | `SelectionScopes.Of`: `Order→PerOrder`, `Journey→PerTraveller`, `Portion→PerTravellerPerPortion`, `Sector→PerTravellerPerFlight` | exact source-scoped **coverage** must be preserved for multi-flight and no-traveller services; don't guess flight refs |
| AC-06 | General `ServiceResolvedSelectionsMapper` hardcodes `ExtraSeats=[]`, `TravelInsurance=[]`; medical/UMNR options not complete | A09, A17, A19 source-incomplete; A10 DynamicQuote and approval-dependent A13/A14/A18 need capability checks |
| AC-07 | `Service/v1/Ancillaries/Hold`, `Hold/{id}/Confirmations`, `DELETE Hold/{id}`, `Hold/{id}/Cancellations`, `GET Hold/{id}` exist; SeatMaps `Holds` separate | Production `IReservationProvider` adapter and exact real capability proof needed; **Hold excludes pets and ExtraSeats** |

## D. More source findings that were under-specified in v1.1

1. `AncillaryPricingRate.UnitTotal` code: `BasePrice + (Tax not IncludedInBase) + (Fee with FeeApplicationUnit==Item)`. `IncludedTaxes` already inside `BasePrice`. `IsUnitTotalComplete` false on unapplied fee or unknown tax treatment. This is directly verified at `AncillaryPricingRate.cs:64–74`. Don't double charge included taxes.
2. `SeatMapSelectionId.Encode` uses base64url `travellerRef|flightId|pricingId|seatNumber`, not signed quote/purchase proof. A price chosen from multi-PointOfSale contexts needs an authoritative `pointOfSaleId` or immutable source quote to be held later, not a fabricated agency-related field.
3. Ancillary `HoldSeatSelectionsRequest` requires `PointOfSaleId:long`. That fact is **commercial reservation context**, not identity/authorization; accept quote must carry it to Ordering's immutable Seat snapshot or fulfillment cannot be correct later.
4. Ancillary `OrderingAncillaryContext.OrderingAncillaryService.TravellerId` is nonnull (`long`) at `190aa74`, while its Order-coverage reservation unit requires null. Source wire must become nullable before a genuine PerOrder purchase can be shopped repeatedly.
5. Master current **local** amendment reportedly makes `OrderAncillaryService.Quantity:int`, not the old stale `decimal`. Never repeat v1.1 decimal.
6. The old `CLAUDE.md` header's legacy aggregates and `OrderStateMachine` text is explicitly superseded by canonical Master per CLAUDE.md §144. Do not revive it.

## E. Consequence for coding

A real `AddAncillaries` backend can be tested with trusted fixtures before AC-01/AC-03, but **no live commercial source integration can close** without the relevant AC items. All CI/test reports must distinguish: `DOMAIN_PASS`, `MOCK_CONTRACT_PASS`, `LIVE_SOURCE_PASS`, `BLOCKED_SOURCE`, `BLOCKED_LOCAL_BASELINE`. A success from test fixture is NOT a success of actual upstream shopping.


---

# 02 — Audited airline benchmark: what is documented, and what is NOT proved

**Method:** primary public IATA/Amadeus/Sabre/Travelport/Duffel and airline websites were opened/rechecked on 2026-10-28. This document separates **observable API semantics** from **our design choices**. No vendor UI is treated as proof of its internal PSS schema.

| Authority | Verifiable behavior | AeroTech consequence | Strict limit |
|---|---|---|---|
| **IATA Enhanced & Simplified Distribution / NDC implementation guide 21.3** | `OrderCreate` uses selected offer; `OrderReshop` returns offers for changing existing Order; `OrderChange` applies new/changed OfferItems. Worked UC06 explicitly shows flight change cancelling dependent ancillaries. | Create and Add have a shared commercial acceptance kernel; OrderChange history and dependency-aware cancellation; do not conflate OrderChange with confirmation/fulfillment. | Standard messaging model; not proof of AeroTech provider features or a specific REST URL. |
| **Amadeus for Developers** | Price confirmation precedes OrderCreate; chargeable seat is selected per `travelerId` **and `segmentId`** inside `fareDetailsBySegment.additionalServices.chargeableSeatNumber`; priced flight offer then goes to Flight Create Orders. | Exactly one Seat selection per traveller × flight; an authoritative seat price quote must be accepted; external API maps to an Amadeus-inspired `services[]`/offer-item reference, not the internal DTO. | Self-Service cannot be used to claim unrestricted post-ticketed Add or a particular internal DTO. |
| **Sabre NDC API** | `/offers/getSeats`, `/offers/getAncillaries`, `/orders/change`, `/orders/create`, `/offers/price` exist; documented add/cancel seat and ancillary, separate fulfillment actions, pre/post order flows. | Ancillary own shopping/price; Ordering owns Order mutation; a product's fulfillment is a separate capability, never a consequence of merely accepting pricing. | Sabre NDC capabilities differ by carrier/content; API name is not an unconditional capability. |
| **Travelport JSON API 11** | Existing booking ancillary flow: new workbench → ancillary shop → **ancillary price (NDC)** → ancillary book → commit; seats are a separate guide; EMD/FOP depend on carrier. | Quote validation and commit boundary; post-create Add is a real operation, but quote/booking/payment/EMD eligibility must be checked independently. | Not universal for GDS/NDC/carriers; cannot infer Hold/Issue implementation. |
| **Duffel** | Create Order can contain `services`; GET order available services then POST `/air/orders/{id}/services` adds service. Documentation explicitly excludes **hold-type orders** from post-order service addition and from create-with-services in hold cases. | Two entrypoints, source-issued references, and **status/capability-specific** support: not all Orders admit Add. | Duffel's payment-coupled semantics are **not copied** into AeroTech's payment-neutral Order core. |
| **flydubai** | Manage Booking allows extras after booking; paid seats are passenger- and leg-specific; extra seat (EXST/CBBG) requires distinct handling and can carry adult-fare/ticket implications. | A07/A08 regular Seat ≠ A09 ExtraSeat; passenger×flight identity; don't treat generic ancillary as operationally bookable. | Consumer help pages; not internal PSS/API proof. |
| **Breeze** | Bags can differ by outbound/return leg, and paid seats are selected/modified per guest/flight after booking. | Correct source coverage/segment scope and journey-dependent changes; a partial flight change must not silently retain a mismatched product. | Consumer workflow; no proprietary pricing-reservation model disclosed. |

## Reference URLs (primary and independently checked)

- IATA 21.3 Implementation Guide: https://guides.developer.iata.org/docs/21-3_ImplementationGuide.pdf (OrderCreate, OrderReshop, OrderChange, UC02/03/06)
- Amadeus official developer guide: https://github.com/amadeus4dev/developer-guides/blob/master/docs/resources/flights.md (seat/segment/price flow)
- Amadeus FAQ: https://admin.developers.amadeus.com/self-service/apis-docs/guides/developer-guides/faq/
- Sabre NDC Product Collection: https://developer.sabre.com/product-collection/new-distribution-capability-ndc/v1/index.html
- Travelport API Reference: https://support.travelport.com/webhelp/jsonapis/airv11/content/air11/APIReferences.htm
- Travelport Ancillary and EMD Guide: https://support.travelport.com/webhelp/JSONAPIs/Airv11/Content/Air11/Ancillaries/AncillaryGuide.htm
- Duffel Orders: https://duffel.com/docs/api/orders
- flydubai Manage Booking: https://www.flydubai.com/en/book-and-manage/view-or-change-booking/
- flydubai Seat/ExtraSeat: https://www.flydubai.com/en/book-and-manage/add-extras/select-seats/
- Breeze bags: https://www.flybreeze.com/support?a=Add-or-Change-a-Bag---id--j_f8IfxkR5iipZi3ZN8UcQ
- Breeze seats: https://www.flybreeze.com/support?a=Add-or-Modify-Seats-on-an-Existing-Reservation---id--tyEPBgv5SnKpOyGspJOHOw

## Derived ADRs, not alleged vendor rules

1. **Source selection references** are carried from Ancillary to Ordering; customer cannot supply quantity, price, tax, POS, traveler/flight or supplier metadata during final acceptance. (AeroTech design; vendor pattern compatible.)
2. **Operational completeness is separate from accepted commercial total.** No success label indicating held, booked, ticketed, paid or delivered merely because Order Item exists.
3. **A09 cannot ride the A07/A08 seat path.** ExtraSeat may consume another seat/ETKT and airline-specific accounting; its future operational model needs explicit FlightFlow and ticketing proof.
4. **Payment orchestration** stays external as Master mandates. A real carrier's channel may require payment/credit authorization before committing fulfillment; this is an airline deployment policy, not a domain-intrinsic JetPay dependency.
5. **No `pricedForTravelAgencyId`, `pricedForSurface` or similar stopgap fields.** Price/binding evidence belongs to Ancillary's general quote and POS, and existing authorization remains in place.


---

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


---

# 04 — Exact Create/Add contracts, channel adaptation and concurrency

## 1. Single internal application command, two entry points

```
Client -> Ancillary offer/order shopping, price, and SeatMap selection
      -> Ordering Create Order OR AddAncillaries
      -> source resolver/seat acceptance validator (read-only)
      -> one typed source normalization and domain acceptance kernel
      -> same Order PriceLines/Allocations/Items/Services + OrderChange
      -> existing command SaveChanges and query synchronization
```

Domain MUST NOT call Ancillary HTTP or know its REST DTOs. A small Application/Providers ACL produces an `AcceptedAncillaryBasket` with complete immutable typed selections. **No new API per variant, no hidden pricedForAgency/Surface fields.**

## 2. Internal canonical selected basket — exact shape

```json
{
  "selectionId": "s_source_general_selection_or_null",
  "seatSelectionIds": ["source_seat_selection_1"]
}
```

At least one nonempty part required when basket supplied. `selectionId` is a **single priced basket**, not a list of arbitrary OfferItemIds; `seatSelectionIds` is **distinct per traveller×flight**. No caller-supplied selected quantities, price, VAT, status, flight or passenger/agency provenance. Actual product selection and quantity are in Ancillary's trusted Price/Resolve.

## 3. Backoffice: preserve current contract + additive property

Create: existing `POST /Backoffice/v1/Orders/FlightOffers`, unchanged required `offerId/customerId/contact/travelers/seatSelections/remarks`, optional `selectedAncillaries` canonical property. **Do not add a new `idempotencyKey`.** Existing legacy `seatSelections` and new `seatSelectionIds` must not both be nonempty (typed 422).

Add: `POST /Backoffice/v1/Orders/{orderId:long}/Ancillaries`:

```json
{
  "expectedCommercialVersion": 1,
  "selectedAncillaries": {
    "selectionId": "s_source_general_selection",
    "seatSelectionIds": []
  }
}
```

Result uses the existing envelope convention, exact DTO (no duplicate save receipts):

```json
{
  "data": {
    "orderId": 12345,
    "orderChangeId": 98765,
    "commercialVersion": 2,
    "currencyId": 70,
    "customerTotal": 25000000,
    "addedOrderItemIds": [10,11],
    "addedOrderServiceIds": [20,21]
  }
}
```

`OrderId` values above are illustrative. The return values are generated by Ordering, not from opaque selection ids.

## 4. OTA (`Api`) / OtaPanel: external `services[]` wire adapter (Q5 resolved)

**Standing owner convention:** external surfaces should resemble the Amadeus/IATA OfferItem→Order paradigm; the internal `selectedAncillaries` is NOT imposed as an identical public contract. Keep the **existing Create fields and original response** so current clients are not broken. Add an optional external `services[]` collection whose items are only source selection references, with no embedded price or internal IDs:

```json
{
  "offerId": "existing-offer-id",
  "contact": { "...": "existing fields retained" },
  "travelers": [],
  "services": [
    { "type": "ancillary-selection", "id": "s_source_general_basket" },
    { "type": "seat-selection", "id": "seat-source-id-1" }
  ]
}
```

This is explicitly **Amadeus/IATA-inspired**, NOT a claim that it is an official Amadeus schema. The exact `type` strings above are AeroTech's new public facade vocabulary, mapped one-to-one to the canonical basket; **one** general `ancillary-selection` per request, 0..N seat selections; duplicates/unknown type rejected. Existing legacy seatSelections remains accepted for backward compatibility, never mixed with new seat entries.

Create paths:
- `POST /Api/v1/Bookings/FlightOffers`
- `POST /OtaPanel/v1/Bookings/FlightOffers`

Add paths:
- `POST /Api/v1/Bookings/{orderId:long}/Ancillaries`
- `POST /OtaPanel/v1/Bookings/{orderId:long}/Ancillaries`

Add request for external surfaces:

```json
{
  "expectedCommercialVersion": 1,
  "services": [{ "type": "ancillary-selection", "id": "s_order_priced_basket" }]
}
```

Use existing platform `data` envelope and response versioning conventions. Existing Create result stays exactly as implemented; Add returns a compact `data` with `orderId`, `orderChangeId`, `commercialVersion`, `currencyId`, `customerTotal`, and `services[]` of accepted `orderServiceId, serviceType, variantCode` if the chosen external facade exposes it; map from canonical result and do NOT expose definitionVersionId/provisionId/pointOfSaleId/agent/authorization fields publicly. If the local repository has an existing Amadeus-shaped service reference DTO, reuse it instead of introducing duplicate `services[]` records. Run API-shape snapshot tests for Backoffice, Api, OtaPanel. Preserve the current API auth/owner checks; do not add per-surface quote fields.

## 5. Create flow, step by step

1. Request validation: old Create shape; optional basket; duplicate seat mechanism rejected; existing authorization and customer extraction.
2. `IOfferProvider.GetByOfferIdAsync` loads matching real AirOffer; no flight/air-pricing modifications.
3. Read-only Ancillary general Resolve; verify **sourceKind=Offer, sourceOfferId == args.OfferId, not-expired and total resolved count** as defined in AC-01; otherwise abort before creating any Order.
4. Seat quote AC-03, bound to **same OfferId**, per traveller/flight, with one final complete price and POS. No Seat Hold here.
5. Create Order Air itinerary/travellers/fare topology using existing source. Shared acceptance kernel maps the basket into newly created canonical Order service identities **using the same Create Change**. Price normalizer sums all components. `ReconcileTotals` and `OrderCreated` run **once** after all lines.
6. Existing command persistence + projection ordering, subject to actual two-context semantics in doc 09. A failure before command save leaves **zero** Order rows.
7. Do not add special client key/receipt to new Create path. The existing global Create duplicate protection remains a separate **production blocker**. Never claim universal exactly-once until solved once for *all* Create paths.

## 6. Add flow, step by step

1. Acquire existing `IReservationLock.AcquireAsync(orderId)` (same lock all reserve/cancel/issue use); `IOrderAuthorization.Ensure(order)` per surface.
2. Verify `Order.Status == Created`, `expectedCommercialVersion == Order.CommercialVersion`, no historical `FulfillmentReservation`, no started/pending FulfillmentTask, no ETKT/EMD, applicable `LastTicketingDate` not elapsed; prohibit Add when eligibility cannot be proven. Local `AncillaryContext.acceptsChanges` must use the same predicate; it is advisory for shopping, the write validates again.
3. Resolve selection **sourceKind=Order, orderId==path, orderCommercialVersion==locked version, unexpired, expected line count**; separately validate AC-03 seat quote source/order/version; no stale cross-Order quote acceptance.
4. Validate active traveler and exact **source scope/coverage** (0..N), source definition/version/selected choices; reject duplicates in the request and conflicts with currently active order services/seat assignments, no implicit replacement.
5. Normalize Price and insert **one** new `OrderChange(AddProduct)` with `CommercialVersion=old+1`; then one Item/Service for each resolved priced occurrence and corresponding PricingLines/Allocations. All or nothing, **no external Hold** in Add.
6. Commit command-side, then follow existing query projection semantics. If query projection fails after command save, the client receives an ambiguous result; retry with old version gets 409, then must reload/repair canonical Order projection. Do not pretend a distributed transaction exists.
7. A repeated Add using the old expected version returns **409**; GET Order and reconcile. No new `AncillaryAcceptanceReceipts` or client key.

## 7. Add eligibility matrix

| State/history | Allow Add? |
|---|---|
| Created, no historic reservation/task/document, version current, deadline valid | YES (if complete source quote) |
| Created but reservation ever started (even released/expired) | NO, no hidden "fresh" assumption |
| Confirmed / ReserveFailed / ReservationUnconfirmed | NO, until dedicated Stage6B servicing |
| Ticketed / Paid / payment or issuance unconfirmed | NO for this early-Add route |
| Cancelled / Expired / Refunded / unresolved Cancel | NO |
| Unknown future state | NO by default |

## 8. Error contract and concrete code register

New codes are an **append-only proposed allocation** against checked remote `ExceptionFactory` (max used 2822). The owner's review reserves **2824 onward**; coding agent must scan the actual *local* source before adding these exact codes; collision => `ERROR_CODE_COLLISION` and report, not arbitrary reassignment.

| Code | HTTP | Meaning |
|---|---:|---|
| 2824 | 409 | `ANCILLARY_COMMERCIAL_CHANGE_NOT_ALLOWED` (reserve history/version/state conflict) |
| 2825 | 409 | `ANCILLARY_SOURCE_VERSION_MISMATCH` |
| 2826 | 409 | `ANCILLARY_SELECTION_EXPIRED` (same conflict style as source; do not introduce first 410) |
| 2827 | 422 | `ANCILLARY_SOURCE_MISMATCH` (Offer/Order/identity/count mismatch) |
| 2828 | 422 | `ANCILLARY_PRICE_INCOMPLETE_OR_INCONSISTENT` |
| 2829 | 422 | `ANCILLARY_VARIANT_OR_SELECTION_INCOMPLETE` |
| 2830 | 409 | `ANCILLARY_SELECTION_CONFLICT` (duplicate/seat/product) |
| 2831 | 409 | `ANCILLARY_OPERATION_NOT_READY` (Reserve/Issue gate, **before provider call**) |
| 2832 | 409 | `ANCILLARY_PARTIAL_CANCEL_REQUIRES_REQUOTE` |
| 2833 | 422 | `ANCILLARY_COVERAGE_INVALID` |
| 2834 | 422 | `ANCILLARY_SEAT_QUOTE_AMBIGUOUS` |

Existing `OrderNotFound/OrderDoesNotBelongToCaller` and upstream `ProviderRequestFailed` (502) must be reused where appropriate. **HTTP 502** for unavailable Ancillary upstream (not 503); 400 for purely syntactic input; 403 or existing 404 privacy policy for auth; no random domain 500 on missing configured provider, no fabricated HTTP 410. Source errors must be translated at ACL boundary into stable Ordering exceptions.

## 9. Explicit limits and wire safety

- General resolved basket max = exact Ancillary Price/SelectionSnapshot limit, **not an arbitrary extra Ordering limit**. Seat IDs bounded by verified product API; no unverified magic 50 cap. Validate against source constraints and make a test.
- JSON numeric money `decimal` and IDs `long`; no JS float conversion, no guessing names from `Id` fields. Match repository's current serializer and `EnumValueDto` adapter.
- Selection IDs are source correlation, NOT security tokens. Existing authorization remains; all quoted content re-resolved read-only using actual source.
- No new channel-specific `pricedFor*` field. `PointOfSaleId` inside a validated **seat quote** is commercial source evidence, separate from caller role.


---

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
| Journey | PerTraveller | one traveller | source-defined journey-wide flight set; resolve `AC-05` if otherwise ambiguous; do NOT guess every flight |
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


---

# 06 — A07/A08 commercial SeatMap and actual POS provenance

## 1. The root flaw in the legacy path

Current `Order.Create.BuildSeatServices` uses `(TravellerIndex, BoundId, SeatNumber)` and creates the **same seat on every flight** in the Bound; it attaches each new Seat Service to an existing AirFare `OrderItem` and does not accept independent seat price. Actual Ancillary `SeatMapSelectionId.Encode` = base64url(`travellerRef|flightId|pricingId|seatNumber`); current `SeatMaps/Resolve` loads **active pricing definition/rates** but not a unique selected, trusted final price/expiry for a given source context. **Neither** constitutes a commercial charge authorization.

## 2. Canonical Seat commercial invariants

- A07/A08 -> exactly **one** traveller × actual flight ID × seat number × verified rate/POS quote. Same traveller flight cannot have two active seats. Two travellers cannot own the same physical seat for that flight inside one Order.
- Same `OrderSeatService` must be both the commercial purchase and the physical seat assignment reference. DO NOT create `OrderAncillaryService` shadow or treat A09 ExtraSeat as a normal seat.
- New paid/free accepted seat has `OrderItem.Kind=Seat`, independent `AcceptedTotal`, correct Base/Tax/Fee, `SourceSeatSelectionId`, Variant A07/A08, Definition/Provision/Pricing, rate version, `PointOfSaleId` from **the selected authoritative source quote**.
- `FulfillmentProviderKey=FlightFlow` remains for seat physical reserve. Ancillary definition/provision and commercial hold semantics stay independent; no unverified assumption that its Seat Hold reduced aircraft occupancy.
- Legacy `SeatSelections` remains supported **only** in the legacy Create route, with historic semantics untouched for compatibility, while new source SeatMap uses the per-flight path. A mixed nonempty old/new seat input is a typed error.

## 3. Required **AC-03** Ancillary read-only selected quote contract

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


---

# 07 — Mandatory operational closure: Reserve, Confirm, Cancel, Issue, EMD

**Owner instruction 2026-10-28:** Reservation and Issue may be included where required to make an airline-safe system. We do **not** pretend they already work. Stage6A code prevents inconsistent behavior immediately; Stage6B makes real active product flows operational and is **required before production release** of that product. Both phases are parts of this **one** final Pack, not an indefinite unplanned follow-up.

## 1. Proven B1 failures on current Ordering source

1. `ReserveService.RequiredServices(order) => order.Services.Where(_providers.RequiresReservation)`; `RequiresReservation` invokes `CapabilityOf` and `Resolve(service.FulfillmentProviderKey)`. No `Ancillary` provider => `NoFulfillmentAdapterRegistered` (2505, HTTP 500), *not* a controlled retail operation error. See current `ReservationProviderResolver.cs` and `ReserveService.cs`.
2. `Order.CancellationScopeOf` includes only Air, dependent Infant Air, and associated Seat. A newly accepted ancillary is **not** canceled by `Cancel(order,[])` and Air cancellation leaves dependent ancillary Active.
3. `IssueOrderService` and `IssuancePlanner` issue ETKT for Air. `ElectronicMiscDocument` has Master schema and message placeholders, **not a proved working EMD issuance pipeline**. Marking the Air Ticketed cannot mean the optional service is documented or fulfilled.
4. `FlightFlowReservationProvider.PlanUnits` groups Air + `OrderSeatService` per passenger/flight into a single physical unit. It already can carry a Seat number, but **an accepted Seat's source POS/price must be preserved**, and double-physical hold must be avoided.

## 2. Stage6A mandatory minimal safety behavior (implement with commercial acceptance)

### R1. Reserve early gate — both whole order and targeted reserve

**Before** calling `_providers.RequiresReservation`, `_providers.CapabilityOf`, `_providers.Resolve`, reading an external provider, or scheduling a FulfillmentTask:

- Determine the **active** services included in the *requested* reserve scope **and their required dependency closure** (Air, Seat and ancillary that must be fulfilled with Air). For whole-Order Reserve, scope is **all active required Order Services**.
- If any purchased general Ancillary **or purchased Seat lacking verified commercial/physical fulfillment** has no **verified** registered provider/capability with actual downstream implementation, raise `ANCILLARY_OPERATION_NOT_READY`, HTTP 409 / proposed code 2831; **zero provider calls, zero reservations, zero tasks, zero changes**.
- Do **not** register a fictitious provider `ReservationMode.None` or silently omit Ancillary from the plan to let Air confirm with purchased but unfulfilled services.
- If a known product is truly `BookingRequired=false`, distinguish that from `fulfillment complete`: a source-approved no-booking/no-document product may not need a provider, but that must be a **verified explicit product rule**, not inferred because an adapter is missing.
- Add a **guard also to Confirm and Issue**: cannot call Confirm/Issue for a basket with unresolved mandatory ancillary fulfillment. **A07/A08 purchased Seat must be part of this gate even though its ProviderKey=FlightFlow is registered**: that provider can reserve the *physical* seat, but does not prove Ancillary commercial Seat Hold/document fulfillment. If source explicitly proves no additional commercial Hold or DocumentRequired, FlightFlow confirmation plus accepted source price can satisfy that seat; otherwise block until the full paired strategy is verified. Read-only quote is not itself a reservation.
- For **targeted** Reserve of Air only when dependent active Ancillary exists, either include the complete dependency closure in plan or reject typed, never bypass via requestedServiceIds subset. Scope rules are shared with full Reserve.

### R2. Cancel correctness now, before full reservation support

- `Cancel(order,[])` includes **every active** Air, Seat, purchased baggage and all other Ancillary services. If there are zero reservations, the command can cancel them all within existing Order/Snapshot code; no provider calls.
- Partial cancel of Air includes its dependent A07/A08 Seats; also includes dependent Ancillary where its **entire** source-backed coverage is contained within the ending AirServices. A single ancillary spanning two flights must not be left Active when one covered flight is removed.
- Where a multi-flight Ancillary overlaps *some but not all* ending AirServices, or is Order-wide/Traveller-wide with ambiguous dependency, **fail closed** with `ANCILLARY_PARTIAL_CANCEL_REQUIRES_REQUOTE` (409) until a sourced servicing/pricing decision exists. Do not arbitrarily cascade whole-product cancellation or prorate a booking-wide fee.
- Pure ancillary-only cancellation before reservation is permitted by **scope rules** if the product can be ended independently; legacy Seat-only cancellation remains explicitly blocked until a validated seat release/replace route exists. Do not expand this into post-confirmed refund/exchange by accident.
- If an affected service has held/confirmed capacity or issued document, current planner must settle/restrict it using verified provider capabilities; do not change commercial state until settlement. Unknown provider effect => no premature cancellation.

### R3. What happens to historical accepted price after Cancel?

The v1.1 review claimed Stage5 has a Pricing reversal rule; **current `Order.Cancel` does not produce reversal lines or change `CustomerTotal`**. Under Master history semantics, the original accepted PricingLines are immutable **and remain part of historical accepted total**. Therefore:

1. **Mandatory**: after whole-order cancel, zero active Services/Items remain, and `OrderStatus.Cancelled` is accurate; historical prices, changes and source references remain readable.
2. **Never** represent `CustomerTotal` as *current refund amount* or *amount due* after Cancel; it is the last accepted sale total under current implementation. UI/consumer must distinguish it from payment/credit/debt.
3. Do **not** append fabricated negative Tax/Fee/Base lines during Stage6A cancellation; a future authoritative `Refund/Reshop/Settlement` policy creates reversal/credit with reason/source references. If the product must refund automatically on Cancel, that product is **NOT live-sellable** until refund policy/JetPay/EMD behavior is available.
4. Test cancellation with no reservation, cancellation with incomplete provider result, and a flight-pricing atom fractured by partial cancellation (Stage5 issue still blocks unsafe ticketing).

## 3. Stage6B — real `Ancillary` reservation provider, minimal adapter design

**Implement only after verifying actual Hold API semantics per product and reliable operation/readback.** Use existing `IReservationProvider`, `FulfillmentReservation`, `ReservationUnit`, `FulfillmentTask` and lock. No new Saga/aggregate. **One Order Service may map to one provider unit, but preserve supplier grouping** if the source legitimately treats several units atomically. Do not infer operation-level atomicity from 201 response alone.

### Source endpoints verified at Ancillary `190aa74`

| Operation | Actual Service endpoint | Relevant behavior to verify in integration test |
|---|---|---|
| General Hold | `POST /Service/v1/Ancillaries/Hold` | Accepts Baggages, Meals, AssistedTravel, AirportServices, PriorityServices, Connectivity, Upgrades; **NOT Pets/ExtraSeats** in input |
| General Hold readback | `GET /Service/v1/Ancillaries/Hold/{holdId}` | Status/unit identities and recovery after timeout |
| General Confirm | `POST /Service/v1/Ancillaries/Hold/{holdId}/Confirmations` | confirmed vs Unknown; replay semantics |
| General Release | `DELETE /Service/v1/Ancillaries/Hold/{holdId}` with optional `unitIds` query | scope; partial release needs actual source proof |
| General CancelConfirmed | `POST /Service/v1/Ancillaries/Hold/{holdId}/Cancellations` with `reasonCode`, `unitIds` | no partial cancel without proven unit matching |
| Seat commercial Hold | `POST /Service/v1/Ancillaries/SeatMaps/Holds` | requires `PointOfSaleId`, OrderCommercialVersion and exact seat id; **not automatically a physical seat** |
| Flight physical Hold | FlightFlow existing `IFlightFlowProvider` / `FlightFlowReservationProvider` | actual seat+air capacity owner; `PlanUnits` currently groups Air+Seat |

### Provider invariants

1. Set `FulfillmentProviderKeys.Ancillary="Ancillary"` for general purchased services (not supplier's arbitrary providerKey). **Only register the real adapter** after capability semantics are verified; before then early guard applies.
2. `PlanUnits` must use stable `OrderServiceId`, exact accepted `VariantCode`, `ProvisionId`, quantity, coverage, typed selected option snapshot. **Never** call Price again during Hold or use mutable catalog definition as sold truth. Source's Hold may independently verify provision validity — a refusal is business failure, not silent repricing.
3. `Hold` is independent of Ordering from the Ancillary side; Ordering orchestrates it, no HTTP cycle. Send requested expiry from actual commercial ticketing/hold rules, not a hardcoded 30/90-second TTL.
4. Operation idempotency from **FulfillmentTask**/reservation intent, not client Add key. Persist provider request and correlation before invoking non-retriable effects, following Stage2/3 pattern.
5. Timeout/HTTP disconnect => `Unknown`, readback by `HoldId` or stable reference if source allows; do NOT blind-retry with changed idempotency key. Confirm/Release/Cancel replay behavior must be source-tested.
6. Some variant demands manual approval, external quote, unavailable stock or unsupported source Hold route. **Do not** call that product `HoldThenConfirm` capability; gate sale or expose a real alternative provider only when implemented.
7. A07/A08: physical FlightFlow hold and ancillary commercial quote must reference **one** seat occurrence. Prove any Ancillary Seat Holds complement, not duplicate, FlightFlow physical inventory; never fabricate a second held seat. `PointOfSaleId` from accepted seat quote survives to hold.
8. For multiple suppliers, operation can succeed partially unless provider explicitly guarantees all-or-none. Existing `IReservationProvider` `BatchResultMode` must match **observed** behavior; plan/release/recovery per unit and test mixed outcomes.

## 4. Stage6B — Issuance and document behavior (not optional for live EMD-required sale)

1. **Air ETKT:** current IssuancePlanner issues ETKT for `OrderAirTransportService`; keep it. Before Issue, verify every active necessary Ancillary has confirmed required fulfillment or an explicitly approved no-booking service contract. Never mark a whole composed purchase "fully fulfilled" from an Air Ticketed status.
2. **EMD-A:** only after corresponding ETKT Coupon exists and association is valid. `ElectronicMiscDocument` + `EmdCoupon` + `EmdPriceLink` + `EmdAssociationHistory` per Master §17; one correct RFIC/RFISC from actual source. Do not infer them from A-code.
3. **EMD-S:** standalone where source `DocumentRouting/DocumentType` says so; no invented ticket coupon association. `NoAncillaryDocument`: do not issue one. `TicketOrExchange`: may require ETKT/change and cannot be disguised as EMD.
4. For source states `DocumentRequired=true` and missing EMD issuer, **sell capability disabled**. A zero-priced Wheelchair/Meal may legitimately need SSR/provision confirmation but not an EMD: require its actual booking/delivery outcome, not blanket "no charge, nothing to do".
5. Payment authorization remains a **channel orchestration/policy** responsibility under Master payment-neutral core. Do not issue unpaid traffic documents from unvalidated actual sales flow. DocumentStock allocates local document numbers only for supported stock type; never invent/simulate EMD number or coupon.
6. Flight+Ancillary with unfulfilled required item must not be reported as completely fulfilled; expose per-service outstanding fulfillment/document facts. Root `OrderStatus.Ticketed` means Air ETKT if that is the agreed summary, **not** that every EMD has been issued. Guard API consumer claims accordingly.
7. **Unknown issuance** and externally issued EMD require provider readback/operation idempotency; no fake success or double issuance. Capacity and document operations are independently tracked.

## 5. Mandatory live proof scenarios before airline rollout

- Air + two regular seats + purchased baggage: Create, priced acceptance, FlightFlow physical Hold, Ancillary baggage Hold, both Confirm, ETKT Issue, EMD-A for baggage when required, positive association, restart/readback, OrderView matches.
- Free SSR meal and wheelchair: accepted zero source price, proper booking/confirmation without invented EMD; verify SSR/service outcome.
- Airport Lounge/CIP depending on document type: explicit provider Hold or immediate confirm, EMD-S if required, release and cancel from actual provider.
- Partial reservation timeout: one side Confirmed and other Unknown => root **not** fully confirmed, recovery/release and no unchecked Issue.
- Cancel all before reserve: all active Order Services cancelled, no orphan; source sale price historical and not misrepresented as refunded.
- Cancel after Hold: positive provider release evidence; failure remains unresolved, not commercial Cancelled.
- Multi-flight baggage: a partial itinerary cancellation fails pending service re-quote; never retains false coverage or charges only half by guess.
- A09 extra seat, A13/A14 pets, A17 medical, A19 UMNR: **never live** until actual provider/approval/document support and typed quote succeed, regardless of Domain tests.

## 6. Release taxonomy

`6A_DOMAIN_ONLY`, `6A_HTTP_COMMERCIALLY_ACCEPTED_BUT_OPERATION_BLOCKED`, `6B_PROVIDER_VERIFIED`, `6B_DOCUMENT_VERIFIED`, `AIRLINE_PILOT_READY` (last requires end-to-end tests, financial policy and operations approval). A Domain PASS without provider never equals airline pilot readiness.


---

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


---

# 09 — All 24 Ancillary variants, without silent exclusions or fabricated capabilities

**Verified:** `AncillaryVariant.cs` at `190aa74` enumerates **exactly A01…A24** under **nine profiles**: Baggage(6), Seat(3), Upgrade(1), Meal(2), Pet(2), AssistedTravel(5), AirportService(3), Priority(1), Connectivity(1). `travelInsurance[]` is presently a reserved empty array and **is not** one of these 24.

## 1. Two independent statuses per variant

- **D = Domain complete**: exact source-selected choices and source-fixed definition facts can be represented in typed Order VOs, mapped to price, saved, reloaded, and projected, even for a **trusted complete contract fixture** when current live source cannot emit it.
- **S = Source live**: actual current `AncillaryOffers / Price / Resolve` (or Seat price validator) returns all necessary source-proven data, complete monetary price and correct source identity. `D=PASS` never means `S=PASS`.
- **O = Operational live**: actual Ancillary or FlightFlow provider supports Hold/Confirm/Cancel/Issue/SSR in the required way. A commercialized product **cannot be airline-live** unless the required O and document/payments are verified.

State `SOURCE-GATED`/`OPERATION-GATED` is explicit, never silently converted to a test PASS or faked with a zero rate/unknown supplier. Do NOT hard-code one Scope per A-code: `Provision.CoverageScope` decides Sector, Portion, Journey or Order independently of a variant name.

## 2. Full A01…A24 registry-to-Order crosswalk

| Code / source Profile | Immutable selected data + sourced fixed facts (nothing invented) | Occurrence | Live constraint at 190aa74 |
|---|---|---|---|
| **A01 ExtraCheckedBag / Baggage** | `quantity:int`; `chargeKind=ExtraPiece`, `allowanceConcept`, `maxKgPerPiece?`, package/allowance definition if actually supplied | `OrderBaggageService`, type Ancillary, kind Baggage | Source complete quote; count ≠ necessarily pieces unless Unit says so |
| **A02 ExtraWeightPackage / Baggage** | quantity, fixed `packageProductRef = ServiceDefinitionRef`, `packageWeightKg?`, allowance concept; don't invent weight | same | Actual product identity + source rate; no fake derived package |
| **A03 Overweight / Baggage** | `bagRef:string`, `measuredWeightKg:decimal>0`, conditional quantity; charge combination and weight interval | same | `PerKilogram` needs full applied total; incomplete unit quote blocked |
| **A04 Oversize / Baggage** | `dimensions{lengthCm,widthCm,heightCm}`; source maxSize/linear sum | same | Exact dimension rule, not universal airline 158cm |
| **A05 CabinBag / Baggage** | quantity, actual chargeKind/cabin allowance limits | same | Purchased cabin bag distinct from included Air allowance |
| **A06 SpecialEquipment / Baggage** | fixed equipmentKind; optional/required dimensions and weight per definition; actual limits | same | No arbitrary caller equipment code; complete source rules |
| **A07 StandardSeat / Seat** | seat source ID, real traveller ref, flight ID, seat number, A07 definition/provision, **one final rate**, POS, qty=1 | `OrderSeatService`, type SeatAssignment, kind Seat | **AC-03 Source-gated**; raw SeatMaps/Resolve not a final quote |
| **A08 PreferredSeat / Seat** | same A07, retain A08 price/rule distinction and exact source-selected seat | same | **AC-03 Source-gated** |
| **A09 ExtraSeat / Seat** | purpose/seat number/extra occupied seat count/adjacency+ticket requirements **only when exact source supplies**; true beneficiary/flight | `OrderAncillaryService`, type ExtraSeat, kind Seat | **AC-06 Source-gated**: `extraSeats=[]` in current Resolve; source operational ExtraSeat Hold absent |
| **A10 CabinUpgrade / Upgrade** | fixed from/to cabin, `targetCabinId` when source-selected, eligible fare families, `requiresTicketExchange`, allowedUpgradeKind | `OrderAncillaryService`, type Ancillary | FixedAncillary valid complete rate only; **ExternalQuote/DynamicQuote gated**; no Air RBD mutation |
| **A11 FreeSpecialMeal / Meal** | fixed `mealCode`, meal kind, dietary? catering lead time; source quantity | type Meal, kind Meal | **Real zero-priced Rate** mandatory; name Free alone insufficient |
| **A12 PaidPreorderMeal / Meal** | fixed `menuItemRef`, quantity, meal kind, diet?, exclusive family, lead time | type Meal, kind Meal | Source menu identity and actual rate; no placeholder 0 price |
| **A13 PetInCabin / Pet** | animalType, otherAnimalCode?, animalAgeWeeks?, combinedWeightKg, carrierDimensions, document acknowledgements; source Cabin constraints | type Ancillary, kind Ancillary | Can be domain accepted only from genuine complete quote; **Pets absent from current General Hold**, supplier approval gated for live |
| **A14 PetInHold / Pet** | A13 plus sizeBracket? and source hold transport/size/weight limits | same | **General Hold absent**; supplier/stock approval gated |
| **A15 Wheelchair / AssistedTravel** | `assistanceSsrCode` selected; allowed SSRs/assistance level/lead time | type Ancillary | Rate may be 0 legitimately; SSR confirmed by operational provider required |
| **A16 DisabilityAssistance / AssistedTravel** | selected SSR; definition allowed SSRs/required communication method? | type Ancillary | Matching current typed assistance options when source offers; supplier rules remain authoritative |
| **A17 MedicalEquipment / AssistedTravel** | CustomerSelectionContract: EquipmentCode, EvidenceDocumentRefs? and for oxygen OxygenUnits?, OxygenFlowRateLitersPerMinute, FrequentTravellerMedicalCardNumber?; definition approval/equipment facts | type Ancillary | **AC-06 Source-gated**: current Price/Resolve lacks all required medical fields; hold needs supplier confirmation. Do not replace with an empty options object |
| **A18 Bassinet / AssistedTravel** | infantRef, guardianRef, source verified parent/infant relation, max age/weight and compatible seat groups | type Ancillary | Genuine priced choice only; manual approval/physical allocation gated until proved |
| **A19 UnaccompaniedMinor / AssistedTravel** | source `ChildRef`, exact **text** GuardianHandoffContact + GuardianPickupContact, age/connection limits | type Ancillary | **AC-06 Source-gated**: CustomerSelectionContract text vs actual Price/Resolve no text; never convert boolean flags to text |
| **A20 Lounge / AirportService** | airport/facility/terminal/direction/windows/timezone, selected `timeWithOffset?`/`guestCount?` | type LoungeAccess | Slot/facility policy source; source hold/EMD test before live |
| **A21 FastTrack / AirportService** | same airport facts, kind FastTrack, only actual conditional appointment/guest choice | type Ancillary | Availability/provider actual; not auto bookable due to listing |
| **A22 CipMeetAssist / AirportService** | A20 plus includedComponentCodes and policy guest count | type Cip | Multi-component service remains one purchased occurrence if one quoted item; source capacity/document gate |
| **A23 PriorityBoardingCheckin / Priority** | selected `optIn:true` when form requires; definition zone/group/fareBenefitRef/airports | type Priority | Missing/false opt-in rejected when mandatory, don't infer from Price token alone |
| **A24 OnboardWifi / Connectivity** | fixed planCode = ServiceDefinitionRef, plan kind, duration/data/devices, optional selected deviceCount | type WiFi | Actual deliveryStage/fulfillment required; don't invent eSIM/email delivery |

### Profile count check

A01–A06=6; A07–A09=3; A10=1; A11–A12=2; A13–A14=2; A15–A19=5; A20–A22=3; A23=1; A24=1. **Total 24.** Every enum in `AncillaryVariant.All` must map to exactly one row; if a new A-code appears, fail type-set/contract tests rather than discard it.

## 3. Critical difference among Authoring, Shopping, Price, Resolve and Hold

| Source | What it actually proves | What it does not prove |
|---|---|---|
| `AncillaryVariant` registry / `CustomerSelectionContract` | domain definition, name and authored **potential** fields, including A17/A19 | selection actually returned by active shopping or accepted priced rate |
| `TypedSelectionForm.Fields` | customer option inputs currently exposed for a subset of variants | all authored definition variants available in Price |
| `PriceAncillaryOffers.SelectionOptionsInput` | actually accepted current option families: Baggage/Pet/AssistedTravel/AirportService/Priority/Connectivity | A17 medical and A19 contacts if not in current DTO |
| General `ServiceResolvedSelectionsDto` | actual accepted general source data **for emitted rows** | A09 (currently empty), expiry/source header, final non-item fees without completeness flag |
| Seat `SeatMaps/Resolve` | definition/active rate list of selected seat ID | source-bound chosen one-price rate and POS |
| Ancillary `Hold` | real approved capacity for supported families | pet/ExtraSeat capacity, existing FlightFlow physical seat or ETKT/EMD |

## 4. Typed choice requirements and Source/SQL parity

Use **family-typed** snapshots as specified in doc 03, with a discriminated `VariantCode`. A01/A02/A05 count changes are service Quantity `int`, **not** created per-item child service rows. A03 measured weight `decimal` never int or rounded; A04 dimensions are three positive decimals; A13/A14 document codes and animal type choices survive reload; A17 source medical selected fields are **not stored as dummy nulls masquerading as completed source**; A19 real free text is never collapsed to boolean flags. A20–A22 DateTimeOffset retains original offset, no UTC-only loss of local appointment meaning. A07/A08 seat Rate/POS is source-required. A09 extra seat is distinct from ordinary seat number assignment.

**No giant DTO with 30 nullable unrelated fields; no 24 independent aggregate roots.** Persist typed family-specific semantically relevant fields and source-fixed details. Where current source is incomplete, write a **contract test with a complete trusted fixture** and mark external live case `BLOCKED_SOURCE`, but do not hard-wire an arbitrary extra SQL table/column name before an actual source type exists. Use `CustomerSelectionContract` for the intended field name/type and AC-06 for resolving the final wire.

## 5. Per-variant acceptance tests — 48 domain/SQL + 24 live-status tests

For every Axx implement **two** isolated test families `Axx_CREATE` and `Axx_ADD`; assert exact object type, profile/variant, quantity, passenger/scopes, source options, price/evidence, one Change, historical price and SQL command+read roundtrip. Additionally run `Axx_SOURCE_CONTRACT` against actual source to return **PASS**, **BLOCKED_SOURCE** or **BLOCKED_OPERATION**, not passing with a fixture.

Required variant-specific negative matrix:

- **A01/A02/A05** qty 0, unit mismatch, package ref not matching source; **A03** bagRef missing, negative/oversized weight, incomplete per-kg quote; **A04/A06** dimensions partial, invalid equipment kind/required weight absent.
- **A07/A08** invalid or replayed seat id, duplicate physical seat, wrong traveller, wrong flight, expired quote, multiple differing POS prices, source Rate mismatch; **A09** false normal Seat conversion or fake physical hold.
- **A10** DynamicQuote taken as Filed, mutation of Air's accepted RBD/cabin; **A11** zero invented from name; **A12** mutable menu item changed after quote; **A13/A14** disallowed species, dimensions or missing document acknowledgements.
- **A15/A16** invalid SSR; **A17** medical fields/evidence omitted; **A18** infant/guardian mismatch; **A19** guardian text silently discarded/converted to booleans.
- **A20/A21/A22** timezone offset lost, guest policy violation, mismatched facility; **A23** no opt-in when required; **A24** planCode/deviceCount mismatch.

All 24 must be structurally designed and domain-tested; only exact real source and provider proof enables an actual sellable route.


---

# 10 — Exact Ordering source changes, EF/CQRS migration, existing-source compatibility

## 0. Mandatory preflight: NOT source-blind edits

1. At start, `git status --short`, `git branch --show-current`, `git rev-parse HEAD`, `git log -5 --oneline`. The owner's 2026-10-28 review states **local `6a7bb71`** is unpublished and has **`GET /Service/v1/Orders/{orderId}/AncillaryContext`**, updated `FareType:AirFareType?`, Master `Quantity:int`, tests and migrations. **If this work exists, extend it; never re-add controller, rename it, duplicate query tables, overwrite migrations or revert the local HEAD.** In the remote source only `cd50a2a` was independently verified. If expected local work is absent, stop only that delta and report `LOCAL_BASELINE_MISMATCH`, do not pretend the prior pack's "create route" instruction is still valid.
2. Read the *current local* Master, `CLAUDE.md`, `.canonical-stage`, `canonical-stage5-v1.0.yaml`, Stage5 type-set tests, and current command/query migration HEAD. The source-specific mapping below is against `cd50a2a` unless noted.
3. Do not replace the local Master with the stale bundled reference from Pack v1.1. This pack **does not contain** a Master copy.
4. Run `git grep` for all listed classes and counterpart mappings, not only the source file listed, before changing a shared type. A `long?` in the abstract base impacts serializers, query Dto, aggregate methods and pre-existing reservation provider call sites. Keep air/seat-specific `long` required explicitly at subtype level.

## 1. Source-level change matrix (not optional)

| Existing file or family, confirmed remote | Minimal change to implement | Tests |
|---|---|---|
| `Domain/OrderAggregate/Entities/OrderService.cs` | Change beneficiary to `long?` with explicit PerOrder invariant; retain strict non-null for Air/Seat subclasses. Preserve TPT identity and historical rows. | Null PerOrder vs forbidden null pax service |
| `Entities/OrderAirTransportService.cs`, `Entities/OrderSeatService.cs` | Air invariants unchanged. Seat keeps associated AirService and one flight/one traveller. Add immutable accepted seat quote provenance and source variant; do **not** generate second seat service. | Legacy seat persisted, Paid seat separate item, no duplicate physical identity |
| `Domain/OrderAggregate/Entities/OrderItem.cs` | Keep as existing `ProductType` item, one item per priced selection unit. Do not create another aggregate or a generic optional-item table. | Kind, AcceptedTotal and cancelled item history |
| NEW `OrderAncillaryService` and `OrderBaggageService` under `Entities/` | Subtypes per Master §6.7/§6.8. `Quantity:int`, coverage 0..N, independent SelectionScope/CoverageScope, versioned definition/price provenance, typed family selected facts, no blank placeholder. `OrderBaggageService` only for A01–A06. | A01–A24 known code crosswalk, SQL roundtrip |
| NEW minimum family-typed selected VOs under `ValueObjects/` | Baggage, Pet, Assistance, Airport, Priority, Connectivity, Meal, Upgrade, ExtraSeat, Seat source snapshot; only fields proven by `CustomerSelectionContract` and source type-specific `details`. Avoid one nullable-everything class. When source wire is missing, gate real use rather than invent missing field type/column. | Each field survived exact roundtrip; mapping coverage |
| `Domain/OrderAggregate/Order.Create.cs` | Pre-resolve in application, create Air unchanged, map added services **under the existing Create change id**, before *one* `ReconcileTotals()` and *one* `RaiseCreated()`. Replace old `BoundId+SeatNumber` multi-flight behavior for new seat selection IDs. Keep **legacy** input backward-compatible (with conflict rejection if mixed). | Air-only Stage1 unchanged, 2-leg seat picks, 1 Create event/version |
| NEW `Domain/OrderAggregate/Order.AddAncillaries.cs` | One `OrderChange(AddProduct)`, version +1 once, add all items/services/pricing atomically command-side, reject partial basket/duplicate/conflict, never hold externally. Map trusted source facts into local IDs. | Add once, duplicate 409, exact totals, all-or-none |
| `Domain/OrderAggregate/Order.Pricing.cs` | Shared accepted ancillary `PricingLine` builder and reconciliation; included-tax/fee algorithm in doc05. `OrderPricingReason.AdditionalSale=9`, `OrderPricingLineSubCategory.AncillaryBase=6` append-only where actually needed. Do not use `Discount` or `BaggageAllowance` as purchase type. | 0 price, included VAT, fees, source sum and allocations |
| `Domain/OrderAggregate/Order.Cancellation.cs` | Whole cancel includes all active purchased services; air-dependent scope + multi-flight fail-closed rules in doc07; no imagined refund automatic credit. Existing order cancellation settlement remains gate for any started provider work. | All cancelled, cross-journey ambiguous reject |
| `Application/FulfillmentReservationAggregate/Commands/Reserve/ReserveService.cs` | Under existing Order lock **before `RequiresReservation`** check, reject unsupported purchased services **including paid Seat when FlightFlow physical hold is not sufficient to satisfy its source commercial Hold/document rules** (and targeted partial reserve when it can leave an inconsistent sold basket); use new typed domain exception 2831, not 2505/500. No dummy provider registration. | Reserve returns 409 before FlightFlow calls |
| `Application/OrderAggregate/Commands/IssueOrder/IssueOrderService.cs` | Early pre-provider/stock effect guard for active ancillary not having required fulfillment/document capability. Do not change Stage4 Air Ticket issuer or allocate ETKT/EMD based on guessed flags. | Issue rejected before DocumentStock allocation |
| `Domain/OrderAggregate/Order.Reservation.cs`, `Order.Issuance.cs` | Add only necessary derived predicates for operation readiness (active ancillary, required vs completed); don't add a second lifecycle state machine. Preserve existing Stage5 validations. | Created/reservable vs blocked; Stage5 regression |
| `Application/OrderAggregate/Commands/CreateOrderFromOffer/` | Extend `CreateOrderArgs`, input mapping, validators and three channel command/handler paths to accept normalized canonical basket. Reuse `ICreateOrderFromOfferService` and existing AirOfferProvider; no new create aggregate. | Three surface Create compatibility |
| `RestApi/V1/OrderAggregate/Controllers/{Backoffice,OtaPanel,Ota}Controller.cs` | Add `POST {orderId}/Ancillaries` to all three, keep Create routes; Backoffice internal basket, partner/OTA panel surface-specific `services[]` wire adapter as doc04. Keep existing auth. | OpenAPI snapshot, auth and no breaking old request |
| `Application/OrderAggregate/Commands/AddAncillaries/*` NEW | Existing lock, `expectedCommercialVersion`, source Resolve, Order status/history/doc eligibility, `Order.AddAncillaries` once, commit. **No client `idempotencyKey` or receipt DB.** | Repeated Add 409; concurrent version race; 502 outage |
| `Domain/OrderAggregate/Contracts/IOrderRepository.cs`, `Persistence/.../OrderRepository.cs` | Reuse `GetAsync` with aggregate includes; add only existing-pattern methods if proven necessary, no duplicate repository. Existing `.Include(order => order.Services)` loads TPT descendants; test after migration. | SQL TPT full graph reload |
| `Persistence/OrderAggregate/OrderServiceConfiguration.cs` | Preserve `UseTptMappingStrategy`; make `TravellerId` nullable at base, correct new subtype EF tables and coverage child relationships. No cascade deletion of accepted historical snapshots. | Fresh migration and upgrade migration |
| `Persistence/OrderAggregate/OrderSeatServiceConfiguration.cs` | Add seat sale snapshot fields and explicit `PointOfSaleId` if accepted seat quote requires it; nullable/required appropriate. **Existing seats remain valid** without quote. | Old Row load and new Paid Seat load |
| `Application/OrderAggregate/Projection/OrderReadModelSnapshotFactory.cs` | Currently matches only Air/Seat and throws for other services; add exhaustive `OrderBaggageService`/`OrderAncillaryService` mapping. No `NotSupportedException` for accepted A-code. | Every A-code command→read projection |
| `Domain/OrderAggregate/Contracts/OrderReadModelSnapshot.cs` + `Query/OrderAggregate/*` | Include variant, typed selected choices, selection/coverage and source accepted price provenance and 0..N flight links; for Base beneficiary nullable; all channel mappers should support it, external may intentionally omit internal IDs. | GET by Order, pagination unaffected |
| `Synchronizer/OrderAggregate/*`, Query EF configs and migrations | Preserve existing projection pattern, update both SQL schemas, test interrupted projection recovery/rebuild. **Never** state command+query saved by one transaction: `OrderingUnitOfWork.SaveChangesAsync` saves them sequentially. | Command-saved/query-failure repair test |
| `RestApi/V1/OrderAggregate/Controllers/ServiceController.cs` on **owner local HEAD** | **EXTEND existing** `AncillaryContext` route with purchased ancillary/seat facts, `acceptsChanges` from the same Add predicate. Do not add a duplicate controller. If local doesn't have it, report local mismatch. | 0 then >0 ancillary return, no 503 on repeat shopping |
| `Domain/_Shared/Resources/ExceptionFactory.cs` and messages | New typed 2824..2834 **after checking current local collisions**, upstream 502, 409 state/expiry, 422 invalid selection. No random 500. | Error code collision and REST negative |
| DI composition root and external ACL | Register only *read-only* Ancillary Resolve and Seat validated quote in 6A; **do not register fake `Ancillary` reservation provider**. Later real 6B provider may be added after actual hold conformance. | No false provider capability |

## 2. Source migration constraints

- **Command schema:** add TPT tables for OrderAncillaryServices, OrderBaggageServices, typed source selected fields, quantity, exact Coverage child set, seat quote evidence. `OrderService.TravellerId` nullable. Use existing naming/index conventions; database IDs generated by existing `IIdGenerator`. One `OrderItem` per sold unit and no duplicate store of whole generic response JSON.
- **Read schema:** add the matching projection/read model, new migrations tied to Query DbContext (not an invented unified DbContext). Backfill historical Seat rows as legacy unpriced assignments; never fabricate `pricingId/PointOfSaleId` or create Seat Item migrations for old rows. No in-place renaming of old `OrderAirTransportService` ownership.
- **Versioning:** source `ServiceDefinitionRef` and immutable `VariantCode` must survive a definition edit or deactivation; when `DefinitionVersionId` and PricingVersion are real source fields keep them. SQL must preserve accepted selected numeric/enum/date offsets, no roundtrip of `decimal` through float.
- **Child relation coverage:** store child mapping of `OrderAncillaryService` → 0..N **OrderAirTransportService IDs**, referenced only within same Order, enforce uniqueness and order membership in Domain; for `PerOrder` keep zero list when Source says no flight. Do not create one duplicate ancillary row per covered flight.
- **Status and lineage:** preserve `OrderChange` relationship, `OrderService.CreatedByChangeId`, accepted Price line allocation and `OrderItem.CreatedByChangeId`. History rows immutable; cancellation marks EndedByChangeId, no hard-delete.
- **Concurrency:** Add must lock per Order **before** resolve final write and recheck CommercialVersion; EF row version/lock remains existing pattern. Two concurrent Add operations with same old version cannot both succeed.
- **Query atomicity:** command-side Order+Items+Services+Lines/Changes persist in one command context save; the query context save occurs **after** command save. Implement a tested re-projection/reload/read fallback using existing code (avoid new distributed transaction/outbox merely for 6A). If client sees an error after successful command save, `expectedCommercialVersion` prevents duplicate repeat. Report this genuine ambiguity in test evidence.

## 3. CI guardrails and authority switching

The checked repo has `.canonical-stage` equal `5` (number). The Stage5 test explicitly tests `_stage==5`. In 6A add `canonical-stage6a-v1.0.yaml` as a **thin type-set/frozen-enum** manifest and `Stage6ATypeSetConformanceTests.cs` in the same `Domain.ConformanceTests` project. **Set `.canonical-stage` to `6` (numeric, not string `6a`)** when Stage6A tests and local Master change are ready; keep all Stage1–5 frozen numeric enum values and tests. Mirror the existing YAML parsing/conformance pattern exactly rather than inventing a new pipeline framework. The full source of enum/type mapping is the *current local* files, not an incomplete names-in-PRD guess. No new security types in manifest.

Update `CLAUDE.md` in the **same commit** that changes `.canonical-stage`: line149 Stage5 active authority -> Stage6A pack + Master; line166 explicitly allow owner-approved **repo-root `reports/`** deliverables; **preserve** line78 server-derived lock/no client servicing idempotency, rather than overwriting it. Keep framework/style rules and stage1–5 authority intact.

## 4. Output to `reports/` (always repo root)

- `reports/STAGE6A-SOURCE-DELTA.md`: local and remote SHA, file-by-file changes, preflight discrepancies.
- `reports/STAGE6A-DOMAIN-FIELD-PARITY.md`: each A01–A24, source accepted fields, SQL, DTO, test evidence, `Domain/Source/Operational` statuses.
- `reports/STAGE6A-INTEGRATION-EVIDENCE.md`: actual HTTP trace/status, version, fallback/error, command/query reconciliation, no unproven PASS.
- `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md`: AC-01…AC-06 handoff owners, open items, resolved Q1–Q9, production conditions.
- Do not commit docs in arbitrary `docs/reports` folders, and do not update any file in Ancillary repo from the Ordering agent.


---

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


---

# EXACT CODING AGENT PROMPT — AeroTech.Ordering.Final — Stage 6A/6B (v2.0)

## ROLE, SOURCE OF TRUTH, AND SCOPE

You are the implementation and conformance agent for `aliifarhadi/AeroTech.Ordering.Final`, local `k8s-stg`. The owner expects a real-airline PSS, **not merely a passing project**. Implement *exactly* the decisions in `Ordering-Ancillary-Operational-Integration-Pack-v2.0`, docs 00–14, and the **current local** `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`; do not use its stale prior bundled copy. Read `CLAUDE.md` and canonical guardrails. Never bypass existing Stage1–5 behavior or improve unrelated areas.

**Goal:** accept every actually complete, source-priced ancillary A01–A24 (including A07/A08 SeatMap) into `Order` at **Create** and **Add before reserve**, preserving exact purchased option, quantity, source, beneficiary/coverage, price, history, and typed output. Resolve actual source-contract gaps through a documented Ancillary handoff. Avoid operational dead ends via minimal **Reserve, Cancel, Confirm, Issue** protection immediately; implement real supplier provider/EMD in 6B only from verified source, not guessed.

## NON-NEGOTIABLE BASELINE CHECK (STOP IF CONTRADICTORY)

1. `git status --short; git branch --show-current; git rev-parse HEAD; git log -5 --oneline; git diff --stat`. **Do not reset or overwrite uncommitted owner changes.** This pack's independently observed Ordering remote `cd50a2a` is *not* its local Source of Truth. Owner's source review asserts local `k8s-stg@6a7bb71` is unpushed, already implementing `GET Service/v1/Orders/{orderId}/AncillaryContext`, command/query migrations, `FareType:AirFareType?`, and `OrderAncillaryService.Quantity:int` Master update. Confirm exact source and tests in the checkout. If absent, report `LOCAL_BASELINE_MISMATCH` and implement nothing that would conflict with it.
2. Read current Master §§6–9,13–14,17,27, Stage6; read `.canonical-stage`, `canonical-stage5-v1.0.yaml`, Stage5 type-set tests and latest migration snapshots. Read `src/.../Order.Create.cs`, `Order.Pricing.cs`, `Order.Cancellation.cs`, `Order.Reservation.cs`, `Order.Issuance.cs`, `ReserveService`, `CancelOrderService`, `IssueOrderService`, `FlightFlowReservationProvider`, `OrderReadModelSnapshotFactory`, three channel controllers and existing `AncillaryContext` tests. Read companion Ancillary `k8s-stg@190aa74`, and then PIN the actual latest sha before implementation. Never infer a remote change absent in pinned code.
3. Check `Framework/` and AirPrice/FlightFlow sister repository patterns *before* introducing any infrastructure. No new Saga, generic receipt table, duped pricing or Channel-dependent security layer.
4. Write `reports/STAGE6A-SOURCE-DELTA.md`: actual local SHA, source evidence, contradictions vs pack, source contract status AC-01…AC-07. If a contract is absent, record `BLOCKED_SOURCE` rather than fake it. If any pack item contradicts current owner Master or actual code, **do not guess**: preserve baseline and report exact conflict in `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md`; implement independent work only.

## FROZEN DECISIONS — DO NOT REOPEN DURING IMPLEMENTATION

- `OrderService.TravellerId:long?` solely to model true PerOrder (Air/Seat remain non-null); amend Master §6.4 visibly. `Quantity:int`, not decimal; `CoveredAirServiceIds:0..N`; `CoverageScope` distinct from `SelectionScope`. Store one occurrence per purchased priced unit, not one per flight or individual counted piece.
- A01–A06: `OrderBaggageService`; A07–A08: **existing `OrderSeatService`**, per traveller×flight, separate purchase Seat Item; A09: `OrderAncillaryService` **ExtraSeat**, not a normal seat; other A-codes family-typed `OrderAncillaryService`. Reuse `OrderServiceType` meaningful members; generic newly appended `Ancillary` for categories missing from existing enum. No 24 new enums/subtypes and no alias `BaggageAllowance` for purchased luggage.
- Order Create accepts an optional canonical `selectedAncillaries {selectionId?, seatSelectionIds?}` on Backoffice; Api/OtaPanel use an Amadeus/IATA-inspired external `services[]` adapter; retain old required Create and response fields. No duplicate Create service. Existing legacy `SeatSelections` cannot be combined with new seat IDs; retain existing legacy behavior for existing callers except clearly defective identity with dedicated tests.
- Add with `POST .../{orderId}/Ancillaries`: existing distributed Order lock, `expectedCommercialVersion`, one `OrderChangeType.AddProduct`, once per batch version bump, all-or-nothing command-side. NO new public `idempotencyKey`, NO acceptance-receipt table. Retry old version => 409 + re-read. Universal Create duplicate policy remains separately documented; not secretly implemented only for ancillary Create.
- Pricing: source complete, one currency or **actual** FX evidence, exact included-tax/non-item-fee treatment, invoice/settlement separation. `PricingLine` accepted source is immutable and allocated to Item+Service; no false refund/reversal on cancellation.
- **No upstream Hold during Create/Add.** Before real provider capability is verified, Order with outstanding ancillary **including paid Seat with unproven source commercial Hold/Document requirements** must be rejected by full Reserve and Issue before any provider/stock effect, typed 409 (not 2505/500); Confirm only against genuine reservations. Whole Cancel covers all active services, including purchased extras. Partial Air cancel with overlapping multi-flight ancillary requires actual requote/settlement or fails closed, not guess.
- `PointOfSaleId` of a seat originates from **one actual source-priced seat quote**, not reconstructed from arbitrary office/customer/security fields. Missing quote/POS or ambiguous rate => block paid seat acceptance. A17/19 fields are exactly source typed, no invented bool↔text conversion. A09 source `extraSeats[]` presently empty, not live.
- Ancillary sub-repository changes are **handoff only**. Do not edit its repository, and do not make fabricated DTOs on Ordering side as if it had already delivered AC-01…AC-07.
- Reports go only in repository-root `reports/`. Update current Master **in place**, `CLAUDE.md`, `.canonical-stage` numeric `6`, add `canonical-stage6a-v1.0.yaml` and `Stage6ATypeSetConformanceTests.cs` when activating Stage6A. Preserve earlier enum values and tests.

## IMPLEMENTATION ORDER AND EXIT REQUIREMENTS

### 6A.0 — safety and baseline, earliest commit

- Freeze local SHA and Master version; check existing `AncillaryContext` controller, tests and migrations. Verify `OrderingUnitOfWork.SaveChangesAsync` semantics (command DB then query DB, **not** atomic across two contexts).
- Introduce typed `AncillaryOperationNotReady` precondition **in `ReserveService` before `RequiredServices/RequestedServices` calls `ReservationProviderResolver.RequiresReservation`**. Ensure this also recognizes a newly purchased A07/A08 seat whose FlightFlow physical capability exists but whose *commercial ancillary Hold/DocumentRequired* capability is not proven; never treat a registered FlightFlow provider as blanket proof for a paid seat. Also pre-effect `IssueOrderService` guard for unfulfilled/undocumented active accepted ancillary. Keep old Air-only reserve, confirm, issue flows **green**.
- Fix whole-order `CancellationScopeOf` to include active non-Air/Seat services; dependency-aware partial-cancel rules, without automatic imagined monetary reversal. Do NOT mark cancelled if a known Hold/Document external effect remains unsettled. Tests: Created→general Add→Reserve rejects typed 409, **zero FlightFlow/Ancillary calls**; Created→Add→whole Cancel ends all active; Air-only existing Stage5 tests unchanged.
- If safety guards require the new subtype first, land together with the minimum domain materialization; no enabled public Add endpoint may reach a broken Reserve path.

**Exit:** `6A0_SAFETY_PASS` by unit + HTTP tests, or BLOCKED with exact evidence. This does not mean a single ancillary can yet be held.

### 6A.1 — core Domain and Add; AC-02+AC-04 unblock fixtures, AC-01+AC-05 additionally required for live

- Implement `OrderAncillaryService` / `OrderBaggageService` / extended `OrderSeatService`, typed family VOs and source field/coverage mapping per docs 03/05/09. `Quantity:int`, PerOrder nullable beneficiary, 0..N AirService coverage. Append only proven enum members.
- Implement actual source-supported **general** Add via existing Order locking pattern, current `OrderService` active/history checks, `OrderChange(AddProduct)`, accepted Pricing/Allocations exact and full persist/sync/Query mapping. Single domain acceptance kernel reused later by Create; don't copy mapping into three handlers.
- Extend existing local `GET Service/v1/Orders/{orderId}/AncillaryContext` to include active/history selection facts and the exact Add readiness predicate, **not a new route/controller**.
- AC-02 `IsUnitTotalComplete` and AC-04 nonempty AncillaryContext are first companion dependencies. **Live Add also needs AC-01 sourceKind=Order/orderId/version/expiry and AC-05 truthful coverage**; until then fixtures can pass but HTTP status is `BLOCKED_SOURCE`. Never accept opaque `selectionId` solely because Resolve returned some rows.
- Verify full SQL fresh and upgraded migrations, Include() TPT, DTO/read model, auth, batch failure atomicity, query projection recovery. Add all A01–A24 fixtures into Domain tests even if current upstream cannot sell A09/A17/A19.

**Exit:** `6A1_DOMAIN_PASS`; `6A1_HTTP_LIVE_PASS` only after pinned **AC-01 + AC-02 + AC-04 + AC-05** actual traces exist for a product/coverage.

### 6A.2 — Create with source-signed general selection, AC-01 + AC-05

- Extend existing Create on **all three surfaces** to accept a normalized optional priced basket. Air-only behavior unchanged; **one existing `OrderChange(Create)`, initial `CommercialVersion=1`, one `ReconcileTotals`, one `OrderCreated`.** No Create→Add chained mutation.
- Resolve read-only, validate `SourceKind=Offer`, exact `OfferId`, expiry, returned count = snapshot selected count, traveller/flight/Bound coverage, no duplicates and complete price; commit Order only after entire basket passes. Support multi-flight `Portion`/`Journey`/`Order` correctly; no guessed flight/age/Agency data.
- AC-01+AC-05 must be a live Ancillary source contract. Fail closed on source-missing fields, do not mark live pass from local fake response. Do not add ancillary-only Create idempotency table/policy.

**Exit:** Air-only regression plus `6A2_HTTP_LIVE_PASS` source-backed or `6A2_BLOCKED_SOURCE`.

### 6A.3 — A07/A08 SeatMap, AC-03 and POS

- Implement one truthful seat accepted quote per traveller×flight with source `PointOfSaleId`, one Rate/Price and exact seat number. Keep `OrderSeatService`; new purchased seats get standalone Seat Item, no duplicate `OrderAncillaryService`, don't mix legacy seat payload with new selection IDs.
- `SeatMaps/Resolve` currently doesn't prove a unique final rate or POS. Require actual `ValidateForAcceptance` **proposed** source Handoff; no chosen arbitrary rate, estimated/zero price, or fake seat inventory. **No physical Hold in 6A**; FlightFlow remains physical owner.
- Verify simultaneous two travellers/two flights, collision on same physical seat, pricing differences for traveller/POS, SQL & UI projection, source rejection.

**Exit:** `6A3_SEAT_LIVE_PASS` only with AC-03 actual source contract; otherwise Domain fixture-pass and source-blocked.

### 6A.4 — source-gated hard cases AC-06

- Verify exact `CustomerSelectionContract` fields for all 24 variants and Source typed Price/Resolve. A09 ExtraSeat, A17 Medical, A19 UMNR live are **not** claimed ready at current source. A13/A14 Pet may be accepted commercially with complete quote but are **not operationally sellable** without supplier provider. A10 dynamic upgrade is distinct from fixed upgrade and blocked until genuine Quote/Exchange exists.
- Extend the typed source path **only after source AC-06 landed**, retain immutable accepted typed selected data, positive/negative validation and SQL roundtrips. Do not invent medical fields into current wire, mock a price, or store guardian contact truth as boolean.

**Exit:** 24 Domain case matrix all verified; individual live cells PASS/BLOCKED transparently.

### 6B — real operational completion where needed for a product to be airline sellable

The owner's instruction explicitly **allows** Reserve/Issue if needed. For a product to be released for real airline sales, deliver the actual 6B capability or keep it BLOCKED (not silently Deferred and sold anyway):

- Validate and implement genuine Ancillary `IReservationProvider` adapter using `POST /Service/v1/Ancillaries/Hold`, `GET Hold/{id}`, Confirm/Release/Cancel, and actual provider capability per family. Test timeout/Unknown, readback, unit atomicity, refusal, expiry, partial unit cancellation. Use existing `FulfillmentReservation`, `FulfillmentTask` and provider idempotency—no Saga.
- For A07/A08 integrate FlightFlow physical seat hold in same flight unit and Ancillary commercial source as needed **without double physical inventory**. Ensure chosen POS/quote remains associated. Reject seat amendments to already Confirmed flight unless a verified FlightFlow servicing operation exists.
- `IssueOrderService`: ETKT still for Air, EMD-A coupon association only after ticket exists, EMD-S standalone only when source specifies, SSR/NoDoc services require their real fulfillment outcome; `TicketOrExchange` variants need actual Ticket/Exchange path, not fake EMD. Never issue fake EMD or mark Order fully serviced from Air Ticketed status alone. Explicit payment/settlement policy for live channel.
- End-to-end one realistic full Booking (Air+Seat+Baggage+Meal), Confirm, Issue, Cancel/release and recovery with live integrations. If supplier/EMD/Payment missing, stage verdict `6B_BLOCKED`, **not** production ready.

## REQUIRED TESTS AND DELIVERABLES

- Build solution 0 errors. Run source-conformance, Domain, persistence, fresh + upgrade migration, API/OpenAPI, full Stage1–5 regression, lifecycle sabotage, and real Ancillary HTTP integration tests for available contracts. **Do not cherry-pick positive tests**. Before running DB migrations snapshot and safeguard developer data according to repo conventions; never drop production tables.
- Tests per variant: exactly **A01…A24 × Create and Add** with expected representation, source fixed+typed selected data, money, beneficiary, `0..N` coverage, SQL reload, read DTO, negative; `PASS_DOMAIN` is distinct from `PASS_LIVE_QUOTE` and `PASS_RESERVE_ISSUE`.
- Concurrent Add and repeated retry old version (409); cross-offer/order/expired selection; included tax; non-item fee; wrong rate; no duplicate seat; two-leg baggage; per-order null beneficiary; unknown cancellation; provider timeout; query failure after command save; no changed old Air-only flows.
- Add tests of **whole cancellation after Add** and **Reserve/Issue early guard** at start of 6A. Tests for nonresumable provider effects must be **real**, not only a toy stub. Stage6B tests require actual provider readback trace.
- Source handoff tasks AC-01…AC-07 per doc11. Do not claim they landed if absent. Report precise actual source SHA.
- Reports exactly under `reports/`: `STAGE6A-SOURCE-DELTA.md`, `STAGE6A-DOMAIN-FIELD-PARITY.md`, `STAGE6A-INTEGRATION-EVIDENCE.md`, `STAGE6A-DECISIONS-AND-BLOCKERS.md`, and when implementing 6B, `STAGE6B-OPERATIONAL-CONFORMANCE.md`.
- Each report includes **files changed, migration IDs, SHA, executed test commands, pass/fail counts, exact service HTTP traces, remaining blockers, and verdict**. No unpushed source claimed source-verified by this pack. No code modified in other repos. Do not leave an apparently functional 6A endpoint enabled for a non-reservable/undocumentable product in airline production.

## ABSOLUTE BOUNDARIES

No unauthorized rewrites, no auto-approve supplier, no fake rates/EMD/POS, no invented RFIC/RFISC, no `pricedFor*`, no new web security framework, no new acceptance receipts, no client idempotency input, no missing AC waiver silently accepted, no 24 aggregate roots, no duplicated Seat physical stock. Never ignore local Master §6.4 amendment or pinned Ancillary typed contract. If a missing contract prevents a **live** test, stop just that live increment and mark `BLOCKED_SOURCE`; continue only independent Domain work and tests.

**Required final response to owner:** summary by increments with exact pass/block verdicts, source changes and branch SHA, all 24 per-variant matrix, list open cross-service AC items, and paths to every report. If source is incomplete, state it plainly; do not say "Stage closed".


---

# 13 — Master/CLAUDE/CI amendments: precise authority and field policy

**Patch the current LOCAL Master in place. Never replace it with the stale Master from v1.1.** Owner review reports its updated local SHA prefix `1e51a948`; this SHA is not independently accessible over GitHub. The coding agent must read that Master on the working machine, record its SHA, and keep any already-added `FareType: AirFareType?`, `Quantity:int` and AncillaryContext contract.

## A. Master v2.0 normative amendment text

### §6.1 and §6.4 — buyer/beneficiary/service grain

- An `OrderService` is one purchased commercial occurrence. Air transport and ordinary Seat require one traveller; **PerOrder optional services have the Order as beneficiary**, `TravellerId:long?` nullable in the abstract base, but `null` is legal only with `SelectionScope=PerOrder` and `CoverageScope=Order`. No fake passenger or `TravellerId=0`.
- A single sold source unit maps to one OrderItem + OrderService, even if it covers several flights. Quantity remains `int>0` in source and Order. Multiple Flight IDs are coverage links, not separate charged occurrences.
- Source `VariantCode`, broad `OrderServiceType`, `ProductType`, `SelectionScope` and `CoverageScope` have **different meanings** and remain separate. Amend the old passenger-only benchmark sentence and any “Exactly one beneficiary occurrence” blanket rule to explicitly include the Order-level exception.
- Source `FulfillmentProviderKey="Ancillary"` for general ancillary is a **future verified provider route**; before actual adapter registration, all service-affecting Reserve/Issue calls fail with a typed business status before provider effects. Do not register fake `ReservationMode.None` or use a supplier name as the system provider key.

### §6.6 — Seat/ExtraSeat

- A07/A08: one Seat per traveller × flight. An actual source-bound seat acceptance quote gives `SeatNumber`, A07/A08 `VariantCode`, chosen complete rate/price, source definition/provision version and `PointOfSaleId`. The purchased seat gets its own Seat OrderItem and *only* one `OrderSeatService` linked to its Air Service.
- Existing legacy SeatSelections remain compatible, but must not be combined with new seat IDs or expanded into multiple silently chosen seats for new path.
- A09 ExtraSeat is its **own product with ticket/seat safety obligations**, never disguised as A07/A08. Until real source `extraSeats[]` is emitted and validated, it cannot be live sold.

### §§6.7 and 6.8 — general ancillary and baggage

- `OrderAncillaryService.Quantity:int`; `CoveredAirServiceIds:0..N` all in the same Order; `TravellerId:long?` order-level exception as above. Persist `ServiceDefinitionRef`, `DefinitionVersionId`, `VariantCode`, `ProvisionId`, `PricingId`, `PricingRateId`, `PricingVersion`, `PricingUnit`, source typed selected values and source-document route/SSRs when actually supplied. No giant unrelated nullable DTO.
- A01–A06 purchased baggage is separate from *included* checked/cabin baggage in Air Fare. Do not spread a multi-flight ancillary into multiple commercial price lines. Preserve exact source fields, units, per-piece/per-kilogram meaning and coverage.
- A09/A17/A19 fields may be present in the source `CustomerSelectionContract` but absent from current Price/Resolve: Domain fixtures must model intended typed truths, **live source acceptance remains BLOCKED** pending actual verified wire. No invented text/Rate/EMD.

### §§7–9 — pricing and history

- One accepted basket during Create is one `OrderChange(Create)` and CommercialVersion=1. Add is one `OrderChange(AddProduct)` and increments CommercialVersion once. Source prices are immutable accepted evidence, allocated to OrderItem/Service.
- Base + **added** taxes + applied item fees = quoted total. Included taxes are already within Base; unapplied non-item fee/unknown tax treatment makes quote incomplete. No invented exchange rate or tax amount.
- Current `Order.Cancel` does not create historical financial reversal; accepted `CustomerTotal` is **not a refund/due balance**. A future explicit refund/settlement policy owns reversal/payment adjustment. Financially charged cancellation is not live-safe until this is resolved; do not invent implicit 100% refund.
- Proposed append-only `OrderPricingReason.AdditionalSale=9`, `OrderPricingLineSubCategory.AncillaryBase=6`, generic `OrderServiceType.Ancillary` only when actual local enum check shows unused values; preserve all earlier frozen enum numeric values. Existing meaningful OrderServiceTypes are reused, but purchased Baggage is never `BaggageAllowance`.

### §§13–14, §17 and Stage6 — fulfillment, documents, lifecycle

- Commercial accepted != reserved/confirmed/documented. Reserve blocks early with typed business 409 for unfulfillable active ancillary **before any provider interaction**. Confirm requires actual reservation. Whole cancellation closes all active services, including ancillary; Air partial cancellation is coverage/dependency-aware, and overlapping multi-flight products fail closed pending real requote.
- ETKT continues to document Air. Real EMD-A requires associated ETKT coupon, EMD-S is standalone, no fake RFIC/RFISC, no EMD for `NoAncillaryDocument`, no unfulfilled SSR assumed done. `TicketOrExchange` is never converted into an arbitrary EMD.
- `OrderStatus.Ticketed` remains an Air-ticket summary until the authoritative broader status is designed; read-side exposes per-service outstanding fulfillment/document truth. Never declare all-ancillary fulfillment merely because ETKT exists.
- **6A:** commercial Create/Add+24 source-typed variants+operational fail-closed guards+correct Cancel. **6B:** genuine Ancillary hold/confirm/cancel/provider recovery, physical FlightFlow Seat, and real EMD/SSR/financial policy as required for production sellable products. No 6B success based only on fixtures.

## B. CLAUDE.md exact amendment points

- Keep lines 76–82: no client servicing idempotency key; use Order lock and server-generated provider keys. Universal duplicate-Create policy remains a separate gap.
- Around line 149: replace active **Stage5** materialization authority with **Stage6A** (Master v2.0 in-place remains domain authority). Stage1–5 are closed and frozen regression authorities, not deleted. Stage6B is the conditional operational stage with tested real provider evidence.
- Around line166: permit owner-authorized source/implementation/test evidence **under repository-root `reports/` only**, while other architecture docs remain governed by existing repo convention. Keep other CLAUDE.md rules (layers, namespaces, reference projects, no comments, no broken API auth).

## C. Canonical guardrails

- Remote `.canonical-stage` currently contains numeric `5`; when Stage6A is activated, set it to numeric **`6`**, not string `6A`. Existing conformance test helper parses an integer.
- Create `canonical-stage6a-v1.0.yaml` by extending the verified Stage5 structure: `version`, `authority`, `stage_file`, `stage6a.types`, `stage6a.abstract_types`, `stage6a.value_objects`, `stage6a.enums`. Build exact actual type inventory from local Domain assembly **after** applying required changes; YAML is a thin name/enum guard, not a substitute for field-level Master.
- Add `tests/AeroTech.Ordering.Domain.ConformanceTests/Stage6ATypeSetConformanceTests.cs`, mirroring Stage5 tests with `Stage=6`. Freeze new enum values and preserve frozen Stage1–5 values. Do not weaken earlier tests and do not use future-only types to make the manifest look full.

## D. Evidence and precedence

- **Current local** Ordering commit and Master win over old remote for implementation. `cd50a2a` is only independently inspected remote baseline; review describes unpushed `6a7bb71` with already-existing AncillaryContext and migrations. Never recreate this route from old instructions.
- Ancillary actual verified source is `190aa74`; source changes AC-01…AC-07 require newer SHA, field-accurate HTTP contract and passing test before they can be claimed as landed.
- Implementation reports under `reports/`: file/line evidence, exact SHA, migration diff, build/tests/HTTP traces, domain vs source vs operational status, unresolved dependencies and explicit stage verdict.


---

# 14 — Review closure, evidence gates and acceptance matrix for an airline-operated system

## 1. Four blocking findings: concrete resolution, not hand-waving

| Finding | v1.1 defect | Mandatory v2.0 action | Closure proof |
|---|---|---|---|
| **B1** | Accepted ancillary fails Reserve `NoFulfillmentAdapterRegistered` 2505/500; whole Cancel misses it | Immediate **6A0 Reserve/Issue typed guard** before provider/DocumentStock effect, whole Cancel all active, Air dependency/overlap rule. Later **6B real Ancillary provider + EMD** for live product | `SAFE-01..09` with actual no-provider-call traces; 6B HTTP Hold/Confirm/Cancel/recovery |
| **B2** | AC-01 source, AC-02 complete-price, AC-03 seat quote, AC-04 populated-order all absent; no true HTTP flow | Implement source handoffs separately; `6A1_DOMAIN_PASS` possible on fixtures only; **real Add requires AC-01+02+04+05**, Create likewise; Seat requires AC-03 | Source SHAs, raw HTTP/contract tests; no source-missing PASS |
| **B3** | New client Idempotency-Key and Receipt table contradict `CLAUDE.md` | Delete public key/receipt entirely; existing per-order lock + expectedCommercialVersion for Add, 409 re-read on retry; Create duplicate remains a universal production blocker | `CON-01..07`, no receipt table in schema, CLAUDE unchanged policy |
| **B4** | Pack against Ordering old remote and Ancillary old HEAD; duplicate AncillaryContext route | Ancillary pinned `190aa74`; local Ordering `6a7bb71` reported but unpushed and must be inspected by coding agent. **Extend existing route** if present; no stale Master snapshot | Preflight exact SHAs; code + migration/test existence; no duplicate route |

## 2. Eleven corrections: field-level closure

| Review | Correction | Source check / acceptance |
|---|---|---|
| C1 | `OrderAncillaryService.Quantity:int`, not decimal | `ServiceResolvedPricingDto.Quantity:int`; Master local §6.7 |
| C2 | Explicit Master §6.1/§6.4 rewrite for `TravellerId:long?` true PerOrder only; keep air/seat non-null | PerOrder real model supports null; current catalog Order-scoped sellability **unverified**, so live gate is independent |
| C3 | Do not claim command/query joint transaction | `OrderingUnitOfWork`: save command then query; query failure recovery proof |
| C4 | Upstream 502, 409 expiry, 422 source mismatch; no 503 or gratuitous 410 | `ExceptionFactory` remote max 2822, proposed new codes 2824+ local collision check |
| C5 | Stage6A active authority, numeric `.canonical-stage=6`, thin YAML and type-set test | Manifest frozen older enum values, Stage1–5 green |
| C6 | Api/OtaPanel Amadeus/IATA-inspired external `services[]` adapter; Backoffice `selectedAncillaries` | Preserve existing external Create fields and auth; do not pretend proposed wire is official Amadeus |
| C7 | Reuse meaningful `OrderServiceType` members, add generic only for missing types | Explicit 24-row mapping; never use `BaggageAllowance` for purchased bags |
| C8 | Accepted seat carries source-verified `PointOfSaleId` | Source Hold requires it; quote must supply it, not re-derive after purchase |
| C9 | A09/A17/A19 Domain fidelity sourced from CustomerSelectionContract, while missing live wire stays gated | Source `extraSeats=[]`, missing medical/Oxygen/UMNR text, no fake SQL values or 0 price |
| C10 | Reports always repository-root `reports/`; align CLAUDE | Exact paths listed in doc10/12 |
| C11 | Staged 6A0/1/2/3/4 and conditional 6B, each separate verdict | No single big-bang "done" after fixture tests |

## 3. Nine owner decisions, resolved within this design

| Question | Final design choice | Tradeoff explicitly accepted |
|---|---|---|
| Q1 | Minimal Reserve early typed 409; Cancel all active + dependency-safe partial; 6B real adapter required before live sale | Commercial acceptance may exist as unreservable draft **but cannot proceed to reserve/issue** |
| Q2 | No client IdempotencyKey/Receipt; Add lock+expectedVersion; Create duplicate problem is global | Repeat successful Add with old version returns conflict, not replayed success; client must read current Order |
| Q3 | AC-02+04 first for core Domain; **AC-01+05 also mandatory before any live Add/Create** | Delivery can progress in fixtures without unsafe operation exposure |
| Q4 | Agent validates **actual unpushed `6a7bb71`** locally; no owner push required just to write Domain work | This pack cannot attest that commit's contents remotely |
| Q5 | Public Api/OtaPanel map to **Amadeus-inspired** `services[]` but preserve old Create contract; Backoffice canonical basket | New public facade is AeroTech-defined, not verbatim Amadeus wire; API snapshot must be signed off before external release |
| Q6 | Expired source selection = **409 Conflict** (source state changed); 502 upstream; 422 malformed/invalid source | The reviewer suggested 410, but first new HTTP 410 is unnecessary; a stale commercial attempt is a state conflict. Client re-shops. |
| Q7 | `TravellerId:long?` in Base now, limited to true PerOrder; amend Master | Avoid changing schema again when real Order coverage is published; verify catalog source before live sale |
| Q8 | Reuse existing meaningful service types and append one `Ancillary` for unmapped types | Generic categories keep exact `VariantCode`; do not proliferate enum members |
| Q9 | Update `CLAUDE.md`, stage YAML/test, root reports in same Stage6A activation change | Keeps Master and canonical guardrails coherent, without rewriting project tooling |

**No other owner decision is required for the basic Stage6A design.** For material unknown production source/provider behavior, use per-product `BLOCKED_SOURCE`/`BLOCKED_OPERATION` and report evidence instead of guessing. No auth-entitlement rearchitecture as part of these nine decisions.

## 4. Phase exit gates (each independent)

| Phase | Required demonstrable behavior | Gate/exit verdict |
|---|---|---|
| **6A0** | Guard new accepted services from unsupported Reserve/Issue before external effect; whole Cancel no orphan; Stage5 Air-only regression passes | `6A0_SAFETY_PASS` |
| **6A1** | All 24 typed Domain acceptance fixtures persisted/reloaded, general Add, exact Pricing, Source-derived 0..N coverage, local OrderContext extension | `6A1_DOMAIN_PASS`; **`6A1_HTTP_LIVE_PASS` additionally needs AC-01,02,04,05** |
| **6A2** | Existing Create plus optional general purchased services; one Create Change; source same Offer/expiry count and exact initial Price | `6A2_HTTP_LIVE_PASS` needs AC-01,02,05; missing source => blocked |
| **6A3** | A07/A08 separately selected/quoted traveller×flight; exact POS, no duplicated Seat Service; no physical Hold promise | `6A3_SEAT_HTTP_PASS` requires AC-03 and actual source quote |
| **6A4** | A09/A17/A19 true domain data, typed current source API when available; all A-codes tested in both Create/Add | `6A4_DOMAIN_24_PASS`, source live cells marked individually; AC-06 required for live A09/A17/A19 |
| **6B-PROVIDER** | Ancillary/FlightFlow Hold/Confirm/Cancel, atomicity and timeout Unknown/recovery real HTTP, no double physical seat | `6B_PROVIDER_VERIFIED` by product/profile |
| **6B-DOCUMENT** | ETKT+EMD-A/S or appropriate SSR/NoDocument and real payment/settlement path, document authority and trace | `6B_DOCUMENT_VERIFIED` by product/profile |
| **AIRLINE PILOT** | All applicable variant/source/provider/finance/security/compliance constraints validated with realistic booking/retry/cancel, runbooks/recovery | `AIRLINE_PILOT_READY` only upon separate business/ops approval |

**Production invariant:** Source readiness + Domain readiness alone is **not** capability to sell an ancillary in an airline. Publishing an unsupported service merely because quote/Price succeeds is an operations defect.

## 5. Named mandatory tests: source/type/financial data

### 5.1 All 24 variants, two entry points each = 48 explicit coverage rows

For each `Axx` in `A01…A24`, add named tests `V-Axx-CREATE` and `V-Axx-ADD` (48 tests), asserting at least:

1. Correct **same** `VariantCode`, family type and selected option object after SQL roundtrip; no missing/unknown code silently omitted.
2. Correct `OrderItem.Kind`, `OrderService.ServiceType`, `CreatedByChangeId`, source refs, int quantity, beneficiary and exact covered 0..N AirServices.
3. Correct priced Base/Tax/Fee and CustomerTotal difference including zero Rate; one source unit not multiplied by flights unless priced separately; no unapproved fee or exchange rate.
4. On Create, `OrderChange(Create)` only / version1. On Add, exactly one `OrderChange(AddProduct)` / version increment1; no phantom seat/ancillary duplicate.
5. Read model and all three surface mappers preserve applicable values, while public intentionally omits internal pricing/provision IDs.
6. Negative test: wrong traveller, out-of-order/fake coverage, missing required selected option, inconsistent price or unrecognized variant rejects before command mutation.

Source-gated A09/A17/A19 require complete **trusted source-contract fixtures** for Domain tests; live tests must say `BLOCKED_SOURCE` until actual Price/Resolve outputs those fields. This is honest coverage of all 24, not a claim that all are currently sold/held.

### 5.2 Domain/Source proof named cases

| ID | Case and required result |
|---|---|
| SRC-01 | Offer-bound selection accepted only with matching `OfferId`, valid expiry, selected line count |
| SRC-02 | Selection priced for other Offer is rejected; 0 Order writes |
| SRC-03 | Selection priced for other Order is rejected; no Add mutation |
| SRC-04 | Order commercial version stale rejects 409 before save |
| SRC-05 | Expired selection rejects 409; source re-shop required |
| SRC-06 | Resolved line count mismatch (e.g. A09 dropped) rejects the entire basket |
| SRC-07 | Unrecognized `VariantCode` rejects, never silently omitted |
| SRC-08 | Source Quote marked `IsUnitTotalComplete=false` rejects; no partial acceptance |
| SRC-09 | Source upstream timeout returns 502/typed indeterminate, no Order mutation |
| SRC-10 | Two previously purchased services mapped back into Order-based Shopping without 503 |
| SRC-11 | Cancelled historical service not falsely treated as active duplicate; exact enum mapping |
| SRC-12 | Order `PerOrder` without traveller accepted only for source Order/PerOrder, no fake traveller |
| SRC-13 | `PerTraveller` beneficiary exists, no other traveller imported |
| SRC-14 | Portion covering 2+ flights yields one charge and 2+ coverage links |
| SRC-15 | Journey coverage maps all actual source flight refs, not first bound guess |
| SRC-16 | Invalid flight/Portion/Order references reject, even when opaque selection token well formed |
| SRC-17 | Source definition changed after quote but immutable accepted evidence remains identical |
| SRC-18 | A17 medical, A19 text fields cannot be coerced from missing DTO/booleans |

| ID | Financial proof and required result |
|---|---|
| PR-01 | Base=100, included tax=10, added tax=5, item fee=3 -> accepted total 108, not 118 |
| PR-02 | Included tax > base rejected at source; Ordering doesn't invent correction |
| PR-03 | Non-item unapplied fee results `IsUnitTotalComplete=false` -> reject, not “remaining fee 0” |
| PR-04 | Qty3: unit price X×3 only, components/total reconciliation valid |
| PR-05 | Zero-priced A11/A15 with **real** zero Rate remains valid, no fake discount |
| PR-06 | Source complete quote currency differs from Order, without actual FX evidence -> reject |
| PR-07 | Source actual FX evidence available -> correct equivalent amount/decimal precision |
| PR-08 | Baggage two-flight coverage: charge once, no duplicated included baggage allowance |
| PR-09 | Different refunds/penalties are not inferred from `Refundability` null |
| PR-10 | Cancel without charge reversal does NOT claim CustomerTotal=refund/amount due; read models distinguish |

### 5.3 Seat-specific proof

| ID | Case |
|---|---|
| SEAT-01 | A07 paid seat gets one `OrderSeatService` and independent Seat Item, no generic shadow ancillary |
| SEAT-02 | A08 different real seat rate/provision preserved |
| SEAT-03 | One traveller selects different seat on 2-flight bound; no same SeatNumber duplication |
| SEAT-04 | Two travellers cannot buy same seat on same flight within Order |
| SEAT-05 | Same traveller cannot have two active seats on same flight; no implicit replace |
| SEAT-06 | Seat quote from different Offer/Order/CommercialVersion rejected |
| SEAT-07 | Seat quote chooses exactly one valid rate for pax/POS; two eligible rates => fail |
| SEAT-08 | Missing `PointOfSaleId` rejects and is not re-derived from TravelAgency/Office |
| SEAT-09 | Legacy SeatSelections still deserialize and legacy Air-only flow passes; mixing with new IDs rejected |
| SEAT-10 | FlightFlow physical seat Hold only from verified provider; read-only Quote doesn't consume seat |

### 5.4 Concurrency, cancellation, provider and document proof

| ID | Case |
|---|---|
| CON-01 | Two Add with same expectedVersion/Order lock: exactly one commit |
| CON-02 | Retry successful Add with old version => 409 and read the actual change, no receipt table |
| CON-03 | Source Resolve fails midway => zero new Item/Service/Change in command DB |
| CON-04 | Query projection save fails **after** command save: command state true, retry cannot duplicate, reprojection repairs |
| CON-05 | ExpectedCommercialVersion mismatch with one previously reserved/cancelled operation => Add rejects |
| CON-06 | `Created` with any historical Reservation/Task/ETKT/EMD cannot Add without specialized future servicing |
| CON-07 | Existing Api/OtaPanel Create request/response unchanged when new fields absent |
| SAFE-01 | Create/Add with unregistered Ancillary provider then whole Reserve -> typed 409 **no FlightFlow/provider calls** |
| SAFE-02 | Targeted Reserve of ancillary or mixed air/ancillary -> typed 409 before provider side effect |
| SAFE-03 | Air-only Reserve, Confirm, Issue unchanged from Stage1–5 suites |
| SAFE-03B | Source-priced A07/A08 with unverified commercial Hold/DocumentNeeded is blocked *before* FlightFlow physical call, even though the FlightFlow provider is registered |
| SAFE-04 | IssueOrder with unfulfilled mandatory ancillary -> typed 409 before DocumentStock changes |
| SAFE-05 | Whole-Order Cancel includes all active purchased Baggage/Meal/Seat and Air; no orphan Active rows |
| SAFE-06 | Air partial Cancel cascades only to fully dependent service coverage; no arbitrary order-level discard |
| SAFE-07 | Partial flight cancellation intersects indivisible two-flight baggage -> 409 until actual re-quote/settlement |
| SAFE-08 | Held/Confirmed ancillary provider effect Unknown => don't mark commercial cancelled, preserve recovery target |
| SAFE-09 | A09/A13/A14/A17/A19 without verified provider/approval/document cannot be live sold |
| OPS-01 | General Hold full success with actual unit IDs and expiry, `GET Hold/{id}` readback |
| OPS-02 | Provider timeout/Unknown and replay with same provider-operation key yields no duplicate reservation |
| OPS-03 | Provider partial result honors exact `BatchResultMode` and cancellation scope; no guessed atomicity |
| OPS-04 | Seat physical FlightFlow inventory holds once while Ancillary Seat commercial hold doesn't double consume |
| OPS-05 | Issued ETKT then EMD-A for purchased baggage associates with correct coupon/amount |
| OPS-06 | EMD-S issued without ticket association where actual service routing says standalone |
| OPS-07 | Genuine SSR/NoAncillaryDocument requires operational service confirmation, not automatic EMD |
| OPS-08 | Failed EMD or supplier confirmation leaves per-service pending/failed state; Ticketed Air summary not full delivery |
| OPS-09 | Provider release after Cancel/expiry doesn't mark success on Unknown response; recovery readback |
| OPS-10 | Refund/void/chargeback/settlement governed by actual policy; no accepted price interpreted as refund |

## 6. Migration/test compatibility and adversarial checks

- Fresh SQL commands + query migrations build and run, and **upgrade a Stage5 database** without data loss. Verify one old legacy Seat and Air+coupon ticket read after upgrade, nullable Base field mapping and no fake POS values.
- Stage1–5 full suite, architecture, OpenAPI, serializer, REST errors, SQL execution, projections, record locator, issue/void/partial cancel, fare pricing atom tests remain passing. Do not weaken legacy guards.
- Future 6B issue proof must separately check EMD coupon association, stock issue uniqueness, operation retry, offline provider, `DocumentRequired` vs `DocumentRouting` and payments. **An issued ETKT is not an EMD**.
- At least one full real booking scenario: adult+child, multi-flight bound, two SeatMap selections (A07/A08), baggage (A01 and A03), free SSR meal (A11), paid CIP (A22); genuine priced Create/Add as available, reservation/confirmation/issuance documented, cancellation with recovery. If a variant requires unavailable provider, mark just that variant `OPERATION_BLOCKED`—do not create a fictitious provider or green test.

## 7. Reporting and final verdict contract

Each test report has ID, code path, payload/fixture class, actual HTTP source commit, actual result status, SQL evidence, and `PASS/FAIL/BLOCKED_SOURCE/BLOCKED_OPERATION`. **Do not count blocked or fixture-only tests as live tests**; old Pack v1.1 “145 tests” was a proposed set, not run evidence. The named cases in this pack are the traceability authority; run and report actual numbers.

Deliver in repository root:

- `reports/STAGE6A-SOURCE-DELTA.md`
- `reports/STAGE6A-DOMAIN-FIELD-PARITY.md`
- `reports/STAGE6A-INTEGRATION-EVIDENCE.md`
- `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md`
- `reports/STAGE6B-OPERATIONAL-CONFORMANCE.md` when 6B starts

Final owner report: actual HEADs, migration names and generated changes, source AC-01…AC-07 status, per-variant matrix, executed test commands/counts, release gate, remaining source/production blockers. **No “FINAL CLOSED” with only Domain fixtures or missing unpushed local source.**


---
