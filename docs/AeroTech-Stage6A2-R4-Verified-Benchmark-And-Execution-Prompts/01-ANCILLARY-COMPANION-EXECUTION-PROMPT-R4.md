# EXECUTION PROMPT R4-A — Ancillary: minimal trusted Resolve source contract for Ordering initial Create

## Mandate
Implement precisely the following contract corrections in `aliifarhadi/AeroTech.Ancillary` on `k8s-stg`. This task is a small **prerequisite** to Stage 6A.2 Ordering Create. You are NOT asked to benchmark or design; the architect has completed benchmark and decision memo `00-OWNER-ARCHITECTURE-DECISION-AND-BENCHMARK.md`. Read it, but the decisions in this prompt are executable. Before editing confirm repository HEAD; expected inspected baseline `c6e783bfbe989dfe0ce42a297e1e497c18b75949`. If different, produce targeted code-diff report and stop if the affected contracts changed incompatibly. Do not copy assumptions from old Stage6 packs in preference to live source.

This is a **source-side minimal contract amendment only**. No new shopping flows, reservations, seat holds, migrations, tables, enums, domain aggregates, cross-service HTTP calls or general engine changes.

## Source files already checked by architect
- `src/AeroTech.Ancillary.Domain/SelectionSnapshotAggregate/SelectionSnapshot.cs`: has `OfferId`, `OrderId`, `OrderCommercialVersion`, `ExpiresAtUtc`, `SourceKind`, and `Lines`.
- `src/AeroTech.Ancillary.Query/Shopping/AncillaryOffers/Dto/Service/ServiceResolvedSelectionsDto.cs`: currently only `OrderId`, `OrderCommercialVersion`, and per-family arrays.
- `.../AncillaryOffers/Queries/ResolveAncillarySelection/Service/ServiceResolvedSelectionsMapper.cs`: maps family lists, but loses source identity and expiry.
- `.../AncillaryOffers/Dto/Service/ServiceResolvedPricingDto.cs`: lacks completeness flag although `AncillaryPricingRate.IsUnitTotalComplete` exists.
- `.../SeatMaps/Dto/Service/ServiceSeatMapResolutionDto.cs`: has `OrderId`, `OrderCommercialVersion`, and `Seats`, but no source identity/count.
- `.../SeatMaps/Snapshots/SeatMapResolutionSnapshot.cs`: has `OrderId`, `OrderCommercialVersion`, `ExpiresAtUtc`, `Seats`, but no source kind/ref/count.
- `.../SeatMaps/Queries/ResolveSeatMapSelections/ResolveSeatMapSelectionsService.cs`: opens SelectionSnapshot then resolves each stored line by exact selected `PricingRateId`.
- `.../SeatMaps/Queries/ResolveSeatMapSelections/Service/ServiceSeatMapResolutionMapper.cs`: each `ServiceSeatRateDto` ALREADY supplies `IsUnitTotalComplete`, selected `UnitTotal`, price components, `PointOfSaleId` and expiry per seat.
- `.../Shopping/Engine/CoveragePlanner.cs`: `Journey` means every flight of every portion in the Offer context.
- `.../Shopping/Selections/SellableCandidate.cs`: hard-gates A17/A19, excludes `Profile=Seat` (A09) and multiple other variants. Do not alter this policy in R4-A.

## Exact public service Resolve changes, additive

### General Resolve header
In `ServiceResolvedSelectionsDto` add the following four **non-null, trusted fields** derived from `ResolvedSelections.Selection`, not the caller:
1. `ShoppingSourceKind SourceKind`: `resolved.Selection.SourceKind`.
2. `string SourceReference`: Offer snapshot -> `resolved.Selection.OfferId!` EXACT; Order snapshot -> `resolved.Selection.OrderId.Value.ToString(CultureInfo.InvariantCulture)` EXACT. No `offer:` or `order:` prefix, no client-supplied fallbacks.
3. `DateTimeOffset ExpiresAtUtc`: `resolved.Selection.ExpiresAtUtc`.
4. `int SelectedLineCount`: `resolved.Selection.Lines.Count`, including any profile that the mapper currently does not emit. This preserves fail-closed detection of silently dropped lines.
Retain existing `OrderId`, `OrderCommercialVersion` and all existing per-family arrays with their names and types. No duplicate header DTO, no new `Resolve` version or endpoint.

