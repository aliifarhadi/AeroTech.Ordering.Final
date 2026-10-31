# Stage 6A.2 R4: Create with priced selections, evidence

Date: 2026-10-31. Executes `docs/AeroTech-Stage6A2-R4-Verified-Benchmark-And-Execution-Prompts/02-ORDERING-CREATE-WITH-ALL-PRICED-SELECTIONS-EXECUTION-PROMPT-R4.md`, amended by owner decisions D15 and D16.

## 1. Status

Create with general and seat selections is proven live on a real offer, from Price through Resolve to Create and the read model. A seat number was proven live through a FlightFlow hold.

| Item | State |
|---|---|
| Ordering baseline | `stage6a-ancillary-commercial@07b7158` |
| Ancillary | `k8s-stg@bfd3fd8` (R4-A), host on :4046 |
| FlightFlow | Staging, `k8s-stg@962f4188` |
| Ordering under test | Side host on :4144 built from this change, dev database `DotAirOrdering` |
| Flight | IKA-IST 2026-11-05 and IST-IKA 2026-11-06, one adult, one child, one infant |
| Migration | None |

## 2. Live orders

| Order | Fare | Content | Result |
|---|---|---|---|
| `1566038949862834176` | DA590 Basic plus DA701 Basic | 13 general lines and 4 free seats | Total 509,665,036 |
| `1566040759453351936` | DA591 Flex | 13 general lines with A10, one paid and one free seat | Total 746,157,518. Reserve answers 2854, as D15 requires for a general ancillary. |
| `1566040834904686592` | DA591 Flex | One paid and one free seat | Total 83,607,518. Reserve held, PNR `4J7UX8`. |
| `1566041034096377856` | DA590 Flex | Air only | Total 41,607,518 |
| `1566041210831765504` | DA590 Flex | One A07 seat, 12A | Accepted at a complete zero rate |
| `1566038119650689024` | DA590 Basic plus DA701 Basic | Same basket as the first | Left broken by the race in section 5. Test data, read model partial. |

The hold on seats 1C and 2C of flight 591 is kept on staging at the owner's request. It expires on 2026-11-05 05:30 UTC.

FlightFlow `OccupiedSeats` returned seat 1C with reference `order:1566040834904686592:reservation:1566040843201019904`. `ReservationUnits.ObservedSeat` holds 1C and 2C.

## 3. Pricing proof

Order `1566040759453351936`: air 41,607,518 + general 662,550,000 + paid seat 42,000,000 + free seat 0 = **746,157,518**, equal to `CustomerTotal`.

It has one Create change at commercial version 1. The read model holds 18 items and 18 services, 15 of them with ancillary facts. Item totals sum to 743,557,518; the difference of 2,600,000 is the Offer's order-level tax line, which is not allocated to an item.

## 4. Gates

| Gate | Fixture and SQL | Live |
|---|---|---|
| G01 air only | PASS | PASS |
| G02 general selection | PASS | PASS |
| G03 seat selection | PASS | PASS, paid and free |
| G04 air, general and seats | PASS | PASS |
| G05 complete zero seat | PASS | PASS, A07 and A08 |
| G06 refusals leave no order | PASS, 16 general and 14 seat cases | PASS for selection of another offer (2830) and unknown selection (2864) |
| G07 coverage | PASS | Sector and Portion PASS. `PerOrder` and Journey were not offered on any of 13 fares, so they are fixture only. |
| G08 family fidelity | See section 6 | See section 6 |
| G09 three surfaces | PASS | Backoffice only. No Ota or OtaPanel token. |
| G10 event and read model | PASS | PASS |
| G11 | Removed by D15 | Removed by D15 |
| G12 Add unchanged | PASS | Not run |
| G13 guard | PASS: general ancillary blocks Reserve; priced seat reserves, confirms and is refused at Issue | Reserve PASS for both. Confirm and Issue fixture only. |
| G14 real HTTP | n/a | PASS |

## 5. Defects found by the live run

Neither was visible in fixtures, and both were fixed.

| Defect | Effect | Fix |
|---|---|---|
| Read model race | `OrderingUnitOfWork` saved command then read model on two connections. A publish between them let the consumer insert the read model header. Create answered 500, the Order stayed stored and the read model stayed partial. It happened once live, on order `1566038119650689024`. | One shared transaction (D16). `OrderingUnitOfWorkTests` rebuilds the race on SQL. |
| Offer baggage unit | AirOffer sends `cabinBaggageUnit: "0"` with zero pieces and weight for a fare with no allowance. Every Create on such a fare failed with 2703. | `OfferResponseMapper` treats that exact shape as no allowance. A real allowance with that unit is still refused. |

## 6. Variant matrix

Fulfillment is `NOT_IN_SCOPE` for all 24. Issue stays refused for every ancillary and seat.

| Code | Domain fixture | SQL round trip | Live |
|---|---|---|---|
| A01, A03, A04, A06 | PASS | PASS | PASS |
| A02, A05 | PASS | PASS | `NOT_TESTABLE_CATALOG`: not offered on these 13 fares |
| A07 | PASS | PASS | PASS, free |
| A08 | PASS | PASS | PASS, paid and free |
| A09 | PASS | `BLOCKED_PERSISTENCE` (D1) | `BLOCKED_SOURCE` |
| A10 | PASS | PASS | PASS, Flex and Green fares only |
| A11, A12 | PASS | PASS | PASS |
| A13, A14 | PASS | PASS | `NOT_TESTABLE_CATALOG`: no pet offer on these 13 fares |
| A15, A16 | PASS | PASS | PASS |
| A17, A19 | PASS | `BLOCKED_PERSISTENCE` (D1) | `BLOCKED_SOURCE` |
| A18 | PASS | PASS | `BLOCKED_SOURCE`: the form offers no infant on an Offer |
| A20, A21, A22, A23 | PASS | PASS | PASS |
| A24 | PASS | PASS | PASS on DA590. Not offered on DA591. |

Live total: 16 of 24 accepted end to end. Four are not offered on this route, four are blocked at source.

## 7. Open

1. Order `1566038119650689024` is test data with a partial read model. It predates the fix.
2. OR-006: two orders can hold one seat, a risk D15 accepted.
3. An order holding a seat, free or paid, cannot be issued until Stage 6B. An order holding a general ancillary cannot be reserved.
4. A client that still sends `seatSelections` is not refused: the field is ignored, so no seat is booked.
5. The default host on :4044 is not running. It serves this code once started from this commit.
