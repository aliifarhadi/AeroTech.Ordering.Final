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
| D3 | Superseded by D15. No new priced seat was reservable in 6A. | The Reserve guard rejected every Order holding one. |
| D4 | 6B is removed from this coding scope. | It needs a separate domain and flow closure first. |
| D5 | `Contracts/AeroTech.Messages/Ancillary/` (85 enum files) is copied verbatim from `AeroTech.Ancillary@190aa74`. | Ordering uses `AeroTech.Messages.Ancillary.Enums` directly, as it uses `AirPrice.Enums.AirFareType`. The folder is read-only here. |
| D6 | Ordering stores only the scalar facts that identify what was sold, plus every customer choice. | Catalog eligibility lists are not copied: allowed SSRs, allowed animals, eligible aircraft, eligible fare families, compatible seat groups, airports and cabins. |
| D7 | An ancillary cannot be cancelled on its own in 6A. | It ends only with the Air service its coverage depends on, or with a whole-order cancel. A request for the ancillary alone gets a typed rejection. |
| D8 | The Api/OtaPanel Add response carries `services[]` with `orderServiceId`, `serviceType` and `variantCode` per accepted service. | Internal pricing and provision ids are not exposed. |
| D9 | Superseded by D12. Families were first modelled with one typed choice value object each, and only baggage had its own entity. | — |
| D10 | The A07/A08 seat domain is built now, with fixtures. | Only the adapter and the live test wait for AC-03. Seats extend `OrderSeatService`: one per traveller × flight, with a quote snapshot that includes `PointOfSaleId` and its own `ProductType.Seat` item. |
| D11 | Tax jurisdiction on an ancillary price line uses Master §8.1 `TaxJurisdictionSnapshot` (CountryId, StationId). The source treatment and fee unit are two scalar fields on `PricingLine`. | The invented `PriceComponentSource` was removed and the command migration was regenerated. |
| D12 | Each family is a sealed entity under an abstract `OrderAncillaryService` (owner diagram). | The base holds source, definition, provision, pricing, coverage and quantity. Nine family entities hold their variant's own facts and rules: Baggage, ExtraSeat, Upgrade, Meal, Pet, AssistedTravel, Airport, Priority and Connectivity (Master §6.7.1). The nine choice value objects were removed; `MedicalEquipmentChoice` and `UnaccompaniedMinorChoice` stay inside `OrderAssistedTravelService`. Command migration `SplitAncillaryFamilies` creates one TPT table per persisted family, copies existing rows, then drops the 41 old columns; `Down` copies back before dropping the tables. The read model stays one ancillary table. |
| D13 | Offer-sourced Journey coverage is derived from the accepted Offer. | The source defines Journey as every flight of every portion of the exact Offer (`CoveragePlanner.Units`), and `SelectionScopes.Of(Journey)` is `PerTraveller`. `CoverageRef` is internal and not on the wire, so Ordering proves the case from `SelectionScope=PerTraveller`, `Provision.CoverageScope=Journey` and a unit with no portion or flight, after the source reference equals the Offer id. The service covers every active air service of that traveller, once. Order-sourced Journey still needs named flights. Master §6.7 is amended in place (R4, 2026-10-31). |
| D14 | Create takes `AncillaryPricedSelectionId` and `SeatPricedSelectionId` on Backoffice, Api and OtaPanel. | Each is one optional `s_` snapshot id, an AeroTech adapter name and not an IATA field. They replace `selectedAncillaries`, `seatSelectionIds[]` and the external `services[]` for Create only. Seats resolve through `SeatMaps/Price` and `SeatMaps/Resolve`, so AC-03 and `ValidateForAcceptance` are not built. A seat source keeps `s_` as `SelectionId` and the seat id as `OfferItemId` (R4, 2026-10-31). |
| D15 | Legacy `SeatSelections` is removed from Create, and a priced seat may be reserved and confirmed. | The seat number comes only from `SeatMaps/Resolve` and is stored per traveller × flight, so it reaches FlightFlow through the existing hold. `EnsureReservationIsSupported` (Reserve, Confirm) refuses only active `OrderAncillaryService`. `EnsureIssuanceIsSupported` (Issue) also refuses priced seats, free ones included, because no EMD exists until 6B. Accepted risk: FlightFlow has no seat uniqueness guard until OR-006 closes. This replaces R4 G01 (legacy part), G11 and the seat part of G13, and the condition in R4 `03`. Codes 2716, 2717 and 2866 are gone. Seat rows stored earlier still load because the quote columns stay nullable (owner, 2026-10-31). |
| D16 | `OrderingUnitOfWork` commits the command and read model contexts in one shared transaction. | Both contexts use one database, so the read model joins the command connection and transaction. Before this, the outbox row committed first, so a publish between the two saves let the consumer insert the read model header. The API then failed with 500, the Order stayed stored and the read model stayed partial. This happened live on 2026-10-31 (owner, 2026-10-31). |

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
| AC-01: `sourceKind`, `sourceReference`, `expiresAtUtc`, `selectedLineCount` in the Resolve header | Missing at `190aa74`. Delivered uncommitted on `c6e783b` (R4-A) | The ACL rejects the basket as source-incomplete until that release is running. |
| AC-02: `isUnitTotalComplete` in Resolve pricing | Missing at `190aa74`. Delivered uncommitted on `c6e783b` (R4-A) | Pricing is rejected as incomplete until that release is running. |
| AC-03: read-only seat acceptance quote | Replaced by `SeatMaps/Price` and `SeatMaps/Resolve` (D14). The seat header and count are delivered uncommitted on `c6e783b` (R4-A) | The adapter exists and fails closed until that release is running. |
| AC-04: non-empty `ancillaryServices` in the shopping context | Rejected (`OrderingAncillaryContextMapper.cs:17`) | Ordering will project accepted services. Second shopping fails until Ancillary changes. |
| AC-05: truthful coverage | Closed for Offer source by D13, with no wire change | Order-sourced Journey coverage is rejected until the source states its flights. |
| AC-06: A09, A17 and A19 typed source | Missing | D1. |

