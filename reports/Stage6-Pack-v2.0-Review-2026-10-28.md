# Ordering–Ancillary Operational Integration Pack v2.0: review

| | |
|---|---|
| Date | 2026-10-28 |
| Pack reviewed | `docs/Ordering-Ancillary-Operational-Integration-Pack-v2.0/`: files 00–14, README and `reference/`. All 19 files match `SHA256-MANIFEST.json`. |
| Ordering | local `k8s-stg@6a7bb71` (not pushed); `origin/k8s-stg@cd50a2a` |
| Ancillary | `k8s-stg@190aa74`, same as `origin/k8s-stg` |
| Previous review | `reports/Stage6A-Pack-v1.1-Review-2026-10-28.md`. The same file is bundled as `reference/REVIEW-v1.1-2026-10-28.md`. |
| Verdict | **6A.0 and 6A.1 can go to the coding agent once N1–N3 and N5–N9 are fixed in the pack. 6B is not ready for coding (N4).** |

Every claim below was checked against the source on 2026-10-28.

## 1. Closure of the v1.1 review

| v1.1 item | v2.0 resolution | Status |
|---|---|---|
| B1: Reserve dead end and Cancel half-open | 6A.0 typed pre-provider guard on Reserve and Issue (2831); whole Cancel covers every active service; partial Cancel is dependency-aware and fails closed on overlap. | Closed |
| B2: no live flow possible | Separate verdicts `DOMAIN`, `MOCK_CONTRACT`, `LIVE_SOURCE` and `BLOCKED_SOURCE`; AC merge sequence. | Closed (wording issue, N8) |
| B3: client idempotency key | Removed. Add uses `IReservationLock` and `expectedCommercialVersion`; CLAUDE.md:78 kept. | Closed |
| B4: stale baseline | Ancillary pinned at `190aa74`; the agent must extend the local AncillaryContext; the stale Master snapshot was removed. | Closed |
| C1–C11 | All addressed in doc 14 §2. | Closed. N5 and N6 are new small field issues. |
| Q1–Q9 | Resolved inside the pack (doc 14 §3). Q6 chose 409 over 410, which is consistent with "do not add a first 410". | Accepted |

**Erratum to my v1.1 review.** B1's correction said "pricing reversal follows the existing Stage-5 reversal rule". That was wrong. `Order.Cancel` (`src/AeroTech.Ordering.Domain/OrderAggregate/Order.Cancellation.cs:35-73`) only marks services and items as Cancelled. It writes no reversal lines and does not call `ReconcileTotals`. Pack v2.0 doc 07 §2 R3 states this correctly.

## 2. Pack claims verified as correct

- `IReservationLock.AcquireAsync(long orderId, …)` exists and is used by Reserve, Confirm, Release, Cancel, Issue, Void and the deadline job.
- These all exist at the paths the pack names:
  - `CancelOrderService`;
  - `IssueOrderService`;
  - `IssuancePlanner`;
  - `FlightFlowReservationProvider`, whose `PlanUnits` attaches `OrderSeatService` to its Air unit at lines 388–442;
  - `BatchResultMode`, with FlightFlow set to `AtomicAllOrNothing`.
- `Stage5TypeSetConformanceTests` uses `private const int Stage = 5`, so a numeric `.canonical-stage = 6` is correct.
- The highest local error code is 2823, so 2824–2834 are free today.
- The Ancillary general Hold request has no `Pets` or `ExtraSeats` member (`HoldAncillaryServicesRequest.cs:12-18`).
- `SelectionSnapshot` persists `OfferId` and `ExpiresAtUtc` and exposes `Lines`. AC-01 is therefore implementable from data Ancillary already stores.
- `OrderServiceFacts`, `ExistingAncillaryServiceFacts` and `OrderShoppingContextMapper` exist in Ancillary.

## 3. Remaining findings

### N1: A09/A17/A19 persistence is contradictory (blocks the 6A.1 and 6A.4 test plans)

- **No columns now:**
  - doc 03 §8: "do not … create placeholder tables purely for missing live Wire";
  - doc 09 §4: "do not hard-wire an arbitrary extra SQL table/column name before an actual source type exists";
  - doc 12 6A.4: extend the typed path "only after source AC-06 landed".
- **SQL roundtrip now:**
  - doc 12 6A.1: "Add all A01–A24 fixtures into Domain tests";
  - doc 14 §5.1: V-Axx-CREATE/ADD for all 24 variants assert the selected option "after SQL roundtrip".

For A09, A17 and A19, the agent cannot satisfy both. Choose one:
- **(a)** Domain value objects and in-memory tests now; their SQL roundtrip is `BLOCKED_SOURCE` until AC-06.
- **(b)** Persist now, with names and types taken from `CustomerSelectionContract`.

Recommendation: (a). It matches the "no column before source" rule and avoids a second migration.

### N2: there is no mechanism to keep 6A out of production sale

