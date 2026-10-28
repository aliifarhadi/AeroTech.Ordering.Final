# Stage 6A Commercial Acceptance Pack v1.1: review findings and required corrections

| | |
|---|---|
| Date | 2026-10-28 |
| Pack reviewed | `docs/Ordering-Ancillary-Commercial-Acceptance-Pack-v1.1/`: files 00–17, README and both `reference/` files |
| Ordering | local `k8s-stg@6a7bb71` (not pushed); `origin/k8s-stg@cd50a2a` |
| Ancillary | `k8s-stg@190aa74`, same as `origin/k8s-stg` |
| Verdict | **Not ready for the coding agent.** There are 4 blocking findings (B1–B4), 11 corrections (C1–C11) and 9 owner decisions (Q1–Q9). |

Every claim below was checked against the source on 2026-10-28. Paths are relative to the root of the repository named in each row.

## 1. Pack claims verified as correct

| Pack claim | Evidence |
|---|---|
| `OrderPricingReason` ends at `Cancel = 8`, so `AdditionalSale = 9` is append-only. | Ordering `Contracts/AeroTech.Messages/Ordering/Enums/OrderPricingReason.cs:29` |
| `OrderPricingLineSubCategory` ends at `Promo = 5`, so `AncillaryBase = 6` is append-only. | Ordering `.../Enums/OrderPricingLineSubCategory.cs:20` |
| `OrderChangeType.AddProduct = 2` exists. | Ordering `.../Enums/OrderChangeType.cs:8` |
| `ProductType` already has `Seat`, `Baggage`, `Meal` and `Ancillary`. | Ordering `.../Enums/ProductType.cs` |
| The Resolve header has only `OrderId`, `OrderCommercialVersion` and the ten family arrays. It has no source kind, offer id, expiry or selected-line count. | Ancillary `src/AeroTech.Ancillary.Query/Shopping/AncillaryOffers/Dto/Service/ServiceResolvedSelectionsDto.cs:3-15` |
| Resolve pricing has no `IsUnitTotalComplete`. The flag exists only on the domain rate. | Ancillary `.../Dto/Service/ServiceResolvedPricingDto.cs:5-16`; `src/AeroTech.Ancillary.Domain/AncillaryPricingAggregate/Entities/AncillaryPricingRate.cs:74` |
| `extraSeats` and `travelInsurance` are always `[]`. | Ancillary `.../ResolveAncillarySelection/Service/ServiceResolvedSelectionsMapper.cs:27-28` |
| Ancillary rejects a non-empty `ancillaryServices`. | Ancillary `src/AeroTech.Ancillary.Providers/Ordering/Services/OrderingAncillaryContextMapper.cs:17` |
| `SelectionScopes.Of` maps Order→PerOrder, Journey→PerTraveller, Portion→PerTravellerPerPortion and Sector→PerTravellerPerFlight. | Ancillary `src/AeroTech.Ancillary.Shopping/Selections/SelectionScopes.cs:10-13` |
| `IReservationLock`, `CallerOwnedOrderAuthorization`, `IFulfillmentTaskRepository` and `OfferReader.GetTravellerRef` exist. | Ordering `src/AeroTech.Ordering.Application/...`, `src/AeroTech.Ordering.Domain/...` |
| The Create routes are `Backoffice/v1/Orders/FlightOffers`, `OtaPanel/v1/Bookings/FlightOffers` and `Api/v1/Bookings/FlightOffers`. | Ordering `src/AeroTech.Ordering.RestApi/V1/OrderAggregate/Controllers/{Backoffice,OtaPanel,Ota}Controller.cs` |

## 2. Blocking findings

### B1: an Order that accepts an ancillary dead-ends at Reserve, and Cancel leaves it half open

**Evidence**

- **Reserve fails.**
  - Reserve selects services through `RequiresReservation` (Ordering `src/AeroTech.Ordering.Application/FulfillmentReservationAggregate/Commands/Reserve/ReserveService.cs:83`, `:268`).
  - `RequiresReservation` calls `CapabilityOf`, which calls `Resolve(service.FulfillmentProviderKey)` (`.../Services/ReservationProviderResolver.cs:19-36`).
  - An unregistered key throws `NoFulfillmentAdapterRegistered` (2505, HTTP 500).
  - Doc 03 §9 gives new non-seat services the key `Ancillary` and forbids registering a provider for it.
  - So every Order that accepts a non-seat ancillary fails Reserve with 2505/500 and can never be reserved or ticketed.
