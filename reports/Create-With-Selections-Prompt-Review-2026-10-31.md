# Review: "Initial Order Creation with Priced Ancillary Selections" prompt

Date: 2026-10-31. No code was changed for this review.

| | |
|---|---|
| Ordering | `stage6a-ancillary-commercial@07b7158`, equal to origin, clean tree |
| Ancillary | `k8s-stg@c6e783b`, equal to origin, clean tree |
| AirOffer (read only) | `AirAvail@e87136e` |
| Local probes | `POST localhost:4046/Service/v1/Ancillaries/SeatMaps/Resolve` and `.../AncillaryOffers/Resolve` with an unknown `selectionId` both answer 404 / 16819 |

## 1. Verdict

The prompt is the right next step. It is increment 6A.2 plus the seat half of 6A.3, and it replaces AC-03 with the seat Price and Resolve flow the owner approved in Ancillary on 2026-10-31.

Ordering's share is small because the domain is ready. It cannot close as written, for one reason: Ancillary does not yet return the source identity, so every live scenario except S1 fails closed. Five points of the prompt also need correcting (section 3).

## 2. Confirmed against the code

| Prompt assumption | Evidence |
|---|---|
| Two Resolve endpoints exist, each taking one `selectionId` | `AncillaryOffers/Controllers/ServiceController.cs:19`, `SeatMaps/Controllers/ServiceController.cs:21`, and the probes above |
| Plan methods reuse cleanly | `PlanAncillaries` and `PlanSeats` already take the pricing reason (`Order.Ancillaries.cs:178`, `:253`). `EnsureSelectionIsCurrent` and `EnsureSeatQuoteIsCurrent` already take the source kind and reference. |
| One Create change, version 1, `InitialSale` | `Order.cs:43`, `OrderPricingReason.InitialSale` exists, `RecordCreateChange` is unchanged |
| Totals | `ReconcileTotals` sums every customer line and every item. Nothing compares the total with the offer. |
| Included and added tax | `AcceptedPriceComponents` subtracts included tax from base once and reconciles to `UnitAmount` |
| Zero price | Allowed when `IsUnitTotalComplete` is true; zero components produce no line and the item total is 0 |
| Traveller, portion and flight refs | Both services read `v1/FlightOffers/Details`. Ancillary uses `ticket.TravellerRef`, `bound.BoundId` and `{boundId}:F{sequence}` (`FlightOfferShoppingContextMapper.cs:14`), which is what `TravellerOf` and `FlightAirServiceOf` expect. |
| OfferId equality | AirOffer echoes the request id exactly (`OfferPricePipeline.cs:89`). Dev orders store the full three-part id, 122 characters. |
| Column lengths | `SourceReference` 512, `SourceSelectionId` and `SourceOfferItemId` 1024, against an Ancillary limit of 512 for `OfferId` |
| Read side | `ProjectCreatedAsync` uses the shared upsert and the snapshot factory already projects priced seats. This path has never run with a priced seat, so S4 must prove it. |
| Ancillary needs no migration | `SelectionSnapshot` already stores `OfferId`, `ExpiresAtUtc` and its lines |

## 3. Corrections to the prompt

| # | Prompt text | Problem | Applied unless the owner says otherwise |
|---|---|---|---|
| C1 | §5: seat Resolve must "verify that the accepted seat data and pricing are still valid" | Resolve checks expiry and that the rate, provision and definition are active. A real seat re-check needs a sales scope, and `SeatPriceVerifier.ScopeAsync` refuses anything except Backoffice, OtaPanel and Api. The Service surface has no token, and the Ancillary guide §4.4 states Resolve does not re-check the seat. Doing it would be the redesign the same section forbids. | "Still valid" means the same rate is active and unexpired, which already holds. Eligibility stays at the seat Hold. Correction: occupancy is not re-read at the seat Hold; see F1 in `R3-Prompts-Review-2026-10-31.md`. |
| C2 | §2H: `OrderCreated` "containing the completed Order snapshot" | Both the domain and the integration event are flat and carry no services. | The event is unchanged; `CustomerTotal` includes the ancillaries. The completed snapshot is the read model written by `ProjectCreatedAsync`. |
| C3 | §3: reuse `ResolvedSeatQuote` as it is | It has no `s_` id. `PlanSeats` stores the per-seat id as `SelectionId` and leaves `OfferItemId` null, so the audit link to the snapshot is lost. | Add `SelectionId` to the quote. Store `s_` as `SelectionId` and the seat id as `OfferItemId`, matching general lines. The seat id is also what the later seat Hold needs. |
| C4 | §4: valid coverage mappings; §5: "ONLY these" fields | AC-05 is not on the list. `ServiceResolvedSelectionsMapper.Coverage` returns no flights for `PerTraveller`, so Ordering rejects every Journey line with `AncillaryCoverageIsNotSupported`. The snapshot line has no flight list, so this cannot be fixed by a mapping change. | Superseded by R3 C4: the Offer itself proves the flight set. |
| C5 | §1: flat properties on all three surfaces | Pack D03 and doc 12 define `selectedAncillaries` and an external `services[]` adapter, with `seatSelectionIds` and `ValidateForAcceptance`. | The prompt wins as the newer owner decision. It is recorded as D13, and Master line 2768 and the D10 and AC-03 text are updated in the same change. |

