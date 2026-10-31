# EXECUTION PROMPT R4-B — Ordering Stage 6A.2: one atomic initial commercial Order

## 0. Authority, baseline, gates
Work ONLY in `aliifarhadi/AeroTech.Ordering.Final`, branch `stage6a-ancillary-commercial`. Inspected baseline `07b715859f9f20806eed0d371d31ef841505492a`. Read in order:
1. `CLAUDE.md` authority section;
2. `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md` (especially §§2,4,6.3-6.7.1,8,9.1,27.8, Stage6A roadmap);
3. `docs/Ordering-Ancillary-Operational-Integration-Pack-v2.0/` (especially 03/04/05/09/12);
4. `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md`, handoff ledger OR-005;
5. consultant memo `00-OWNER-ARCHITECTURE-DECISION-AND-BENCHMARK.md` and prerequisite `01-ANCILLARY-COMPANION-EXECUTION-PROMPT-R4.md`.

The architecture and official benchmark in 00 are **already completed**; do not treat benchmarking or architecture selection as a task for this agent. Do not invent alternative NDC semantics or requirements. If `HEAD` differs, inspect and reconcile changed source paths before editing; if domain contract conflict is genuinely unresolved, return exact file/line issue and stop that change. Do not simply declare the entire stage blocked because the Ancillary prerequisite is pending: implementation and fixture tests can proceed, but label LIVE_HTTP gate blocked until the companion lands.

Scope = commercial **Create Order** from an already accepted Flight Offer, optionally enriched by general + seat priced selection snapshots. Do NOT implement Reserve, Hold, Confirm, Issue, EMD, Cancel, OrderId shopping or a new payment rail. No new aggregate roots, giant DTO, new endpoint version, database schema or speculative fields. Do not call `AddAncillaries` after Create.

## 1. Exactly required public input shape

Preserve existing Create route, required fields, validations and existing response for all three surfaces:
- `POST /Backoffice/v1/Orders/FlightOffers`
- `POST /Api/v1/Bookings/FlightOffers`
- `POST /OtaPanel/v1/Bookings/FlightOffers`

In `src/AeroTech.Ordering.RestApi/V1/OrderAggregate/Requests/CreateOrderFromOfferRequests.cs`, add these two optional JSON properties to both Backoffice and Agency request records:
- `string? AncillaryPricedSelectionId` -> JSON `ancillaryPricedSelectionId`, e.g. `s_...`, one **general multi-line priced selection snapshot**; null/omitted means none.
- `string? SeatPricedSelectionId` -> JSON `seatPricedSelectionId`, e.g. `s_...`, one **SeatMaps Price snapshot that can contain N seats**; null/omitted means none.

These are AeroTech source adapter names, NOT official IATA public NDC field names. **Do not** call the seat snapshot `SeatSelectionId` (this is an individual seat id), do not add `SeatSelectionIds[]`, do not use source `OfferItemIds[]` as unverified-priced selections, do not add misleading `services[]` objects whose members are whole snapshots. Keep `OfferId`, `Travelers`, `Contact`, legacy `SeatSelections`, `Remarks`, `CustomerId` semantics unchanged. Existing air-only Create payloads must remain valid. Adding nullable arguments to positional records must preserve existing callers (nullable default at end or equivalent proven-compatible design).

Propagate both properties through all 3 command types, `IAgencyCreateOrderCommand`, agency/backoffice argument mappers, `CreateOrderArgs` (source IDs are allowed here as request arguments, not domain truth), validators and controller endpoints. Use only existing per-surface adapters; no different business behavior per channel.

**Legacy seat conflict:** if `SeatSelections` has any legacy selections and `SeatPricedSelectionId` is nonempty, fail consistently for all three surfaces with NEW typed error code **2866** and HTTP **422**; guard in the domain Create (validation may also catch earlier). Old seat-only calls remain unchanged. Verify 2866 is unused in current source before registering; if occupied, stop and report, never silently choose a different code.

Example showing only relevant fields (all existing required fields must still be supplied):
```
{"offerId":"<exact FlightOffer OfferId>","ancillaryPricedSelectionId":"s_<general>","seatPricedSelectionId":"s_<seats>","contact":{...},"travelers":[...]}
```