## 4. Increment plan and status (2026-10-28)

| Increment | Scope | Status |
|---|---|---|
| 6A.0 safety | Reserve, Confirm and Issue are refused (2854) before any provider or stock effect when an active ancillary exists. Issue is also refused for a priced seat (D15). A whole cancel ends every active service; an ancillary-only cancel is refused (2856); a partial cancel that ends only part of a multi-flight coverage is refused (2855). | Done, tested |
| 6A.1 domain | `OrderService.TravellerId: long?`, abstract `OrderAncillaryService` with nine sealed family entities (D12), 8 value objects, `AncillaryVariant` registry, two-phase plan-then-commit acceptance in `Order.AddAncillaries`, price normalization (included tax once, item fees only, same currency), codes 2824–2865 | Done, tested |
| 6A.1 persistence and read side | Command migrations `AddAncillaryServices` and `SplitAncillaryFamilies` (D12), query migration `ProjectAncillaryServices`, the `OrderAncillaryServices` read model, snapshot projection, `AncillaryContext.ancillaryServices[]` | Done, tested. `acceptsChanges` stays `false`, because the read side has no reservation-history fact yet. |
| 6A.1 application, providers and API | `IAncillaryProvider` Resolve adapter (fails closed while AC-01 and AC-02 are missing), `AddAncillariesService`, three surface commands, `POST .../{orderId}/Ancillaries` on Backoffice, Api and OtaPanel | Done. Live use is `BLOCKED_SOURCE` (AC-01, AC-02). |
| G0 family-split verification | `SplitAncillaryFamilies` refuses to drop anything unless every row lands in exactly one family table matching its `VariantCode`; real old→new upgrade from data written by the `372a1e4` model; TPT load profile (`reports/Stage6A-G0-G1-Evidence-2026-10-28.md`) | Done, tested |
| G1 typed order detail | `GetOrderById` and the Backoffice order detail carry each accepted ancillary's variant, quantity, coverage, definition and typed customer choices. OTA/OtaPanel (Amadeus shape) are unchanged. | Done, tested. Priced seats cannot reach the read side yet (no seat flow before AC-03). |
| 6A.2 Create with selections | One Create change at version 1 with `InitialSale` pricing through the same `AcceptSelections` kernel as Add, on all three surfaces (R4, D13, D14) | Done. Live proof is in `reports/Stage6A2-R4-Create-Evidence-2026-10-31.md`. |
| 6A.3 Seats | Per traveller × flight, quote snapshot with `PointOfSaleId`, its own Seat item, duplicate guards (D10). Port and adapter read `SeatMaps/Resolve` (D14). | Done, including a live FlightFlow hold with the seat number (D15). |
| Guardrails | `canonical-stage6a-v1.0.yaml`, `Stage6ATypeSetConformanceTests`, `.canonical-stage = 6`, CLAUDE.md authority switch, Master v2.0 amended in place (§6.4, §6.6–§6.8 with §6.7.1, §8.1, §9.1, §27.8, §30, §34.1, §35) | Done |
| Handoff | AC-01…AC-06 filed as `E:\Projects\DotAir\handoffs\OR-005-ancillary-source-contracts-for-commercial-acceptance.md`, listed in the ledger index | Requested |

## 5. Verification evidence (2026-10-28)

| Check | Result |
|---|---|
| Run | 2026-10-28 19:04–19:06, working tree on `48f53d9` plus the G0/G1 changes, SQL Server `AL-FARHADI-L\SQLEXPRESS` 15.0.2200.2 Express |
| Clean solution build (`--no-incremental`) | 0 errors, 53 warnings (same as the Stage 5 baseline), 30 s |
| Conformance tests | 27/27 passed |
| Acceptance tests (real SQL Server: one migrated database per run, plus a fresh database per migration test) | 612/612 passed in 30 s |
| SQL command round trip | Every persisted variant (19), two priced seats and a whole-order service reload unchanged |
| Family split upgrade | Rows written by the `372a1e4` model upgrade to exactly what the current model persists; a row without its family data stops the migration and leaves the schema and rows unchanged |
| Dev database | `DotAirOrdering` at `20261028125838_SplitAncillaryFamilies`; 15 orders, 58 services, 189 price lines unchanged |
| A09, A17, A19 | Domain tests only (D1). SQL round trip is `BLOCKED_SOURCE`. |

Stage 6A.2 evidence, including the 2026-10-31 run, is in `reports/Stage6A2-R4-Create-Evidence-2026-10-31.md`.