- **Cancel leaves the service active.**
  - `Order.CancellationScopeOf` returns only `OrderAirTransportService` (plus dependent infants) and their `OrderSeatService` (Ordering `src/AeroTech.Ordering.Domain/OrderAggregate/Order.Cancellation.cs:11-32`).
  - A whole-order cancel therefore leaves an `OrderAncillaryService` Active, and its price stays in `CustomerTotal`.
- **The pack leaves both paths untouched.** Doc 02 marks `Order.Cancellation.cs` and `Order.Reservation.cs` as untouched, and doc 10 does not list them.
- **Both paths are reachable** as soon as 6A ships:
  - Created → Add → Reserve;
  - Created → Add → Cancel.

**Required correction:** inside 6A, define the minimal behavior of an Order that holds accepted ancillaries, and add FLOW tests for both paths.

1. **Reserve** rejects the Order before any provider call, with a typed business error (409 or 422, new code from 2824 up): "the order holds commercially accepted services that cannot be reserved yet".
   - The alternative is a registered `Ancillary` capability whose mode the planner skips.
   - That alternative would let Air be reserved and ticketed while the ancillary stays in `CustomerTotal` with no fulfillment or EMD, so it is not recommended.
   - Owner decision: Q1.
2. **Cancel:**
   - Cancelling an Air service cascades to the ancillary services whose coverage depends on it.
   - A whole-order cancel includes every active service.
   - Pricing reversal follows the existing Stage-5 reversal rule.

### B2: no live flow can close with today's Ancillary source

By the pack's own gates:

| Flow | Blocking contract | Pack reference |
|---|---|---|
| Create with a general selection | AC-01: source offer id, kind, expiry and line count | doc 05 §2 |
| Any general acceptance, Create or Add | AC-02: `isUnitTotalComplete` must be present and true | doc 06 §2, predicate 11 |
| Seats A07/A08 | AC-03: read-only seat quote | doc 07 §6 |
| Second shopping after a first Add | AC-04: non-empty `ancillaryServices` | doc 09 §3 |

All four contracts are absent at Ancillary `190aa74`. Stage 6A would therefore end fixture-only. Under the stage-closure rule (correct domain, source-verified provider semantics and passing end-to-end flows) it cannot close.

**Required correction:** sequence the cross-service work.

1. Request AC-02 and AC-04 from Ancillary first.
   - AC-02 exposes a flag the rate already computes.
   - AC-04 removes one guard and adds a mapping.
2. Then AC-01.
3. Then AC-03.

In doc 14, state the verdict each Ordering increment can reach given which AC items have landed.

### B3: the client idempotency key contradicts CLAUDE.md and the committed Stage-6 prompt

**Evidence**

- `CLAUDE.md:78`: servicing operations take no client idempotency key. Each runs under a distributed lock with a server-derived key, `{op}:{orderId}`.
- `docs/NEXT-CHAT-PROMPT-Ordering-Stage6-Ancillary-EMD-Domain-Closure.md:43-45` lists the Create duplicate/idempotency policy as a known gap and says: "Do not solve these by inventing Ordering concepts. Keep them parallel source-contract work."
- Doc 04 §2–§3 and doc 08 §4 add:
  - a required client `idempotencyKey`;
  - a new `AncillaryAcceptanceReceipts` table.

  Both apply only to Create-with-ancillaries and to Add. Legacy Create keeps no key, so Create ends up with two duplicate policies.
- Ordering has no idempotency-receipt implementation in `Framework/` or `src/` today. The only idempotency keys are the provider-facing ones in FulfillmentReservation and FulfillmentTask.

**Required correction** (recommendation; owner decision Q2):

- **Add** uses the existing pattern:
  - the per-order lock with a server-derived key `{op}:{orderId}`;
  - `expectedCommercialVersion` as the duplicate guard.

  A retried Add after a success fails with 409 (version mismatch), and the client re-reads the Order. No receipt table.
- **Create** leaves duplicate protection to production gap 3, solved once for every Create, not as a special case for Create-with-ancillaries.
- **If the owner keeps the receipt:**
  - amend `CLAUDE.md:78` in the same change;
  - check AirPrice and FlightFlow `Framework/` for an existing receipt implementation first, as CLAUDE.md requires;
  - apply the receipt to legacy Create as well.

### B4: the pack baseline is stale