## 2. Orchestration (one acceptance, no second change)

In `CreateOrderFromOfferService.ExecuteAsync`:
1. Validate request, including legacy seat collision. Fetch the authoritative Flight Offer details through existing `IOfferProvider.GetByOfferIdAsync(args.OfferId)`; do not claim FlightFlow owns these OfferDetails (AirOffer/FlightOffer provider does).
2. If `AncillaryPricedSelectionId` is present, call existing `IAncillaryProvider.ResolveSelectionAsync` exactly once using `POST v1/AncillaryOffers/Resolve` with `{selectionId}`. Reject null/malformed/empty/partial result; do not fallback to catalog.
3. If `SeatPricedSelectionId` is present, call new `IAncillaryProvider.ResolveSeatSelectionAsync` (name may follow local established convention) using `POST v1/Ancillaries/SeatMaps/Resolve` with `{selectionId}`. It accepts **one snapshot id**, may return 1..N seat rows. Do not invoke the old raw SeatMaps/Resolve route with a list of raw seat IDs and do not invent ValidateForAcceptance.
4. Pass the fully mapped optional source responses and existing `OfferDetail` into the **single** `Order.Create` domain factory, validating before command persistence. Never call `AddAncillaries` inside/after Create; never increment `CommercialVersion` beyond 1. No new `OrderChange(AddProduct)`.
5. Persist one Order, project `ProjectCreatedAsync` from the completed aggregate and SaveChanges once, following existing repository pattern. Any source or domain failure before SaveChanges must result in no new Order and no read projection. Do not add distributed transaction, saga, receipt table or new idempotency semantics to solve this source-read-only phase.

## 3. Source authenticity and completeness checks INSIDE domain

The newly resolved general/seat sources are not trustworthy until checked against the Offer and Order domain:
- `SourceKind=Offer`, `SourceReference` ordinal-equals **original** `args.OfferId` (not normalized/truncated/prefix added), `OrderCommercialVersion=null` for an Offer-based selection; snapshot and every selected row are unexpired against the same Create clock.
- General `SelectedLineCount>0`, equals `Lines.Count`, and each line carries a unique `OfferItemId`. Seat `SelectedLineCount>0`, equals `Seats.Count`, unique per seat selection id and no duplicate traveller+flight or flight+seat.
- Every selected traveller maps exactly to source Offer traveller (`SourceTravellerRef`) and then existing `OrderTraveller`, not client index as the domain identifier. Flight/bound IDs must match Offer itinerary; invalid affiliation rejected with a **typed** error, never redirected to another traveller or flight.
- Definition, provision and chosen rate correspond to exactly the selected `VariantCode` and family, validated with existing registry and typed constructors. Reject unsupported/unknown variant rather than changing enum or coercing to generic service.
- `IsUnitTotalComplete=true`, quantities agree with selection and Pricing, pricing references are nonzero, price components reconcile with base and total using **existing `AcceptedPriceComponents`**; zero-price is valid ONLY when rate explicitly says complete, never inferred from 'free' product name. Enforce same currency as Offer/Order; no synthetic FX, rounded weight/unit or invented fee.
- Service source facts (`AncillarySaleSource`) preserve `SourceKind`, original OfferId, commercial version, **whole snapshot `s_` id** and exact per-item OfferItemId. No internal source IDs in the external Create response.
- Any unknown profile or hidden/empty category in resolved arrays must trigger the selected count mismatch or a typed failure. Never silently skip an item. No success from a fixture response labeled as live HTTP.

## 4. Existing domain planner reuse and initial-sale bookkeeping

