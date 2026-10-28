# Stage 6A: decisions and blockers

| | |
|---|---|
| Branch | `stage6a-ancillary-commercial`, created from local `k8s-stg@6a7bb71` |
| Ordering remote | `origin/k8s-stg@cd50a2a` (`6a7bb71` is not pushed) |
| Ancillary | `k8s-stg@190aa74`, same as `origin/k8s-stg`, clean working tree |
| Authority | `docs/Ordering-Ancillary-Operational-Integration-Pack-v2.0/`, corrected by `reports/Stage6-Pack-v2.0-Review-2026-10-28.md` and the owner decisions below |

## 1. Owner decisions, 2026-10-28

| # | Decision | Effect |
|---|---|---|
| D1 | A09, A17 and A19 exist in the domain with in-memory tests only. They get no SQL mapping until Ancillary delivers AC-06. | The ACL rejects these variants as source-incomplete. `OrderExtraSeatService` has no table, `OrderAssistedTravelService` keeps `MedicalEquipment` and `UnaccompaniedMinor` unmapped (`Ignore`), and their SQL round trip is `BLOCKED_SOURCE`. |
| D2 | Stage 6A stays on `k8s-stg` and is not promoted to production until 6B is verified for at least one product. | No feature flag in code. |
| D3 | No new priced seat is reservable in 6A. | The Reserve guard rejects every Order holding one, with no exception rule. |
| D4 | 6B is removed from this coding scope. | It needs a separate domain and flow closure first. |
| D5 | `Contracts/AeroTech.Messages/Ancillary/` (85 enum files) is copied verbatim from `AeroTech.Ancillary@190aa74`. | Ordering uses `AeroTech.Messages.Ancillary.Enums` directly, as it uses `AirPrice.Enums.AirFareType`. The folder is read-only here. |
| D6 | Ordering stores only the scalar facts that identify what was sold, plus every customer choice. | Catalog eligibility lists are not copied: allowed SSRs, allowed animals, eligible aircraft, eligible fare families, compatible seat groups, airports and cabins. |
| D7 | An ancillary cannot be cancelled on its own in 6A. | It ends only with the Air service its coverage depends on, or with a whole-order cancel. A request for the ancillary alone gets a typed rejection. |
| D8 | The Api/OtaPanel Add response carries `services[]` with `orderServiceId`, `serviceType` and `variantCode` per accepted service. | Internal pricing and provision ids are not exposed. |
| D9 | Superseded by D12. Families were first modelled with one typed choice value object each, and only baggage had its own entity. | — |
| D10 | The A07/A08 seat domain is built now, with fixtures. | Only the adapter and the live test wait for AC-03. Seats extend `OrderSeatService`: one per traveller × flight, with a quote snapshot that includes `PointOfSaleId` and its own `ProductType.Seat` item. |
| D11 | Tax jurisdiction on an ancillary price line uses Master §8.1 `TaxJurisdictionSnapshot` (CountryId, StationId). The source treatment and fee unit are two scalar fields on `PricingLine`. | The invented `PriceComponentSource` was removed and the command migration was regenerated. |
| D12 | Each family is a sealed entity under an abstract `OrderAncillaryService` (owner diagram). | The base holds source, definition, provision, pricing, coverage and quantity. Nine family entities hold their variant's own facts and rules: Baggage, ExtraSeat, Upgrade, Meal, Pet, AssistedTravel, Airport, Priority and Connectivity (Master §6.7.1). The nine choice value objects were removed; `MedicalEquipmentChoice` and `UnaccompaniedMinorChoice` stay inside `OrderAssistedTravelService`. Command migration `SplitAncillaryFamilies` creates one TPT table per persisted family, copies existing rows, then drops the 41 old columns; `Down` copies back before dropping the tables. The read model stays one ancillary table. |

## 2. Repository conventions that override the pack (CLAUDE.md governs layering, placement and EF mapping)

| Pack text | Repository convention applied |
|---|---|
| Doc 05: `IAncillarySaleSource` as an Application port | Port and contract types go in `Domain/Providers/Ancillary/`, the adapter in `Providers/Ancillary/Services/`, wire DTOs in `Providers/Ancillary/Wire/`, and DI uses `AddHttpClient` with `Ancillary:BaseUrl`. This is the same pattern as Offer, FlightFlow and Pricing. |
| Doc 10: "coverage child relationships" | `CoveredAirServiceIds` is a `PrimitiveCollection` field, the same as `OrderFareComponent.CoveredOrderServiceIds`. |
| Doc 03 §4: `DefinitionId` / `definition.id` | The source field is `DefinitionVersionId` (`ServiceResolvedDefinitionDto`). |
| Doc 03 §4: `PointOfSaleId` on `OrderAncillaryService` | Not added. Point of sale belongs to the accepted seat quote (6A.3) on `OrderSeatService`. |
| Doc 08 §2: `acceptsChanges` requires that the "source can honestly price Add" | Only Ordering facts are used: Created, current version, no reservation or task history, no document, deadline not passed. |