Naming only: `AirFare` comes from AirOffer through `Offer:BaseUrl`, not FlightFlow. The route is correct.

## 4. Consequences the owner accepts by this scope

1. **These orders cannot be sold through.** `EnsureFulfillmentIsSupported` stays, so Reserve, Confirm and Issue answer 2854 for the whole Order, including its AirFare. The only exits are a whole cancel or expiry at the last ticketing date; the poller covers `Created` orders. This matches D2 and D3.
2. **A free seat is blocked too.** `IsPricedSelection` means `Pricing is not null`, so a zero-priced seat sent through `SeatSelectionId` blocks Reserve, while the same free seat sent through legacy `SeatSelections` does not.
3. **Order-based shopping stops for these orders.** The AncillaryContext is non-empty, so Ancillary answers 503 / 16815 (`OrderingAncillaryContextMapper.cs:17`, AC-04).
4. **A seat on a Created order is not reserved.** Two orders can carry the same seat, and OR-006 is still open. Rate age band and exit-row eligibility are checked only at the seat Hold, where date of birth is available, so Create can accept a seat that later fails.

## 5. Ancillary companion (exact, no migration)

| Area | Change | Files |
|---|---|---|
| General Resolve header | `SourceKind`, `SourceReference` (`OfferId`, or `OrderId` as text), `ExpiresAtUtc`, `SelectedLineCount` from `Selection.Lines.Count` | `ServiceResolvedSelectionsDto`, `ServiceResolvedSelectionsMapper` |
| General Resolve pricing | `IsUnitTotalComplete` from `Rate.IsUnitTotalComplete` | `ServiceResolvedPricingDto`, `ServiceResolvedSelectionsMapper` |
| Seat Resolve header | `SourceKind`, `SourceReference` | `SeatMapResolutionSnapshot`, `ResolveSeatMapSelectionsService`, `ServiceSeatMapResolutionDto`, `ServiceSeatMapResolutionMapper` |

The rate, completeness flag, point of sale and expiry are already on the seat wire.

## 6. Ordering plan

| Layer | Change |
|---|---|
| RestApi | `AncillarySelectionId` and `SeatSelectionId` on both request records and the three controllers |
| Application | The three commands, `IAgencyCreateOrderCommand` and the args mappers; `CreateOrderFromOfferService` calls Offer, then each Resolve only when its id is present, then `Order.Create` |
| Domain | `Order.Create` takes the two resolved selections, validates them, plans with `InitialSale`, commits before `ReconcileTotals`. Legacy and new seats together: code 2866, 422, thrown in the domain so all surfaces share it. `IAncillaryProvider` gains the seat call. `ResolvedSeatQuote` gains `SelectionId`. |
| Providers | Seat wire file, seat mapper, `AncillaryProvider` seat route |
| Persistence | No migration expected |
| Records | D13, Master status, OR-005 log, evidence report |

Seat wire details the adapter handles, none of them invented data:

- Enums arrive as `{value, name, title}` objects.
- `Quantity` is 1 because Ancillary writes `SingleSeat` on every seat line.
- `UnitAmount` and `TotalAmount` both equal `UnitTotal`.
- A price component has no currency, and Ancillary guarantees it equals the rate currency (`PricingCurrencyMismatch`).
- `RequiresCheckAtReserve` is computed with Ancillary's formula from two wire fields, because the prompt's "ONLY these" list excludes adding it.
- A retired seat rate answers 404 / 16811 where general answers 409. The adapter maps 16811 to `AncillarySelectionIsNoLongerSellable`.

## 7. Acceptance outlook

| Scenario | Fixture and SQL | Live |
|---|---|---|
| S1 air only | Yes | Yes |
| S2, S3, S4, S6 | Yes | Needs the companion |
| S5 free seat | Yes | Needs the companion and a complete zero rate in the catalog, which is unverified |
| S7 | Yes | Wrong offer and expiry can run live. Incomplete price and wrong currency are fixture only. |
| S8 | Yes | Ancillary already refuses duplicates at Price, so the Ordering guard is proven by fixture |
| S9 | Yes | Fixture only. Correction: A09, A17 and A19 never receive a signed id, so they cannot reach Resolve or the adapter live (`SellableCandidate`, `SelectionSigner.ExpiryOf`). |
| S10 | n/a | Needs the companion, a fresh priced offer and new tokens. The earlier `$TEMP/bo.tok` and `ota.tok` no longer exist. |

## 8. Owner decision needed

The prompt allows both modes, and `CLAUDE.md` forbids code in another service's repo without instruction.

- **Allow the companion in `AeroTech.Ancillary`:** the seven files in section 5, on a local commit that is not pushed, run on a side port so the session on :4046 is untouched. S2 to S10 can then be proven live.
- **Ordering only:** OR-005 is updated with section 5. The result is `6A2_BLOCKED_SOURCE`, and the prompt forbids claiming COMPLETE.