Update `Order.Create` signature with optional **trusted resolved-domain quotes** (or add a small overload while keeping existing caller compatibility). Sequence MUST be:
1. Validate Offer traveller set.
2. Construct Order and `RecordCreateChange`: exactly one `OrderChangeType.Create`, `CommercialVersion=1`, existing actor and OfferId source reference.
3. Build Contact, Travellers and Itinerary.
4. Build AirFare `OrderItem`/`OrderAirTransportService` and existing legacy seats as before.
5. Build fare pricing units and flight price lines with existing `OrderPricingReason.InitialSale`.
6. Validate each of general and seat resolved sources, using existing `EnsureSelectionIsCurrent` / `EnsureSeatQuoteIsCurrent` semantics with `ShoppingSourceKind.Offer` and exact `args.OfferId`.
7. Prepare all chosen purchases in one local `List<PlannedPurchase>` with existing private `PlanAncillaries(..., OrderPricingReason.InitialSale, ...)` and `PlanSeats(..., OrderPricingReason.InitialSale, ...)`. These already validate family rules, seat conflict inside the Order, beneficiary coverage and price completeness. **Plan all before Commit**. If either plan fails, Create throws; nothing persisted.
8. `Commit(purchases)` exactly once to this fresh Order, then existing `BuildRemarks` (if not done earlier), `ReconcileTotals()` once and `RaiseCreated(...)` once.

Do not rewrite `OrderItem`, `OrderService`, `OrderPricingLine`, sale history, event schema or `Order.Change` model. `OrderCreated` is a flat event today; do not claim it embeds the entire Order snapshot. The read projection **is** the complete persisted Order snapshot and must display source service details and accurate total.

## 5. Seat quote adapter — precise mappings

In `Domain/Providers/Ancillary/ResolvedAncillarySelection.cs` add to existing `ResolvedSeatQuote`:
- `string SelectionId` (whole `s_` snapshot); and
- `int SelectedLineCount` (from source seat Resolve header).
Keep existing `SourceKind`, `SourceReference`, `OrderCommercialVersion`, `ExpiresAt` and `Seats[]`.

Per seat `ResolvedSeat.SeatSelectionId` remains the exact source seat id (`ServiceResolvedSeatDto.Id`). Do not replace it with `s_`.

**Critical source identity correction** in `Order.Ancillaries.PlanSeats`: create `AncillarySaleSource` with `SelectionId=quote.SelectionId` (whole `s_`), `OfferItemId=seat.SeatSelectionId` (exact original seat ID), and unchanged source kind/ref/version. Existing code uses per-seat ID for `SelectionId` and `OfferItemId=null`, losing audit lineage. Preserve seat-specific id for future hold; one snapshot may cover several seats.

Provider wire from `Service/v1/Ancillaries/SeatMaps/Resolve`:
- Header: `SourceKind`, `SourceReference`, `OrderId`, `OrderCommercialVersion`, `ExpiresAtUtc`, `SelectedLineCount`, `Seats[]`.
- Per seat: `Id`, `TravellerRef`, `FlightId`, `SeatNumber`, `PointOfSaleId`, `ExpiresAtUtc`, `ServiceDefinition`, `Provision`, `Pricing`.
- `ServiceDefinition.Id -> ResolvedAncillaryDefinition.DefinitionVersionId`, `ServiceDefinition.ServiceDefinitionRef`, `VariantCode`, `ServiceTypeCode`, `ServiceSubCode`, `Document.Rfic`, `Document.Rfisc`, `Booking.SsrCode`.
- `Provision.Id -> ProvisionId`; `CoverageScope`, `PurchaseStage`, `Booking.Method`, `Outcome.BookingRequired`, `Booking.ConfirmationRequirement`, `Fulfillment.FulfillmentProviderKey`, `Document.Routing`, `Document.Type`, `Outcome.DocumentRequired`; compute `RequiresCheckAtReserve` using the **exact verified source formula** `provision.Availability.MustCheckAvailability || definition.Booking.ConfirmationRequirement == ConfirmationRequirement.SubjectToConfirmation` for a Seat definition (source: `Ancillary.Shopping/Engine/AvailabilityAssessor.cs:205-207`; no PetRule applies to Seat). The wire carries `Provision.Availability.MustCheckAvailability` and `ServiceDefinition.Booking.ConfirmationRequirement`. Do NOT default or infer any other Boolean.
- `Pricing.Id -> PricingId`; `Pricing.Version -> PricingVersion`; `Pricing.Rate.Id -> PricingRateId`; `Pricing.Rate.CurrencyId`, `BaseAmount`, `UnitTotal -> UnitAmount=TotalAmount`; `Quantity=1` (exact source SingleSeat); `Pricing.Rate.IsUnitTotalComplete`; `Pricing.PricingUnit` existing enum; Tax/Fee split from each `Rate.Components[].Category`; preserve country, station, amount, tax treatment and fee application unit, with component currency = rate currency as source invariant.
- Enum wire values often arrive `{value,name,title}`, not C# enum strings. Use a dedicated narrow wire DTO+converter consistent with existing Provider code; do not serialize arbitrary object to string and guess. Reject unrecognized enum.
- `PointOfSaleId`, selected single rate and zero price when complete must be preserved. Map retired/missing source returns to existing typed exceptions; only add new error when existing semantics cannot express it.
- Check `ExpiresAtUtc` at both header and seat level. No seat occupancy claim or FlightFlow call in this adapter.

