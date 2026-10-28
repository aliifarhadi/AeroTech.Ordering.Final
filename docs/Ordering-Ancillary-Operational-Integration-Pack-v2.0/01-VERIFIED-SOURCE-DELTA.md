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