| Item | Pack says | Source today |
|---|---|---|
| Ordering baseline | `cd50a2a` | Local `6a7bb71` (not pushed) adds `GET Service/v1/Orders/{orderId}/AncillaryContext`, `FareType` as `AirFareType?`, `Quantity: int` in Master §6.7, and command and query migrations. |
| AncillaryContext | "Absent; create a new controller" (doc 02, doc 09 §1, doc 10 §1) | Implemented in `src/AeroTech.Ordering.RestApi/V1/OrderAggregate/Controllers/ServiceController.cs`, with tests in `tests/AeroTech.Ordering.Application.AcceptanceTests/AncillaryContext/OrderAncillaryContextTests.cs`. |
| Ancillary baseline | `c873971` | `190aa74` ("Performance Fix", 2026-10-28, 6 shopping-engine files). |
| `reference/ORDERING-MASTER-V2.0-READONLY.md` | Snapshot of the Master authority | SHA-256 `7c1ddaaa…` is the `cd50a2a` Master. The current Master is `1e51a948…`, so the snapshot lacks the FareType and Quantity amendments. |

**Required correction:**

- Rebase the pack on `6a7bb71` (once pushed; Q4) and on `190aa74`.
- Change doc 09 from "implement the route" to "extend the existing route":
  - the `ancillaryServices[]` fields;
  - the `acceptsChanges` predicate.
- Delete or refresh the Master snapshot, so the agent cannot read the stale copy.

## 3. Corrections

**C1: `OrderAncillaryService.Quantity` must be `int`.**
Doc 03 §4 says `decimal`. That contradicts:
- Master §6.7 (Master line 400; owner decision 2026-10-27);
- the pack's own `AcceptedQuantity: int` in doc 03 §3;
- Ancillary, which uses `int` in both `ServiceResolvedPricingDto.cs:10` and `OrderingAncillaryContext.cs:159`.

**C2: a nullable `OrderService.TravellerId` needs an explicit Master §6.4 amendment.**
- Master lines 322 and 329 state the IATA passenger-specific benchmark and "Exactly one beneficiary occurrence".
- The change has a real source basis:
  - Ancillary `src/AeroTech.Ancillary.Domain/AncillaryReservationAggregate/Entities/AncillaryReservationUnit.cs:53-56` requires `TravellerId is null` for `ServiceCoverageScope.Order`;
  - the Ancillary v13.x design packs describe order-level CIP and Wi-Fi products.
- But it changes a NOW field of the abstract base and every read model and DTO, which is more than an addendum.
- The pack must:
  - rewrite §6.4, including its benchmark sentence;
  - state whether the current catalog sells any Order-coverage product. This review did not verify that; if there is none, the change can wait until one exists (Q7).

**C3: correct the atomicity wording.**
- Doc 08 §2.8, doc 08 §3.7 and doc 10 §3 require the command data, the query projection and the receipt to commit atomically.
- `OrderingUnitOfWork.SaveChangesAsync` (Ordering `src/AeroTech.Ordering.Synchronizer/OrderAggregate/OrderingUnitOfWork.cs:18-23`) saves the command context and then the query context, with no shared transaction. This is the sibling-aligned design.
- Replace the requirement with "the same commit semantics as the existing Create and servicing commands". Only command-side rows are atomic: the Order, plus the receipt if it is kept.

**C4: error statuses.**
- Doc 04 §4 uses 503 for "Ancillary unavailable" and 410 for an expired selection.
- `ExceptionFactory` uses 502 for upstream providers (5 methods) and has no 503 and no 410.
- Use 502 for "unavailable". The status for expiry is owner decision Q6.
- New codes come from Ordering's 2000–2999 block; the next free code is 2824.

**C5: authority and guardrails.**
- `CLAUDE.md:149` still names Stage 5 as the active authority, and `.canonical-stage` is `5`.
- The pack adds about 15 types and 3 enum members:
  - `OrderAncillaryService`, `OrderBaggageService` and `AcceptedAncillarySourceSnapshot`;
  - about 10 typed choice value objects;
  - a pricing-component evidence value object;
  - the receipt.
- It never mentions a `canonical-stage6a-*.yaml`, a Stage-6A type-set conformance test or the `.canonical-stage` switch.
- Add all three to doc 10 §1, and make the CLAUDE.md authority switch a deliverable.

**C6: the OTA and OtaPanel request and response shape.**
- Owner standing rule (2026-09-23): OTA (`Api/…`) and OtaPanel requests and responses follow the Amadeus API shape; only Backoffice keeps the internal shape.
- Doc 04 §2–§3 adds the AeroTech-specific `selectedAncillaries`, Add body and Add result to all three surfaces.
- Either define an Amadeus-shaped mapping for Api and OtaPanel, or record an owner exception (Q5).

**C7: `OrderServiceType` members.**
- The enum already has `SeatAssignment`, `BaggageAllowance`, `Meal`, `LoungeAccess`, `Cip`, `SimCard`, …, `Priority`, `WiFi` and `ExtraSeat` (Ordering `Contracts/AeroTech.Messages/Ordering/Enums/OrderServiceType.cs`).
- Doc 03 §2 appends a generic `Ancillary` for every non-seat product and says nothing about the existing members.
- State the rule explicitly (Q8). Either:
  - use one generic `Ancillary`, with family and variant carrying the specifics and the existing members left unused; or
  - map to the existing members and append only the missing ones (pet, assisted travel, upgrade, purchased baggage).