IMPORTANT: `SourceKind=Offer` is required for Create. Order-scoped seat Price snapshots remain for the **existing** Add path and are not accepted at initial Create. Preserve Add behavior.

## 6. Journey (C4) — concrete Master amendment and domain change

Actual verified source: `Ancillary.Shopping.Engine.CoveragePlanner.Units` sets `ServiceCoverageScope.Journey` to all flights in all `ShoppingPortion` objects of the Offer, once for each source traveller. `SelectionScopes.Of` maps Journey to `SelectionScope.PerTraveller`; Resolve wire `Coverage.TravellerRefs=[travellerRef]`, `Coverage.PortionRefs=[]`, `Coverage.FlightRefs=[]`, `Unit.TravellerRef=travellerRef`, `Unit.FlightRef=null`, `Unit.PortionRef=null`. `CoverageRef=JOURNEY` is INTERNAL and **not on wire**; do NOT require it in Ordering.

The current Ordering `JourneyAirServicesOf` rejects empty flight refs; modify **only the Offer-scoped path** (`sourceKind=Offer`) after the snapshot has been validated against exact `OfferId`:
- require `SelectionScope=PerTraveller`, `Provision.CoverageScope=Journey`, one valid `Coverage.TravellerRef`, null unit flight/portion refs, zero coverage portion refs and no contradictory flight refs;
- use the newly created Order's entire accepted Offer itinerary for that specific traveller; collect every matching **active** `OrderAirTransportService` exactly once, in existing journey/segment sequence; reject empty/duplicate/missing service or mismatch with accepted Offer itinerary, never invent from array ordering;
- persist exactly one `OrderAncillaryService` with `CoveredAirServiceIds` spanning those services and one commercial quantity/price (do not split into one item per flight).
- leave Order-scoped Add's existing requirement for explicitly named flight refs unchanged; no inferred all-flight expansion for Order source.
- for `PerTravellerPerFlight`, `PerTravellerPerPortion`, `PerOrder`, keep current strict matching rules.

**Authority correction must land in the same Ordering commit**: in Master §6.7 `CoveredAirServiceIds` row replace outdated blanket `Journey = exactly the flights the source names (refused while the source names none)` with source-specific rule: Offer-scoped Journey = all air services of same source traveller from the exact accepted Offer (as contractually defined by current Ancillary Shopping); Order-scoped Journey continues requiring explicit authoritative coverage. Add date and verified `CoveragePlanner` reference; update pack doc 05 matching row, existing decision ledger as D13 and `CLAUDE.md` authority paragraph in place. Do not write a competing Master doc.

## 7. Naming (C5), authority and compatibility

Adopt **only** optional external Create properties `ancillaryPricedSelectionId` and `seatPricedSelectionId`. Existing general Add contracts do not change. Record the dated D14 decision in existing `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md`, update pack docs 03/04/12 references for Create only, supersede older `selectedAncillaries`/`services[]` Create prose and **old `seatSelectionIds` arrays** for this new initial-price flow. This change is an internal AeroTech adapter convention, NOT an IATA field rename. Do not refactor `OrderItem`, `OfferId`, `OrderSeatService`, or other existing domain names.

Existing Stage 6A D1 still applies: A09/A17/A19 source incomplete; no SQL mapping for A09, medical/UMNR choice not persisted. No random migration or forced source-un-gating. The source enables only what it genuinely prices.

## 8. Exact verification matrix — not a claim of universal live availability

