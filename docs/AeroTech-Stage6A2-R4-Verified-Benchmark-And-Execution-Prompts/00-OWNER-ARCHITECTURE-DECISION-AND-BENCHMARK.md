# AeroTech Stage 6A.2 R4 — Owner Architecture Decision and Completed Benchmark
Date: 2026-10-31
Status: RECOMMENDED FOR EXECUTION in sequence: 01 Ancillary contract companion, then 02 Ordering Create. FlightFlow seat uniqueness is a separate 6B task. No implementation is claimed to have happened.

## 0. Source authority and frozen source revisions
- Ordering `aliifarhadi/AeroTech.Ordering.Final`, branch `stage6a-ancillary-commercial`, commit `07b715859f9f20806eed0d371d31ef841505492a`.
- Ancillary `aliifarhadi/AeroTech.Ancillary`, branch `k8s-stg`, commit `c6e783bfbe989dfe0ce42a297e1e497c18b75949`.
- FlightFlow `aliifarhadi/Aerotech.FlightFlow`, branch `k8s-stg`, commit `962f41880df0f10a1d105559c233af63ef9db968`.
- Ordering domain authority: `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`, Stage 6A active pack, `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md`, `CLAUDE.md`. Master overrides stage text on disagreement until its precisely scoped amendment is approved and committed.
- This memo is a **consultant's completed analysis**. Agents must not re-decide architecture, invent benchmark rules, or improvise new features. They must stop and report a genuinely unexpected discrepancy with file/line proof.

## 1. Verified benchmark decisions — researched by architect, NOT delegated to agents

BM01 — IATA NDC 21.3 Implementation Guide, official 339-page guide, p.178 'Create Order Without Payment': an airline creates an Order from selected Offer(s)/Offer Item(s); payment details not required for the Create request. The existence of the resulting Order does not by itself imply a FlightFlow physical seat lock. URL: https://guides.developer.iata.org/docs/21-3_ImplementationGuide.pdf#page=178

BM02 — IATA NDC 21.3 p.205 'UC03 - Shop and Order a flight for multiple passengers with additional services': multiple passengers, round trip, additional bag and seat; flow includes Shopping, ServiceList, SeatAvailability, OfferPrice, OrderCreate. **This is evidence for a multi-service initial Order, not evidence that every carrier sells all 24 AeroTech variants.** URL: https://guides.developer.iata.org/docs/21-3_ImplementationGuide.pdf#page=205

BM03 — IATA/NDC industry vocabulary is `Offer`, `OfferItem`, `Order`, `OrderItem`, `Service`, passenger and journey/segment associations. `AncillaryPricedSelectionId` and `SeatPricedSelectionId` are **AeroTech private/internal adapter names**, NOT fields claimed to be in the IATA schema. NDC 21.2 samples also demonstrate `OfferRefID`, `OfferItemRefID`, `PaxRefID`, `PaxJourneyRefID`: https://guides.developer.iata.org/docs/21-2_ImplementationGuide.pdf (p.86). Avoid treating a one-to-many selection snapshot as an individual NDC OfferItem.

BM04 — Breeze official NDC XML `OrderCreate`: initial Order may include selected ancillary seats and services, and hold without payment may include those items. https://pro-docs.flybreeze.com/api/ndc-xml/endpoints/ordercreate

BM05 — Breeze official JSON: `POST /json/v2/orders` accepts `offerItemIds`, `offerItemSelections` including `passengerIds`; initial booking can include fare, seat, bag. Distinguish selected item from snapshot token; validate passenger and item associations. https://pro-docs.flybreeze.com/api/json/endpoints/create-order/ and https://pro-docs.flybreeze.com/api/json/sample-flows/add-bags-and-extras/ and https://pro-docs.flybreeze.com/api/json/sample-flows/add-seats

BM06 — Amadeus official enterprise API description explicitly says `Flight Create Orders` books flights and ancillaries such as bags/seats; its PNR Element/management interfaces are not 1:1 the IATA standard. https://developers.amadeus.com/enterprise/category/booking-management/api/pnr-elements?catalogId=2

BM07 — Travelport is carrier/connection/surface-dependent: booking workbench, optional seats, paid ancillaries and commit; a current ancillary guide excludes some initial-booking purchases (paid bags/non-baggage on certain interfaces). Therefore no universal requirement to provide 24 operationally confirmed services at initial booking. https://support.travelport.com/webhelp/JSONAPIs/Airv11/Content/Air11/Book/BookingGuide.htm and https://developer.travelport.com/docs/flights/guides/seats-and-ancillaries/ancillaries-and-emd-guide