- Doc 12 says: "Do not leave an apparently functional 6A endpoint enabled for a non-reservable/undocumentable product in airline production."
- After 6A, every Order with an accepted ancillary or a new priced seat is refused by Reserve (D10, SAFE-03B), so it can never be ticketed.
- The repo has no feature-flag mechanism: there are no `FeatureManagement` or `IsEnabled` usages in `src/`.
- The pack does not say how the endpoints are kept off, so the agent would have to invent it.

Choose one:
- **(a)** An environment gate: 6A stays on `k8s-stg` and is not promoted until 6B is verified for at least one product. No code.
- **(b)** A required, fail-fast configuration setting per surface that rejects `selectedAncillaries` and Add. This follows CLAUDE.md's no-default-config rule.

Recommendation: (a).

### N3: the SAFE-03B seat predicate is not defined

Doc 07 R1 and doc 12 6A.0 block Reserve for a new priced A07/A08 seat "unless source explicitly proves no additional commercial Hold or DocumentRequired". The facts that rule depends on would come from the AC-03 quote, which does not exist yet, so no test can define or verify the exception.

Recommendation: in 6A, Reserve rejects every Order holding a new priced seat, with no exception. 6B defines the exception from the real AC-03 fields.

### N4: 6B is not ready for coding

- **The EMD aggregate does not exist in code.**
  - Master §17 (line 1594) defines `ElectronicMiscDocument` field by field.
  - `src/` has no `ElectronicMiscDocument` class; only enums exist (`ElectronicMiscDocumentStatus`, `ElectronicMiscDocumentType`, `AccountableDocumentKind` and others in `Contracts/.../Ordering/Enums`).
  - 6B would therefore materialize a new root together with its document stock and issuer.
- **The flow contracts required before coding are missing.** `docs/NEXT-CHAT-PROMPT-Ordering-Stage6-Ancillary-EMD-Domain-Closure.md` §D requires each flow to define:
  - its starting state;
  - its commit pivot;
  - its Unknown and recovery behavior;
  - who owns the final state.

  Doc 07 §5 lists scenarios but defines none of these.
- **Seat inventory ownership is open.** FlightFlow holds the physical seat, and Ancillary `SeatMaps/Holds` holds a commercial seat. Doc 06 §5 and doc 07 §3 defer this to an audit.
- **The Ancillary provider capability matrix** per family and per operation is not source-verified.

Recommendation: remove the 6B section from doc 12. Close the 6B domain and flows in a separate pack before any 6B coding prompt, following the stage-closure rule.

### N5: field name of the definition version

Doc 03 §4 ("Resolve `definition.id`") and doc 05 §2 ("real `DefinitionId`") use the wrong name. The Ancillary field is `DefinitionVersionId` (`ServiceResolvedDefinitionDto.cs`).

### N6: `PointOfSaleId` on `OrderAncillaryService`

- Doc 03 §4 lists `PointOfSaleId: long?` on `OrderAncillaryService`, "required for quoted seat purchase".
- But seats are `OrderSeatService`, which carries `AcceptedPointOfSaleId` (doc 03 §7).
- The general Hold request has no point-of-sale field.

Remove the field from `OrderAncillaryService`, or state which non-seat product needs it.

### N7: choices still left to the agent

Each of these needs a fixed rule in the pack:

1. Doc 04 §4: the Api/OtaPanel Add response returns `services[]` "if the chosen external facade exposes it".
2. Doc 04 §4: "If the local repository has an existing Amadeus-shaped service reference DTO, reuse it". Either name the DTO or state that none exists.
3. Doc 07 R2: ancillary-only cancellation is allowed "if the product can be ended independently". The predicate is undefined.
4. Doc 08 §2: `acceptsChanges` requires that the "source can honestly price Add". Ordering cannot evaluate that. Restrict the predicate to Ordering facts.

### N8: what AC-02 and AC-04 unblock

Doc 11 §8.1 and the doc 12 6A.1 heading say that AC-02 + AC-04 "unblock fixtures" / "Domain/fixture integration". Fixtures need no source at all, and live Add needs AC-01, AC-02, AC-04 and AC-05 (doc 14 §4 states this correctly). Align the two documents with doc 14.

### N9: AC-01 wording on `SourceKind`

Ancillary derives `SourceKind` from `OrderId` (`SelectionSnapshot.cs:54`); it is not a persisted column. AC-01 should say "derived from the persisted `OrderId`/`OfferId`".

## 4. Owner decisions required

| # | Question | Recommendation |
|---|---|---|
| D1 | N1: A09/A17/A19 domain-only now, or persisted now? | Domain-only now; SQL roundtrip is `BLOCKED_SOURCE` until AC-06. |
| D2 | N2: how is 6A kept from production sale before 6B? | An environment gate with no code: `k8s-stg` only until 6B is verified. |
| D3 | N3: is a new priced seat reservable in 6A? | No. Reserve rejects it until 6B. |
| D4 | N4: keep 6B in this coding prompt? | No. Run a separate 6B domain and flow closure first. |

## 5. Not verified in this review

- The public benchmark URLs in doc 02.
- Whether the current Ancillary catalog sells any Order-coverage product. The pack also marks this as unverified.
- Runtime behavior. No build or test was run for this review; it is a document-and-source review only.
