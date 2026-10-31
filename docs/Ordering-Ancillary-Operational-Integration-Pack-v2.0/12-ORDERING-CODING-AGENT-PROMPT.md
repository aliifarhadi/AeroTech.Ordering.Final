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
- **Superseded for Create by decision D14 (2026-10-31):** Create takes two optional strings, `ancillaryPricedSelectionId` and `seatPricedSelectionId`, on all three surfaces. Add keeps its own contract. Original text: Order Create accepts an optional canonical `selectedAncillaries {selectionId?, seatSelectionIds?}` on Backoffice; Api/OtaPanel use an Amadeus/IATA-inspired external `services[]` adapter; retain old required Create and response fields. No duplicate Create service. Existing legacy `SeatSelections` cannot be combined with new seat IDs; retain existing legacy behavior for existing callers except clearly defective identity with dedicated tests.
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
- Superseded by decision D14 (2026-10-31): the seat quote is `SeatMaps/Price` plus `SeatMaps/Resolve {selectionId}`, and `ValidateForAcceptance` is not built. Original text: `SeatMaps/Resolve` currently doesn't prove a unique final rate or POS. Require actual `ValidateForAcceptance` **proposed** source Handoff; no chosen arbitrary rate, estimated/zero price, or fake seat inventory. **No physical Hold in 6A**; FlightFlow remains physical owner.
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
