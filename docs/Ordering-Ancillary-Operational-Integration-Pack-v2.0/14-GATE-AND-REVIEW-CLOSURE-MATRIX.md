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
