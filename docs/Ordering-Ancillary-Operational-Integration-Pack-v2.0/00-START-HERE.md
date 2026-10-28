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