## 3. Open source contracts (Ancillary, not implemented here)

| AC | Status at `190aa74` | Ordering behavior until it lands |
|---|---|---|
| AC-01: `sourceKind`, `sourceReference`, `expiresAtUtc`, `selectedLineCount` in the Resolve header | Missing | The ACL rejects the basket as source-incomplete. Live Add and Create are `BLOCKED_SOURCE`. |
| AC-02: `isUnitTotalComplete` in Resolve pricing | Missing | Pricing is rejected as incomplete. |
| AC-03: read-only seat acceptance quote | Missing | The seat domain is built with fixtures (D10). The adapter and the live seat test are `BLOCKED_SOURCE`. |
| AC-04: non-empty `ancillaryServices` in the shopping context | Rejected (`OrderingAncillaryContextMapper.cs:17`) | Ordering will project accepted services. Second shopping fails until Ancillary changes. |
| AC-05: truthful coverage | Partial | The ACL maps only Sector, Portion and Order. Journey coverage is rejected until the source states its flights. |
| AC-06: A09, A17 and A19 typed source | Missing | D1. |

## 4. Increment plan and status (2026-10-28)

| Increment | Scope | Status |
|---|---|---|
| 6A.0 safety | Reserve, Confirm and Issue are refused (2854) before any provider or stock effect when an active ancillary or a new priced seat exists. A whole cancel ends every active service; an ancillary-only cancel is refused (2856); a partial cancel that ends only part of a multi-flight coverage is refused (2855). | Done, tested |
| 6A.1 domain | `OrderService.TravellerId: long?`, abstract `OrderAncillaryService` with nine sealed family entities (D12), 8 value objects, `AncillaryVariant` registry, two-phase plan-then-commit acceptance in `Order.AddAncillaries`, price normalization (included tax once, item fees only, same currency), codes 2824–2865 | Done, tested |
| 6A.1 persistence and read side | Command migrations `AddAncillaryServices` and `SplitAncillaryFamilies` (D12), query migration `ProjectAncillaryServices`, the `OrderAncillaryServices` read model, snapshot projection, `AncillaryContext.ancillaryServices[]` | Done, tested. `acceptsChanges` stays `false`, because the read side has no reservation-history fact yet. |
| 6A.1 application, providers and API | `IAncillaryProvider` Resolve adapter (fails closed while AC-01 and AC-02 are missing), `AddAncillariesService`, three surface commands, `POST .../{orderId}/Ancillaries` on Backoffice, Api and OtaPanel | Done. Live use is `BLOCKED_SOURCE` (AC-01, AC-02). |
| 6A.2 Create with selections | | Not started |
| 6A.3 Seats | Domain done (D10): per traveller × flight, quote snapshot with `PointOfSaleId`, its own Seat item, duplicate guards. The port and adapter wait for AC-03. | Domain done, tested |
| Guardrails | `canonical-stage6a-v1.0.yaml`, `Stage6ATypeSetConformanceTests`, `.canonical-stage = 6`, CLAUDE.md authority switch, Master v2.0 amended in place (§6.4, §6.6–§6.8 with §6.7.1, §8.1, §9.1, §27.8, §30, §34.1, §35) | Done |
| Handoff | AC-01…AC-06 filed as `E:\Projects\DotAir\handoffs\OR-005-ancillary-source-contracts-for-commercial-acceptance.md`, listed in the ledger index | Requested |

## 5. Verification evidence (2026-10-28)

| Check | Result |
|---|---|
| Clean solution build (`--no-incremental`) | 0 errors, 53 warnings (same as the Stage 5 baseline) |
| Conformance tests | 27/27 passed |
| Acceptance tests (real SQL Server, fresh database per class, both migration sets) | 608/608 passed: the 554 existing tests plus 54 new Stage 6A tests |
| SQL command round trip | Every persisted variant (19), two priced seats and a whole-order service reload unchanged |
| A09, A17, A19 | Domain tests only (D1). SQL round trip is `BLOCKED_SOURCE`. |
