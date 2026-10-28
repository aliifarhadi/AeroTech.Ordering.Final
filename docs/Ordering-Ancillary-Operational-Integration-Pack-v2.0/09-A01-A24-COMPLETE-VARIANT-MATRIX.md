# 09 — All 24 Ancillary variants, without silent exclusions or fabricated capabilities

**Verified:** `AncillaryVariant.cs` at `190aa74` enumerates **exactly A01…A24** under **nine profiles**: Baggage(6), Seat(3), Upgrade(1), Meal(2), Pet(2), AssistedTravel(5), AirportService(3), Priority(1), Connectivity(1). `travelInsurance[]` is presently a reserved empty array and **is not** one of these 24.

## 1. Two independent statuses per variant

- **D = Domain complete**: exact source-selected choices and source-fixed definition facts can be represented in typed Order VOs, mapped to price, saved, reloaded, and projected, even for a **trusted complete contract fixture** when current live source cannot emit it.
- **S = Source live**: actual current `AncillaryOffers / Price / Resolve` (or Seat price validator) returns all necessary source-proven data, complete monetary price and correct source identity. `D=PASS` never means `S=PASS`.
- **O = Operational live**: actual Ancillary or FlightFlow provider supports Hold/Confirm/Cancel/Issue/SSR in the required way. A commercialized product **cannot be airline-live** unless the required O and document/payments are verified.

State `SOURCE-GATED`/`OPERATION-GATED` is explicit, never silently converted to a test PASS or faked with a zero rate/unknown supplier. Do NOT hard-code one Scope per A-code: `Provision.CoverageScope` decides Sector, Portion, Journey or Order independently of a variant name.

## 2. Full A01…A24 registry-to-Order crosswalk

