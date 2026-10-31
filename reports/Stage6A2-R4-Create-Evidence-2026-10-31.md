# Stage 6A.2 R4: Create with priced selections, evidence

Date: 2026-10-31. Executes `docs/AeroTech-Stage6A2-R4-Verified-Benchmark-And-Execution-Prompts/02-ORDERING-CREATE-WITH-ALL-PRICED-SELECTIONS-EXECUTION-PROMPT-R4.md`.

## 1. Status

Create with general and seat selections is implemented and proven by fixture, SQL and wire-shape tests. It is **not** live-proven, so the stage is not COMPLETE.

| Item | State |
|---|---|
| Ordering baseline | `stage6a-ancillary-commercial@07b7158` |
| Ancillary | `k8s-stg@c6e783b` plus uncommitted R4-A work |
| FlightFlow | `k8s-stg@962f4188`, unchanged |
| Migration | None. The dev database holds no seat or ancillary row, so no data correction is needed. |
| Live gate G14 | `BLOCKED_ENVIRONMENT` |

## 2. What changed

| Layer | Change |
|---|---|
| RestApi | `AncillaryPricedSelectionId` and `SeatPricedSelectionId` on both request records and three controllers. The Create response is unchanged. |
| Application | The ids pass through the commands, mappers and validators. `CreateOrderFromOfferService` checks the seat conflict, reads the Offer, resolves each present id once, and calls `Order.Create`. |
| Domain | `AcceptSelections` is the one acceptance kernel for Create and Add. Offer-sourced Journey coverage (D13). Seat source keeps `s_` and the seat id separately (D14). Code 2866. `ResolvedSeatQuote` gains `SelectionId` and `SelectedLineCount`. |
| Providers | `ResolvedSeatsMapper` and `ResolvedSeatsResponse`. `AncillaryProvider` shares one `ResolveAsync`. |
| Authority | Master §6.7 and related rows, pack 00/04/05/06/12 and ALL-IN-ONE, D13, D14, `CLAUDE.md`, OR-005 log. Pack 03 needed no change. |

Not changed: `EnsureFulfillmentIsSupported`, the `OrderCreated` event, Add's contract, persistence mapping.

## 3. Ancillary contract (prompt 01)

Read from the Ancillary working tree, not from a commit.

| Gate | Result |
|---|---|
| AC01 general header | Matches the prompt. Values come from `SelectionSnapshot`. |
| AC02 price completeness | Matches. Comes from `Rate.IsUnitTotalComplete`. |
| AC03 seat header and count | Matches. `selectedLineCount` is the stored line count. |
| R4 tests | 13 of 13 passed in my own run, built to a side folder. |
| Cardinality | Their reasoning holds: `AncillaryProfile` has nine members, eight are mapped and `Seat` is refused by `SelectionResolver`. |
| Live HTTP | Not run. The work is uncommitted and the :4046 host predates it. |

## 4. Gates

| Gate | Result | Evidence |
|---|---|---|
| G01 air only, legacy seats | PASS | `An_air_only_create_calls_no_ancillary_source`, `Legacy_seat_selections_keep_their_bound_wide_behaviour`, existing Create suite |
| G02 general selection | PASS | `General_lines_of_different_families_join_the_initial_order_in_one_create_change` |
| G03 seat selection | PASS | `Priced_seats_become_standalone_seat_items_with_their_exact_source` |
| G04 air, general and seats | PASS | `Air_general_lines_and_seats_are_priced_in_one_create_with_each_tax_counted_once` |
| G05 complete zero seat | Fixture PASS, live `NOT_TESTABLE_CATALOG` until a zero rate is confirmed | `A_complete_zero_rate_seat_is_accepted_with_no_price_line` |
| G06 refusals leave no order | PASS | 16 general and 14 seat cases, plus mixed offers and unknown id |
| G07 coverage | PASS | Portion, Journey, whole-order and contradiction tests |
| G08 family fidelity | See section 5 | 22 general and 2 seat variant cases |
| G09 three surfaces | PASS | `CreateSurfaceContractTests`: JSON, controller, command, args, validators and response shape |
| G10 event and read model | PASS | Event total in the G04 test, SQL read model in `CreateWithPricedSelectionsPersistenceTests` |
| G11 legacy conflict | PASS | 2866 and HTTP 422 before any provider call, and again in the domain. All surfaces share the service. |
| G12 Add unchanged | PASS | Every `AddAncillariesTests` case passes. `AcceptSelections` is shared. |
| G13 guard active | PASS | `Selections_accepted_at_create_keep_reservation_blocked_until_fulfillment_exists`: 2854 and HTTP 409 |
| G14 real HTTP | `BLOCKED_ENVIRONMENT` | No fresh token, and the Ancillary contract is neither committed nor running |

Wire proof is `AncillaryResolveWireContractTests`. Its two payloads follow the DTO shapes and host JSON settings, and each runs through the real adapter into Create. They are hand-written, so they are not live evidence.

## 5. Variant matrix

Fulfillment is `NOT_IN_SCOPE` for all 24. Live HTTP is `BLOCKED_ENVIRONMENT` for all 24.

| Code | Domain fixture | SQL round trip | Source Price |
|---|---|---|---|
| A01, A02, A04, A05, A06 | PASS | PASS | Not proven on the live catalog |
| A03 | PASS | PASS | `PerKilogram` is gated by `SellableCandidate`; other units not proven |
| A07, A08 | PASS | PASS | Seat path, not proven on the live catalog |
| A09 | PASS | `BLOCKED_PERSISTENCE` (D1) | `BLOCKED_SOURCE`: `Profile=Seat` is not sellable |
| A10 | PASS | PASS | Fixed price only; dynamic quotes are gated |
| A11, A12 | PASS | PASS | Not proven on the live catalog |
| A13, A14 | PASS | PASS | Needs immediate confirmation; not proven |
| A15, A16 | PASS | PASS | Not proven on the live catalog |
| A17, A19 | PASS | `BLOCKED_PERSISTENCE` (D1) | `BLOCKED_SOURCE`: hard gate and missing options |
| A18 | PASS | PASS | `BLOCKED_SOURCE` on Offer: the offer has no adult-infant link (Ancillary R4-A report §6) |
| A20, A21 | PASS | PASS | Not proven on the live catalog |
| A22, A24 | PASS | PASS | Not proven. These are the only `PerOrder` variants. |
| A23 | PASS | PASS | Not proven on the live catalog |

## 6. Pricing proof

From the G04 test: air 600 + general 216 + 10 + seats 25 + 40 = `CustomerTotal` 891.

The general line has base 100, included tax 10, added tax 5 and fee 3, at quantity 2. That gives 216, so the included tax is counted once. Every item's `AcceptedTotal` equals its allocated customer lines.

## 7. Open

1. The Ancillary team commits R4-A and restarts the host.
2. A fresh Backoffice token for G14.
3. The host on :4044 runs older code and needs a restart to serve the new fields.
4. A18 needs an adult-infant link on the Offer, and the catalog correction Ancillary reported.
5. Orders created this way stop at Created until Stage 6B. Reserve, Confirm and Issue answer 2854.
6. OR-006 seat uniqueness is not part of this stage and is not solved. A seat at Create is a commercial selection, not a reservation.