BM08 — Duffel `POST /air/orders` uses `selected_offers` and per-service `services` (service id + quantity); **hold orders currently reject services** (`services_not_allowed_for_order_type`). Do not rename a multi-line AeroTech snapshot `services[]` merely to imitate Duffel. https://duffel.com/docs/api/orders

BM09 — IATA/airline fulfillment semantics: wheelchair SSR assistance is not equivalent to a paid baggage product; assistance SSRs include WCHR/WCHS/WCHC, and special medical clearance is conditional. A medical request may require specific source/vendor approval before fulfillment. The IATA accessibility code guide is public, but no claim is made that IATA specifies AeroTech's A17 payload. https://www.iata.org/contentassets/7b3762815ac44a10b83ccf5560c1b308/best-practices-on-the-application-of-ssr-codes-and-assistance-service.pdf

BM10 — Lufthansa bassinet is capacity/age/aircraft-constrained; customer selection in an Order does not guarantee immediate supplier allocation. https://www.lufthansa.com/md/en/fliegen-mit-baby

BM11 — flydubai explicitly does not accept unaccompanied minors in its conditions of carriage (subject to described older-child exception); a provider's policy must govern A19 sellability, not any blanket assertion that all airlines offer A19. Its medical MEDIF rules also show conditional approval. https://www.flydubai.com/en/information/policies/conditions-of-carriage/ and https://www.flydubai.com/en/flying-with-us/accessibility-and-assistance-services/fitness-to-fly/medical-information-form-medif/

BM12 — ExtraSeat is a separate commercial/physical-seat/ticketing scenario, not an ordinary SeatMap seat selection: flydubai states EXST may be charged at full adult fare and uses special passenger naming/booking flows, while British Airways says an extra ticket may be needed and customer service/agent may have to book it. This demonstrates why AeroTech A09 needs an explicit source contract before being released; do NOT infer a universal quantity, adjacency, or air-ticket algorithm. https://www.flydubai.com/en/book-and-manage/add-extras/select-seats/ and https://www.britishairways.com/content/information/seating/reserving-your-seat?topic=howtobook

NOT independently verified as exact API evidence for this design: proprietary Sabre/Altea core PSS implementation, Lufthansa NDC payloads, Condor authenticated pages, or flydubai private OrderCreate APIs. They do not ground any field or rule in these prompts. Missing reference = no fabricated benchmark.

## 2. Actual three-repository code findings

Ordering Create (verified): `CreateOrderFromOfferService` calls only `IOfferProvider.GetByOfferIdAsync`, then `Order.Create`, command persistence and read model. No Ancillary calls. `Order.Create` currently builds air/legacy seats/fare/pricing then `ReconcileTotals` and `RaiseCreated`. `Order.Ancillaries` already has validation, `PlanAncillaries`, `PlanSeats`, `Commit`, `AcceptedPriceComponents`, and one `OrderChange(AddProduct)` path. **Reuse the existing purchase planners and `InitialSale` for Create, but NEVER call `AddAncillaries` after Create.** Order Master §6.4-6.7 and §8 require one accepted service occurrence, correct beneficiary/coverage and accepted price, not fake fulfillment.

Ancillary source (verified): both Price commands create a `SelectionSnapshot` with id `s_...`, offer/order source and expiry. General `ServiceResolvedSelectionsDto` lacks `SourceKind`, `SourceReference`, `ExpiresAtUtc`, `SelectedLineCount`; general pricing DTO lacks `IsUnitTotalComplete`. Seat Resolve lacks source identity and snapshot `SelectedLineCount`. Existing Ordering mapper intentionally fails closed on missing header/completeness. These are **real wire-contract blockers**, not a reason to skip Create.

Journey C4 (verified): `CoveragePlanner.Units(context, traveller, Journey)` uses **all flights from all portions of that shopping context**, and `SelectionScopes.Of(Journey)` returns `PerTraveller`. `ServiceResolvedSelectionsMapper.Coverage` correctly emits only traveller ref for this case. `Order.JourneyAirServicesOf` wrongly requires a non-empty `Coverage.FlightRefs`; Master §6.7 says Journey = only explicitly source-named flights. **Minimal approved proposed fix:** for Offer-scoped snapshot bound to exact Create Offer, `PerTraveller`+`Journey`, and absent portion/flight refs, map all active air services for that exact source traveller from the just-created itinerary. Do not use this rule for Order-scoped Add. Amend exactly that Master row and Stage authority reference in place. Do not add DTO columns or generic coverage architecture for C4.