| Code / source Profile | Immutable selected data + sourced fixed facts (nothing invented) | Occurrence | Live constraint at 190aa74 |
|---|---|---|---|
| **A01 ExtraCheckedBag / Baggage** | `quantity:int`; `chargeKind=ExtraPiece`, `allowanceConcept`, `maxKgPerPiece?`, package/allowance definition if actually supplied | `OrderBaggageService`, type Ancillary, kind Baggage | Source complete quote; count ≠ necessarily pieces unless Unit says so |
| **A02 ExtraWeightPackage / Baggage** | quantity, fixed `packageProductRef = ServiceDefinitionRef`, `packageWeightKg?`, allowance concept; don't invent weight | same | Actual product identity + source rate; no fake derived package |
| **A03 Overweight / Baggage** | `bagRef:string`, `measuredWeightKg:decimal>0`, conditional quantity; charge combination and weight interval | same | `PerKilogram` needs full applied total; incomplete unit quote blocked |
| **A04 Oversize / Baggage** | `dimensions{lengthCm,widthCm,heightCm}`; source maxSize/linear sum | same | Exact dimension rule, not universal airline 158cm |
| **A05 CabinBag / Baggage** | quantity, actual chargeKind/cabin allowance limits | same | Purchased cabin bag distinct from included Air allowance |
| **A06 SpecialEquipment / Baggage** | fixed equipmentKind; optional/required dimensions and weight per definition; actual limits | same | No arbitrary caller equipment code; complete source rules |
| **A07 StandardSeat / Seat** | seat source ID, real traveller ref, flight ID, seat number, A07 definition/provision, **one final rate**, POS, qty=1 | `OrderSeatService`, type SeatAssignment, kind Seat | **AC-03 Source-gated**; raw SeatMaps/Resolve not a final quote |
| **A08 PreferredSeat / Seat** | same A07, retain A08 price/rule distinction and exact source-selected seat | same | **AC-03 Source-gated** |
| **A09 ExtraSeat / Seat** | purpose/seat number/extra occupied seat count/adjacency+ticket requirements **only when exact source supplies**; true beneficiary/flight | `OrderAncillaryService`, type ExtraSeat, kind Seat | **AC-06 Source-gated**: `extraSeats=[]` in current Resolve; source operational ExtraSeat Hold absent |
| **A10 CabinUpgrade / Upgrade** | fixed from/to cabin, `targetCabinId` when source-selected, eligible fare families, `requiresTicketExchange`, allowedUpgradeKind | `OrderAncillaryService`, type Ancillary | FixedAncillary valid complete rate only; **ExternalQuote/DynamicQuote gated**; no Air RBD mutation |
| **A11 FreeSpecialMeal / Meal** | fixed `mealCode`, meal kind, dietary? catering lead time; source quantity | type Meal, kind Meal | **Real zero-priced Rate** mandatory; name Free alone insufficient |
| **A12 PaidPreorderMeal / Meal** | fixed `menuItemRef`, quantity, meal kind, diet?, exclusive family, lead time | type Meal, kind Meal | Source menu identity and actual rate; no placeholder 0 price |
| **A13 PetInCabin / Pet** | animalType, otherAnimalCode?, animalAgeWeeks?, combinedWeightKg, carrierDimensions, document acknowledgements; source Cabin constraints | type Ancillary, kind Ancillary | Can be domain accepted only from genuine complete quote; **Pets absent from current General Hold**, supplier approval gated for live |
| **A14 PetInHold / Pet** | A13 plus sizeBracket? and source hold transport/size/weight limits | same | **General Hold absent**; supplier/stock approval gated |
| **A15 Wheelchair / AssistedTravel** | `assistanceSsrCode` selected; allowed SSRs/assistance level/lead time | type Ancillary | Rate may be 0 legitimately; SSR confirmed by operational provider required |
| **A16 DisabilityAssistance / AssistedTravel** | selected SSR; definition allowed SSRs/required communication method? | type Ancillary | Matching current typed assistance options when source offers; supplier rules remain authoritative |
| **A17 MedicalEquipment / AssistedTravel** | CustomerSelectionContract: EquipmentCode, EvidenceDocumentRefs? and for oxygen OxygenUnits?, OxygenFlowRateLitersPerMinute, FrequentTravellerMedicalCardNumber?; definition approval/equipment facts | type Ancillary | **AC-06 Source-gated**: current Price/Resolve lacks all required medical fields; hold needs supplier confirmation. Do not replace with an empty options object |
| **A18 Bassinet / AssistedTravel** | infantRef, guardianRef, source verified parent/infant relation, max age/weight and compatible seat groups | type Ancillary | Genuine priced choice only; manual approval/physical allocation gated until proved |
| **A19 UnaccompaniedMinor / AssistedTravel** | source `ChildRef`, exact **text** GuardianHandoffContact + GuardianPickupContact, age/connection limits | type Ancillary | **AC-06 Source-gated**: CustomerSelectionContract text vs actual Price/Resolve no text; never convert boolean flags to text |
| **A20 Lounge / AirportService** | airport/facility/terminal/direction/windows/timezone, selected `timeWithOffset?`/`guestCount?` | type LoungeAccess | Slot/facility policy source; source hold/EMD test before live |
| **A21 FastTrack / AirportService** | same airport facts, kind FastTrack, only actual conditional appointment/guest choice | type Ancillary | Availability/provider actual; not auto bookable due to listing |
| **A22 CipMeetAssist / AirportService** | A20 plus includedComponentCodes and policy guest count | type Cip | Multi-component service remains one purchased occurrence if one quoted item; source capacity/document gate |
| **A23 PriorityBoardingCheckin / Priority** | selected `optIn:true` when form requires; definition zone/group/fareBenefitRef/airports | type Priority | Missing/false opt-in rejected when mandatory, don't infer from Price token alone |
| **A24 OnboardWifi / Connectivity** | fixed planCode = ServiceDefinitionRef, plan kind, duration/data/devices, optional selected deviceCount | type WiFi | Actual deliveryStage/fulfillment required; don't invent eSIM/email delivery |

### Profile count check

A01–A06=6; A07–A09=3; A10=1; A11–A12=2; A13–A14=2; A15–A19=5; A20–A22=3; A23=1; A24=1. **Total 24.** Every enum in `AncillaryVariant.All` must map to exactly one row; if a new A-code appears, fail type-set/contract tests rather than discard it.