G01 Air-only Create unchanged all surfaces, legacy seats without new fields unchanged.
G02 General priced selection only: ≥2 variant lines with different families, one Order/Create change/v1, exact quantity/selected options, accepted item total, source IDs.
G03 Seat priced selection only: two travellers on two flights; each seat is `OrderSeatService`/standalone Seat Item with selected rate, point of sale and source snapshot/per-seat references. Validate same-flight duplicate seats and traveller double-booking within a single Order.
G04 General + Seat + AirFare in same initial Create: no intermediate `AddProduct`; total reconciles against accepted source flight + service amounts; tax included exactly once, eligible fee applied exactly once, no synthetic FX.
G05 A07 or A08 with **actual** complete-zero rate if such a catalog fixture exists; if not, fixture-only + live `NOT_TESTABLE_CATALOG`, not invented free seat.
G06 Wrong OfferId for general or seat, expired snapshot, zero/missing lines, incorrect count, stale rate, wrong traveller/flight, unknown or duplicate variants, incomplete price, currency mismatch => typed rejection and no Order persistence.
G07 Coverage examples: 2 flights in one Portion yields **one** purchased service covering two air services; Journey over two bounds resolves exactly all that traveller's flights for Offer source; Order scope one service with `TravellerId=null` only where catalog legally allows (A22/A24 currently), no first-traveller allocation.
G08 Family-specific persisted/reloaded choice fidelity from trusted source fixtures for each A01-A24 with actual family object and domain contract. A09/A17/A19 remain `BLOCKED_SOURCE` / `BLOCKED_PERSISTENCE` until owner-approved AC06. Do not fabricate SQL PASS. Matrix must distinguish Domain Fixture PASS, SQL Roundtrip PASS, Source Price PASS, Live HTTP PASS and Provider Fulfillment NOT_IN_SCOPE separately per Axx.
G09 Create REST three surfaces, API serialization, no leaking provider DefinitionVersionId/PricingRateId/PointOfSaleId/internal refs into public payloads beyond source selection IDs intentionally accepted.
G10 `OrderCreated` remains single event with accurate `CustomerTotal`; `ProjectCreatedAsync` projects all accepted services, price lines and seat items; SQL reload matches snapshot.
G11 Legacy `SeatSelections` plus priced seat snapshot rejected using code 2866 HTTP 422 across all surfaces.
G12 Existing `AddAncillaries` remains unchanged and its commercial version behavior still passes.
G13 `EnsureFulfillmentIsSupported` remains active: ancillary/priced seat prevents Reserve/Confirm/Issue until 6B; do not claim live fulfillment. `Order.Status=Created` does NOT mean seat reserved.
G14 End-to-end service-backed API test must use newly priced OfferId and freshly issued real `s_` tokens from Ancillary on supported surface, then Ordering Create and actual order details. Record path, test fixture, commit, response/status, persisted rows, and total; sanitized logs. If credentials/active catalog inaccessible, mark LIVE_HTTP=BLOCKED_ENVIRONMENT; never substitute a fake and label it success.

## 9. Scope gates: 24 variants and no overengineering

Review all 24 `AncillaryVariant.All` against `00-...` matrix. Do not delete A09, A17, A19. No surrogate sale of rejected product, no hardcoded zero or added `null` medical/guardian fields to simulate a complete selection. No direct general Add in Create; no `ValidateForAcceptance`, new journey DTO, new per-variant HTTP endpoint or new service aggregate. Do not assume 24/24 source sellable. Require a specific Owner approval *later* to remove current source gates or persist currently unsupported A09/A17/A19.

## 10. Output required from agent

At the end produce:
- baseline and final SHA, source files changed, exact diff summary;
- conformance build/test commands and results, SQL roundtrip status;
- all G01-G14 evidence per gate, plus A01-A24 explicit statuses;
- proof of one initial Order/Create Change/v1 with AirFare+selected general and seats, selected money and read model;
- AC01/AC02/AC03 source-contract HTTP availability, any unresolved typed blocker with FILE + LINE and OWNER_DECISION_REQUIRED where appropriate;
- documented Master/pack D13/D14 in-place amendments;
- statement that FlightFlow OR-006 seat uniqueness is not 6A.2 and is not falsely claimed solved.

Execution permission: **Ordering only**. Ancillary prerequisite is a separate prompt in `01-...`; if that team has not delivered it, proceed with local fixture-based domain/application correctness but mark live source gate blocked and never bypass source integrity. No unapproved cross-repository writes.