**C8: forward compatibility of point of sale for the later seat Hold.**
- Ancillary `src/AeroTech.Ancillary.RestApi/V1/SeatMaps/Requests/HoldSeatSelectionsRequest.cs:11` requires `long PointOfSaleId`.
- Ordering has no `PointOfSaleId`. Its ReferenceData office read models carry only `PointOfSaleCountryId` and `PointOfSaleCityId`.
- Doc 03 §12 makes point of sale non-mandatory. A seat accepted in 6A without it cannot be held later unless the point of sale is re-derived.
- Decide now where point of sale comes from (the accepted seat quote or the sales office), or record it as a known Hold-stage gap.

**C9: A09, A17 and A19 are modelled before their wire contract exists.**
- Doc 16 requires typed value objects and persistence for:
  - A09, whose `extraSeats` array is always empty;
  - A17, whose medical fields are missing from Price and Resolve;
  - A19, whose guardian contact text is missing.
- The field names exist in Ancillary `CustomerSelectionContract`, but the Resolve wire shape does not. Column names and types may therefore change when AC-06 lands, which means a second migration.
- The owner has already required all 24 variants. Record the risk, and require the agent to take names and types only from `CustomerSelectionContract`.

**C10: report locations.**
- Doc 14 §1, §3 and §4 name `STAGE6A-SOURCE-DELTA.md`, `reports/STAGE6A-DOMAIN-FIELD-PARITY.md` and `reports/STAGE6A-INTEGRATION-EVIDENCE.md`.
- From 2026-10-28 the owner designates the repository-root `reports/` folder for reports and questions. Put all three files there explicitly.
- `CLAUDE.md` ("Do not create other docs/ or reports/ architecture artifacts") still says otherwise and needs the matching update (Q9).

**C11: delivery size.**
- The scope is several times that of Stage 5:
  - 145 indexed tests plus the per-variant negatives;
  - about 15 types and 3 enum members;
  - TPT tables, coverage child rows and a receipt table;
  - Create and Add on three surfaces;
  - a seat port.
- Split it into increments, each with its own exit verdict:

| Increment | Scope | Needed for live use |
|---|---|---|
| 6A.1 | Domain, pricing and Add for the general families | AC-02, AC-04 |
| 6A.2 | Create with general selections | AC-01 |
| 6A.3 | Seats A07/A08 | AC-03 |
| 6A.4 | Source-gated variants A09/A17/A19 | AC-06 |

## 4. Owner decisions required

| # | Question | Recommendation |
|---|---|---|
| Q1 | B1: how do Reserve and Cancel treat an Order that holds accepted ancillaries? | Both handled in 6A: Reserve rejects with a typed business error before any provider call, and Cancel cascades to the dependent ancillaries. |
| Q2 | B3: client idempotency key and receipt, or the existing lock plus `expectedCommercialVersion`? | The existing pattern for Add. The Create duplicate policy stays production gap 3. |
| Q3 | B2: request AC-02 and AC-04 from Ancillary before Ordering coding starts? | Yes. |
| Q4 | B4: push `6a7bb71` so the pack can be rebased on it? | Push, then rebase the pack. |
| Q5 | C6: an Amadeus shape for Create and Add on Api and OtaPanel, or a recorded exception? | Owner call. The Backoffice shape is fine as written. |
| Q6 | C4: which status for an expired selection: a new 410, or 409/422? | 410, matching Ancillary's own expiry error (16817). This would be the first 410 in `ExceptionFactory`. |
| Q7 | C2: make `TravellerId` nullable now, or once the catalog sells an Order-coverage product? | Verify the catalog first; amend §6.4 only if such a product is sellable now. |
| Q8 | C7: one generic `OrderServiceType.Ancillary`, or the existing members plus the missing ones? | Owner call. Either is consistent once it is written down. |
| Q9 | C5, C10: update CLAUDE.md (active authority, `reports/` folder and, if Q2 keeps the receipt, line 78)? | Yes, in the same change that activates Stage 6A. |

## 5. Not verified in this review

- Whether the current Ancillary catalog or seed contains any Order-coverage product.
- How the FlightFlow reservation capability treats a newly priced `OrderSeatService`, which has the key `FlightFlow` and its own Seat item.
- The `AncillaryPricingRate.UnitTotal` formula quoted in doc 06 §1. Only `IsUnitTotalComplete` at line 74 was read.
- The public benchmark URLs in doc 01.