## 3. Critical difference among Authoring, Shopping, Price, Resolve and Hold

| Source | What it actually proves | What it does not prove |
|---|---|---|
| `AncillaryVariant` registry / `CustomerSelectionContract` | domain definition, name and authored **potential** fields, including A17/A19 | selection actually returned by active shopping or accepted priced rate |
| `TypedSelectionForm.Fields` | customer option inputs currently exposed for a subset of variants | all authored definition variants available in Price |
| `PriceAncillaryOffers.SelectionOptionsInput` | actually accepted current option families: Baggage/Pet/AssistedTravel/AirportService/Priority/Connectivity | A17 medical and A19 contacts if not in current DTO |
| General `ServiceResolvedSelectionsDto` | actual accepted general source data **for emitted rows** | A09 (currently empty), expiry/source header, final non-item fees without completeness flag |
| Seat `SeatMaps/Resolve` | definition/active rate list of selected seat ID | source-bound chosen one-price rate and POS |
| Ancillary `Hold` | real approved capacity for supported families | pet/ExtraSeat capacity, existing FlightFlow physical seat or ETKT/EMD |

## 4. Typed choice requirements and Source/SQL parity

Use **family-typed** snapshots as specified in doc 03, with a discriminated `VariantCode`. A01/A02/A05 count changes are service Quantity `int`, **not** created per-item child service rows. A03 measured weight `decimal` never int or rounded; A04 dimensions are three positive decimals; A13/A14 document codes and animal type choices survive reload; A17 source medical selected fields are **not stored as dummy nulls masquerading as completed source**; A19 real free text is never collapsed to boolean flags. A20–A22 DateTimeOffset retains original offset, no UTC-only loss of local appointment meaning. A07/A08 seat Rate/POS is source-required. A09 extra seat is distinct from ordinary seat number assignment.

**No giant DTO with 30 nullable unrelated fields; no 24 independent aggregate roots.** Persist typed family-specific semantically relevant fields and source-fixed details. Where current source is incomplete, write a **contract test with a complete trusted fixture** and mark external live case `BLOCKED_SOURCE`, but do not hard-wire an arbitrary extra SQL table/column name before an actual source type exists. Use `CustomerSelectionContract` for the intended field name/type and AC-06 for resolving the final wire.

## 5. Per-variant acceptance tests — 48 domain/SQL + 24 live-status tests

For every Axx implement **two** isolated test families `Axx_CREATE` and `Axx_ADD`; assert exact object type, profile/variant, quantity, passenger/scopes, source options, price/evidence, one Change, historical price and SQL command+read roundtrip. Additionally run `Axx_SOURCE_CONTRACT` against actual source to return **PASS**, **BLOCKED_SOURCE** or **BLOCKED_OPERATION**, not passing with a fixture.

Required variant-specific negative matrix:

- **A01/A02/A05** qty 0, unit mismatch, package ref not matching source; **A03** bagRef missing, negative/oversized weight, incomplete per-kg quote; **A04/A06** dimensions partial, invalid equipment kind/required weight absent.
- **A07/A08** invalid or replayed seat id, duplicate physical seat, wrong traveller, wrong flight, expired quote, multiple differing POS prices, source Rate mismatch; **A09** false normal Seat conversion or fake physical hold.
- **A10** DynamicQuote taken as Filed, mutation of Air's accepted RBD/cabin; **A11** zero invented from name; **A12** mutable menu item changed after quote; **A13/A14** disallowed species, dimensions or missing document acknowledgements.
- **A15/A16** invalid SSR; **A17** medical fields/evidence omitted; **A18** infant/guardian mismatch; **A19** guardian text silently discarded/converted to booleans.
- **A20/A21/A22** timezone offset lost, guest policy violation, mismatched facility; **A23** no opt-in when required; **A24** planCode/deviceCount mismatch.

All 24 must be structurally designed and domain-tested; only exact real source and provider proof enables an actual sellable route.