### General pricing
Add `bool IsUnitTotalComplete` to `ServiceResolvedPricingDto`, set exactly `selection.Rate.IsUnitTotalComplete` in mapper, with no hardcoded `true`, no `0` fallback, no price estimate. Preserve all existing `PricingId`, `PricingRateId`, `PricingVersion`, `Quantity`, `UnitAmount`, `TotalAmount`, taxes and fees.

### Seat Resolve header
Add the same `SourceKind`, `SourceReference`, `ExpiresAtUtc`, `SelectedLineCount` to `ServiceSeatMapResolutionDto` (keep existing fields), sourced from the opened `SelectionSnapshot` in `ResolveSeatMapSelectionsService`, flowing through `SeatMapResolutionSnapshot` and `ServiceSeatMapResolutionMapper`. `SelectedLineCount` is the count of ORIGINAL snapshot lines, not a count inferred from filtered/deserialized seats.
- Seat per-row `ExpiresAtUtc` continues unchanged; header must be at least as strong as the row.
- All seat rows MUST correspond one-to-one with original snapshot lines. Unexpected/general profile or undecodable seat ID must fail typed contract; never filter it out while claiming complete success.
- The rate returned for a seat is the SINGLE actually selected rate (`PricingRateId` from the snapshot), never an arbitrary catalog alternative.
- Existing `ServiceSeatRateDto.IsUnitTotalComplete` is authoritative; do not introduce an inconsistent second rate-flag calculation.

### Source and cardinality semantics
For both Resolve APIs, verify the `SelectionSnapshot` is active, unexpired, and has the source type encoded by the stored snapshot. A Create with OfferId MUST NOT treat an Order-scoped snapshot as an Offer-scoped one; Ordering also verifies exact OfferId.
- `General Resolve` must never succeed with `SelectedLineCount=N` but fewer than N mapped category rows without a clear typed rejection; A09 or unknown categories remain **BLOCKED_SOURCE** until their separate approved implementation.
- `Seat Resolve` must never claim a general selection snapshot as valid seat selection. Validate all lines before returning.
- Do not call `FlightFlow` or `Ordering` from Resolve. This is a source lookup and commercial quote contract, not physical seat or supplier-stock Hold.
- Do not add a 'ValidateForAcceptance' endpoint. The new `SeatMaps/Price` endpoint (2026-10-31) already creates an immutable selection snapshot id `s_...` to be used by Resolve.

## Test cases required (typed boundary and wire, not merely direct mapper unit)
T01 general offer snapshot: header has `SourceKind=Offer`, exact original OfferId, expiry and 1..N count; complete price true only when actual rate says so.
T02 general order snapshot: `SourceKind=Order`, decimal text SourceReference, OrderCommercialVersion matches; neither source can be confused.
T03 one complete zero-valued rate: `IsUnitTotalComplete=true` and zero amount; incomplete zero is false. Never infer truth from amount.
T04 2 or more selected lines in one general snapshot -> mapped arrays totals equal `SelectedLineCount`; correctly preserve per-type quantity and options.
T05 seat offer snapshot: exact source header, count, expiry, original selected physical seat ID, unique traveller/flight, selected price and POS; free seat only when catalog provides a genuine complete zero rate.
T06 two seats in one snapshot: count=2, both survive; seat source for another Offer rejected by Ordering (test source identity generated correctly here).
T07 seat order snapshot and cross-kind snapshots retain truthful source; wrong profile through seat Resolve fails closed.
T08 expired/retired/missing rate, definition or provision fails closed. No successful empty or partially populated response.
T09 existing shopping/price/resolve response fields remain backward compatible; no over-engineering.

## Delivery and stop rule
Produce code patch + tests + exact files changed + commit SHA + `dotnet` run/test commands/results. State `AC01` header, `AC02` price completeness and `AC03` seat header/count as PASS or BLOCKED. Do not claim live HTTP PASS without running it against your own service instance. If test environment/auth unavailable, report separately; do not simulate real HTTP with fixtures.

**Forbidden:** changing A09/A17/A19 sellability, designing per-type medical/UMNR selection contracts, making new persistence tables, modifying FlightFlow or Ordering, implementing 6B, auto-correcting NDC public schema based on guesses, rewriting Master documents in another repository.
