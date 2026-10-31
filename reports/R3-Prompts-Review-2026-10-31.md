# Review: R3 prompts for Create with all ancillaries

Date: 2026-10-31. Reviewed `docs/01-ORDERING-CREATE-ALL-ANCILLARIES-AGENT-PROMPT-R3.md` and `docs/02-ANCILLARY-COMPANION-AGENT-PROMPT-R3.md`. No code was changed.

| Repository | State |
|---|---|
| Ordering | `stage6a-ancillary-commercial@07b7158` |
| Ancillary | `k8s-stg@c6e783b`, equal to origin after a fresh fetch |
| FlightFlow | `k8s-stg@962f4188`, equal to origin |
| AirOffer | `AirAvail@e87136e` |

## 1. Verdict

R3 is sound and better than the previous prompt. Its Journey rule (C4) is correct and replaces my rejection proposal. Eight points need fixing before coding. F1 is a false statement about the seat lifecycle, F3 is an authority conflict that would make C4 illegal as written, and F4 means the task as defined can never reach COMPLETE.

## 2. Confirmed

| R3 claim | Evidence |
|---|---|
| Journey covers every flight of the Offer | `CoveragePlanner.cs`: `Journey` builds one unit per traveller over `all`, the flights of all portions. `Portions = offer.Bounds.Select(Portion)` in `FlightOfferShoppingContextMapper`. `SelectionScopes.Of(Journey)` is `PerTraveller`. |
| A line's scope equals its provision's scope | `SelectionResolver.Resolve` refuses a mismatch with 16820 |
| AC-01, AC-02 and the seat source header are missing | `ServiceResolvedSelectionsDto`, `ServiceResolvedPricingDto`, `ServiceSeatMapResolutionDto` |
| Two seat ids map cleanly | `AncillarySaleSource` columns are 1024 long. The property names are `SelectionId` and `OfferItemId`; `Source...` are column names. |
| Family rules exist for the negative tests | `EnsurePresent`, `EnsureValid` and `MissingChoice` in the family constructors: bag ref, measured weight, dimensions, equipment, meal codes, pet choices, SSR, infant and guardian, opt-in |
| Read model holds item prices | `OrderItemReadModel.AcceptedTotal`, projected by the shared upsert |
| Exactly 24 variants, nine profiles | `AncillaryVariant` registry |

## 3. Fixes required

| # | R3 text | Finding | Required fix |
|---|---|---|---|
| F1 | C1: "physical occupation is rechecked later by FlightFlow during Hold" | **False.** FlightFlow's hold path has no seat guard (OR-006 still Requested; `NotContainsAircraftSeatNumbersAsync` has no caller). Ancillary's seat Hold does not read occupancy either: `SeatSelectionVerifier.cs:147` passes `Occupied = false`, and `IOccupiedSeatProvider` is used only by `GetSeatMapService`. No unique index exists on flight and seat; `AncillaryReservation.cs:294` checks one reservation only. The guide's §4.4 sentence is wrong on this point, and my earlier report repeated it. | Replace with: occupancy is read once, non-atomically, at Price. Nothing rechecks it afterwards until OR-006 lands. Record the Ancillary gap with OR-006 for 6B. |
| F2 | C4, D7.4, G08: verify `CoverageRef=JOURNEY` | `CoverageRef` is not on the wire. `TravellerUnitDto` carries only `travellerRef`. R3 also forbids adding AC-05 fields. | Prove it from `Provision.CoverageScope=Journey`, `SelectionScope=PerTraveller` and a unit with no portion or flight. `SelectionResolver` makes these equivalent. Apply to `SourceKind=Offer` only; Order-source stays refused. |
| F3 | D9: avoid changing Master, record one dated delta | Master line 423 says Journey is "exactly the flights the source names (refused while the source names none)", and pack doc 05 line 28 says do not guess every flight. `CLAUDE.md` says Master wins and that Stage decisions merge into it in place. R3 is not named as an authority. | Amend that one Master line in place, and add D13 plus one sentence in the `CLAUDE.md` authority section. |
| F4 | A1 and D12: not COMPLETE until the requested scope is shown; 24 variants | 24/24 live is impossible at source, see section 5. The task can never close. | Define closure as: every code has a status row, every variant sellable pre-order on the catalog is live-proven, the rest carry a named blocker. |
| F5 | D2: snapshot line count must equal returned items | Seat Resolve has no count and prompt 02 item 3 does not add one. Ancillary maps every line or throws, but Ordering cannot prove that. | Add `SelectedLineCount` to the seat header in prompt 02 and to `ResolvedSeatQuote`. |
| F6 | D5: "existing suitable typed error" for legacy plus new seats | No existing code fits (`SeatSelectionBoundIsNotInOrder` and similar mean other things). | New code 2866, HTTP 422, matching G11. |
| F7 | G04: free A08 | Nothing shows a free A08 exists. The gate should test completeness, not a variant. | "A07 or A08 with a verified complete zero rate". Live status depends on the catalog. |
| F8 | D8, Part F | D8 names a new handoff file, but `CLAUDE.md` puts handoffs in the ledger. `03-NEXT-CHAT-CONTINUATION-PROMPT.md` is not on disk. Part F names `02-...PROMPT.md`, while the file is `02-...PROMPT-R3.md`. | Update OR-005 instead of a new file. Supply prompt 03 or remove the reference, and fix the name. |