FlightFlow (verified): `FlightCapacity.HoldSeat` increments capacity/records a `FlightSeatHold` containing `SeatNumber`, BUT does not check duplicate active seat number before adding. Therefore `POST /Service/v1/Flights/Seat-Holds` does not guarantee exclusive physical seating. `OccupiedSeats` is read-only and is not a transactionally atomic lock. This is **OR-006 / 6B**, not part of 6A.2 Create. Do not claim reserved or unique seat at Create or at existing FlightFlow Hold until OR-006 is closed and tested. URLs: https://github.com/aliifarhadi/Aerotech.FlightFlow/blob/962f41880df0f10a1d105559c233af63ef9db968/AeroTech.FlightFlow/FlightFlow/src/AeroTech.FlightFlow.Domain/FlightAggregate/Entities/Capacities/FlightCapacity.cs and https://github.com/aliifarhadi/Aerotech.FlightFlow/blob/962f41880df0f10a1d105559c233af63ef9db968/AeroTech.FlightFlow/FlightFlow/src/AeroTech.FlightFlow.RestApi/V1/FlightAggregate/Controllers/ServiceController.cs

## 3. Scope and product-definition decision

**Target:** A new Order containing all source-valid, selection-priced chosen services in its initial commercial state, one Create change, CommercialVersion=1, accurate total, detailed source audit and persisted typed family entities. No second Add mutation. No seat hold, ancillary hold, Confirm, Issue, payment orchestration or EMD.

**The 24 variants are an AeroTech domain taxonomy, NOT 24 officially standardized NDC operation types.** Each must have a definition, domain representation, test case and explicit live source status. Source-gated does not mean silently dropped and cannot be reported live-pass.

| Variant | Family / source business meaning | Domain decision for Create |
|---|---|---|
| A01 | Extra checked bag | typed baggage, quantity and accurate allowance unit |
| A02 | Extra weight package | typed baggage, source-proven package weight and ref |
| A03 | Overweight | typed baggage, bag ref and measured weight, full priced total only; `PerKilogram` is currently source-gated |
| A04 | Oversize | typed baggage, selected dimensions |
| A05 | Cabin bag | typed baggage, distinguish purchased vs included allowance |
| A06 | Special equipment | typed baggage, equipment identity/rules |
| A07 | Standard seat | `OrderSeatService`, one traveller+flight, actual selected rate; no physical lock |
| A08 | Preferred seat | same, real selected preferred-seat definition/rate |
| A09 | Extra occupied seat | distinct `OrderExtraSeatService`, NOT SeatMap assignment; `BLOCKED_SOURCE` because generic SellableCandidate rejects `Profile=Seat` |
| A10 | Cabin upgrade | typed upgrade, fixed fully priced item only; dynamic quote source-gated |
| A11 | Free special meal | typed meal, **real complete 0-priced rate**; never invented free price |
| A12 | Paid preorder meal | typed meal, selected menu/ref and quantity |
| A13 | Pet in cabin | typed pet, real animal/container/quantity choices and approved source |
| A14 | Pet in hold | typed pet, separate supplier capacity/acceptance pending 6B |
| A15 | Wheelchair | typed assistance + real SSR (WCHR/WCHS/WCHC), not automatically priced |
| A16 | Disability assistance | typed assistance + verified source SSR/options |
| A17 | Medical equipment | typed assistance / real medical choice, `BLOCKED_SOURCE` hard-coded gate and missing options, no fabricated medical facts |
| A18 | Bassinet | typed assistance with infant/guardian, supplier acceptance may gate sale |
| A19 | Unaccompanied minor | typed assistance / real guardian contact, `BLOCKED_SOURCE` hard-coded gate and missing option text; airline-specific applicability |
| A20 | Lounge | typed airport service, airport/facility/time/guest choice if supplied |
| A21 | Fast track | typed airport service, airport/terminal/slot if supplied |
| A22 | CIP / meet assist | typed airport service, source-proven package; eligible `PerOrder` where catalog says so |
| A23 | Priority boarding/check-in | typed priority, actual opt-in/benefit |
| A24 | Wi-Fi | typed connectivity, real plan/devices/duration; eligible `PerOrder` when catalog says so |