## 4. Benchmark verification

R3's own rule requires inaccessible sources to be marked source-access limited, so the E1 labels on BM-02, BM-03, BM-07 and BM-12 cannot stand yet.

| ID | Result | Note |
|---|---|---|
| BM-01 IATA guide | Confirmed | Text extracted from the PDF. OrderCreate "contains the details of a selected Offer"; "Payment information may or may not be included". `ALaCarteOfferItem/OfferItemID` appears; `OfferItemRefID` does not. |
| BM-02, BM-03 Condor | Not verifiable | HTTP 401. The naming cross-check rests on these pages. |
| BM-04 Breeze create | Partly | `passengerIds` is nested in `offerItemSelections`. Fare + seat + bag is in prose; the shown request is fare + bag. |
| BM-05 Breeze ancillaries | Confirmed | Rows scoped to segment, journey or leg |
| BM-06 Amadeus | Confirmed | The repository was archived on 2026-07-17 |
| BM-07 Sabre | Not verifiable | The page is an empty client-rendered shell |
| BM-08 Duffel | Partly | `services` with id and quantity is confirmed in the bags guide. Correction: the error `services_not_allowed_for_order_type` is in the API page's error table ("not currently supported"), although the first read missed it. Duffel's changelog of 2023-11-13 says hold orders with services are supported, so its two official sources conflict. |
| BM-09 Travelport | Confirmed | Ancillary guide only; the seats guide was not checked |
| BM-10 Lufthansa | Confirmed | Base Offer lists no seat reservation; Smart Offer does |
| BM-11 flydubai | Partly | `WCHR/WCHS/WCHC` meanings confirmed from the official page text in search results. The medical page was not read. |
| BM-12 Iberia | Not verifiable | HTTP 403 |
| BM-13 flydubai API | Added, unread | All attempts failed. Needs the page text from a browser. |

Unused strong evidence: Breeze states "A `Hold` booking may include seat and service offer items". That is the exact AeroTech case, unpaid Create with services, and it is more direct than anything R3 cites. It should be added to BM-04.

## 5. What can be sold at all

`SellableCandidate.Is` gates id signing (`SelectionSigner.ExpiryOf`) and Price (`SelectionProofService`). A candidate that fails it has no purchasable id, so Ordering never sees it.

| Gate | Effect on the matrix |
|---|---|
| Profile not `Seat` | A09 is `BLOCKED_SOURCE`, and stronger than R3 says: it cannot be priced, not merely absent from Resolve |
| `GatedVariants` | A17 and A19 are `BLOCKED_SOURCE`. Selected options also lack their fields (`AssistedTravelSelectionOptions`). |
| Confirmation must be `Immediate` | A18 and any pet or supplier product needing confirmation. A18 needed supplier confirmation on dev (`V11-ALL-VARIANTS-LIVE.md`). |
| Inventory authority not `Supplier` | Supplier-held products |
| Price `Filed`, complete, no quote provider, no unapplied fee | Quote-priced products, including dynamic upgrades |
| `PricingUnit` not `PerKilogram` | A03 per-kilogram pricing in R3's D6 is unreachable |
| Filed currency equals offer currency | No cross-currency sale |
| Purchase stage `PreOrder` or `Both` (`ProvisionEvaluation.cs:57`) | On-board A24 and post-ticketing products |
| Order scope only for A22 and A24 (`CoveragePlanner.AllowsOrderScope`) | G07 must use A22 or A24 |

The maximum is therefore 21 of 24 live, and possibly fewer on the staging catalog. The real number needs a token, because direct SQL on `db-test` stays denied. `Ancillary` guide §7 ("can be shopped, priced and resolved") is out of date against these gates.

## 6. Corrections to my earlier report

`Create-With-Selections-Prompt-Review-2026-10-31.md` has been corrected in place:
- C1: occupancy at Hold was taken from the guide without reading the verifier.
- C4: my rejection of Journey lines is superseded.
- S9: A09, A17 and A19 cannot fail live at Resolve or in the adapter, because they never get an id. S9 is fixture only.

## 7. Owner decisions needed

1. **Ancillary authorization.** Both R3 documents keep Ancillary as handoff only until you approve. Without it, Gate 2 and every live gate stay blocked.
2. **Closure definition** from F4.
3. **Live inputs:** a fresh Backoffice token and the page text of BM-13.