`SellableCandidate.Is` also rejects: non-immediate confirmation, supplier inventory authority, non-filed or incomplete price, quote-provider-dependent lines, unapplied fees, mismatched currency, `PerKilogram`; catalog and pre-order purchase-stage rules further restrict live availability. Thus **even 21/24 is only a theoretical upper bound, not a verified live result**. Variant-by-variant evidence must use the source's actual supported Price/Resolve API, not trust fixture tests.

## 4. Closure criteria and owner decision boundary

**Stage 6A.2 engineering closure:** Create works for all currently genuinely source-sellable offerings; 24/24 domain-and-fixture rows pass or are explicitly `BLOCKED_SOURCE`/`BLOCKED_PERSISTENCE`, every status is evidence-backed; all tested source-valid choices persist as accurate OrderItem/OrderService/PricingLines; air-only regression passes. E2E must not be marked COMPLETE when the source contract is not available; use `BLOCKED_SOURCE` for that gate.

**Owner's ultimate commercial goal — every A01-A24 live:** NOT YET achieved; requires **separate** approved Ancillary commerce/option expansion for A09, A17, A19 and any other catalog-gated scenario, plus persistence after a real source contract, and later provider-specific Hold/EMD fulfillment. The user explicitly forbids overengineering: do not silently override D1, synthesize choices, remove source gates, or build 6B here. Present a focused Owner Decision Request before implementing that expansion.

**Authority update:** amend one Master `CoveredAirServiceIds` Journey sentence to recognize this Offer-scoped source-contract invariant, correct Master/pack obsolete seat quote references, add D13/14 dated owner-approved decision in existing ledger, and update `CLAUDE.md` authority paragraph. No parallel new Master or speculative new domain type.

## 5. Evidence ledger (source files)
- Ordering: `src/AeroTech.Ordering.Application/OrderAggregate/Commands/CreateOrderFromOffer/CreateOrderFromOfferService.cs`, `Domain/OrderAggregate/Order.Create.cs`, `Domain/OrderAggregate/Order.Ancillaries.cs`, `Domain/OrderAggregate/Order.Pricing.cs`, `Domain/Providers/Ancillary/ResolvedAncillarySelection.cs`, `Providers/Ancillary/Services/AncillaryProvider.cs`, `Providers/Ancillary/Services/ResolvedSelectionsMapper.cs`, `RestApi/V1/OrderAggregate/Requests/CreateOrderFromOfferRequests.cs`.
- Ancillary: `src/AeroTech.Ancillary.Domain/SelectionSnapshotAggregate/SelectionSnapshot.cs`, `Application/SelectionSnapshotAggregate/Commands/PriceAncillaryOffers/PriceAncillaryOffersService.cs`, `Query/Shopping/AncillaryOffers/Dto/Service/ServiceResolvedSelectionsDto.cs`, `ServiceResolvedPricingDto.cs`, `Query/Shopping/AncillaryOffers/Queries/ResolveAncillarySelection/Service/ServiceResolvedSelectionsMapper.cs`, `Query/Shopping/SeatMaps/Dto/Service/ServiceSeatMapResolutionDto.cs`, `Query/Shopping/SeatMaps/Snapshots/SeatMapResolutionSnapshot.cs`, `Query/Shopping/SeatMaps/Queries/ResolveSeatMapSelections/ResolveSeatMapSelectionsService.cs`, `Service/ServiceSeatMapResolutionMapper.cs`, `Shopping/Engine/CoveragePlanner.cs`, `Shopping/Selections/SellableCandidate.cs`.
- FlightFlow: `AeroTech.FlightFlow/FlightFlow/src/AeroTech.FlightFlow.Domain/FlightAggregate/Flight.cs`, `Entities/Capacities/FlightCapacity.cs`, `Entities/Capacities/FlightSeatHold.cs`, `RestApi/V1/FlightAggregate/Controllers/ServiceController.cs`, `RestApi/V1/FlightAggregate/Requests/HoldSeatsRequest.cs`.

Benchmark confidence: BM01-08 are independently read in official public documentation; BM09 is publicly documented at SSR-policy level; BM10-11 are carrier-policy references not exact contract schemas. Any unsupported claim is omitted rather than invented.
