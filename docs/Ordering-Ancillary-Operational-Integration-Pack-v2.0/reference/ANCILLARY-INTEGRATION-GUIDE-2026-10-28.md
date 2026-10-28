# Adding ancillaries to an order: integration guide for the Ordering agent

Date: 2026-10-28. Source of truth: the AeroTech.Ancillary code on `feat/ancillary-offers-v9` (HEAD `0f9dfbb`).

Some of the routes below are not committed or deployed yet:
- the order-based shopping routes (`…/Orders`, `…/Orders/Price`, `SeatMaps/Orders`);
- the `orderId` and `orderCommercialVersion` in the Resolve output.

They exist in the Ancillary working tree and run on a local host. Everything else is committed.

This guide tells you three things:

1. which data Ordering must give Ancillary and keep for itself;
2. the end-to-end flow to add ancillary services (baggage, meals, seats, assistance, priority, lounges and so on) to an existing order;
3. how to call each Ancillary endpoint, what comes back, and which errors to expect.

Ancillary owns the product catalog, eligibility, prices, seat maps and the inventory hold. Ordering owns the order, its order services, payment, EMD issuance and the commercial version.

---

## 1. Conventions

| Item | Value |
|---|---|
| Ancillary base URL | per environment; the local Development host listens on `http://localhost:4046` |
| Service surface (Ordering → Ancillary) | `Service/v1/…`, anonymous (no token, server to server) |
| Customer-facing surfaces | `Backoffice/v1/…` (airline token), `OtaPanel/v1/…` (agency panel token), `Api/v1/…` (partner API token) |
| JSON | camelCase; `long` ids are written as strings (both strings and numbers are accepted on input); enum members travel as their names |
| Success body | `{ "data": … }` |
| Error body | `{ "errors": [ { "code": 16820, "title": "…" } ] }` with the HTTP status of the error |
| Unknown request members | refused (400) by the AncillaryOffers, Price, Resolve, Hold and Cancellations requests; the SeatMaps, seat Resolve and seat Hold requests ignore them |

---

## 2. What Ancillary reads from Ordering

Ancillary calls your route `GET Service/v1/Orders/{orderId}/AncillaryContext` (Ordering.Final, already built on your side). It uses the `Ordering:BaseUrl` setting and a 5 second timeout. The full field list is in `reports/ORDERING-ANCILLARY-CONTEXT-HANDOFF.md`.

Ancillary enforces these rules on your answer. If any rule fails, Ancillary answers its own caller with 503 / 16815 "the commercial context of order … could not be read".

| Rule | Detail |
|---|---|
| `orderId` | equals the requested id |
| `currencyId` | > 0 |
| `ancillaryServices` | **must be an empty array for now** (see §10, item 1) |
| `journeys` | at least one, each with at least one segment |
| uniqueness | `boundId`, `segmentId`, `travellerId`, `orderServiceId` and the pair traveller + segment are unique |
| references | every air service points to an existing traveller and segment; `infantParentTravellerId` points to an existing traveller |
| one booking per segment | every air service of one segment carries the same `flightCapacityId`, `cabinClassId`, `rbdId` and `bookingClass` |
| `cabinClassId` | null, or 1 … int.MaxValue |
| 404 from you | means "order unknown"; Ancillary then answers 403 / 16809 |

Ancillary ignores `acceptsChanges` until your Stage 6 (Owner ruling R2-1). It does not compare sales channel or country.

### How Ancillary names your objects

Every reference Ancillary returns for an order is derived from your own ids. You will see them in shopping and Resolve answers, and you translate them back for the Hold.

| Ancillary reference | Built from your AncillaryContext | Example |
|---|---|---|
| `travellerRef` | `travellers[].travellerId` as a decimal string | `"1552605112482922496"` |
| `portionRef` | `journeys[].boundId` | `"B1"` |
| `flightRef` | `"{boundId}:F{segments[].sequence}"` | `"B1:F1"` |
| order-level unit | the constant `"ORDER"` (journey level: `"JOURNEY"`) | |

---

## 3. Who calls what

```
Customer surface (Backoffice / OtaPanel / Api)          Ordering service                    Ancillary
------------------------------------------------         -----------------                   ---------
1  POST {S}/v1/AncillaryOffers/Orders {orderId}  ------------------------------------------->  reads your AncillaryContext, lists offers
   POST {S}/v1/SeatMaps/Orders {orderId}         ------------------------------------------->  seat maps with seat ids
2  POST {S}/v1/AncillaryOffers/Orders/Price      ------------------------------------------->  stores a priced selection, returns selectionId
3  "add these services to my order" ----------->  your own endpoint (your design)
                                                  4  POST Service/v1/AncillaryOffers/Resolve ->  definition, provision, price of every line
                                                     POST Service/v1/Ancillaries/SeatMaps/Resolve (seats)
                                                  5  create ancillary order services (yours)
                                                  6  POST Service/v1/Ancillaries/Hold ------->  inventory hold, returns holdId + unitIds
                                                     POST Service/v1/Ancillaries/SeatMaps/Holds (seats)
                                                  7  payment + EMD issuance (yours)
                                                     POST Service/v1/Ancillaries/Hold/{holdId}/Confirmations
                                                  8  abandon: DELETE Service/v1/Ancillaries/Hold/{holdId}
                                                     after confirm: POST …/Hold/{holdId}/Cancellations
```

`{S}` is `Backoffice`, `OtaPanel` or `Api`:
- **Backoffice** may shop any order.
- **OtaPanel and Api** may shop only an order whose `sales.travelAgencyId` equals the caller's agency and whose `customerId` equals the caller's customer. Otherwise the answer is 403 / 16809.

The same flow exists before an order exists. It uses the offer routes (`POST {S}/v1/AncillaryOffers {offerId}`, `…/Price`), and Resolve then returns `orderId: null`.

---

## 4. Step by step

### 4.1 Shop the order (customer surface)

**Offers.** `POST {S}/v1/AncillaryOffers/Orders` with body `{ "orderId": "1552605112424202240" }`.

The answer is `orderId`, `orderCommercialVersion`, `currencyId` and ten family arrays:
- `baggages`, `meals`, `pets`, `assistedTravel`, `airportServices`;
- `priorityServices`, `connectivity`, `upgrades`, `extraSeats`, `travelInsurance`.

Every family is a list of variants (`variantCode`, `title`), and every variant a list of products (`serviceDefinitionRef`, `title`, `details`). Every product is a list of unit offers:

| Unit offer field | Meaning |
|---|---|
| `offerItemId` | the id to buy: `d_…` direct (choose a quantity) or `r_…` reference (fill the `options` form first). Signed, bound to this order version and to the sales context |
| `selectionScope` | `PerOrder`, `PerTraveller`, `PerTravellerPerPortion`, `PerTravellerPerFlight` |
| `unit` | `{ "scope": "ORDER" }`, `{ "travellerRef" }`, `{ "travellerRef", "portionRef" }` or `{ "travellerRef", "flightRef" }` |
| `coverage` | `travellerRefs`, `portionRefs`, `flightRefs` covered by the unit |
| `unitPrice` | price of one unit |
| `quantityPolicy` | allowed quantities |
| `options` | for `r_` ids: the fields the customer must fill (family specific) |

**Seats.** `POST {S}/v1/SeatMaps/Orders` with body `{ "orderId": "…" }`. The Backoffice body may also carry `options`.

The answer is the same `SeatMapsDto` as the offer seat map. Every available seat has one `travelerPricing[].id` per traveller: the seat-map selection id.

### 4.2 Price the basket (customer surface)

`POST {S}/v1/AncillaryOffers/Orders/Price`

```json
{
  "orderId": "1552605112424202240",
  "selections": [
    { "offerItemId": "d_eyJ…", "quantity": 1 },
    { "offerItemId": "r_eyJ…", "options": { "baggage": { "weightKg": 23 } } }
  ]
}
```

| Rule | Detail |
|---|---|
| `selections` | 1 to 50 lines; each `offerItemId` once |
| `quantity` | only for `d_` ids |
| `options` | only for `r_` ids, one family per line: `baggage`, `pet`, `assistedTravel`, `airportService`, `priority` or `connectivity` |
| what Ancillary checks | re-reads the order, re-proves every id against the current order and catalog, checks basket conflicts (for example two main meals) |

The answer is `{ "selectionId": "s_…", "expiresAtUtc": "…" }`. The selection is stored with `orderId` and `orderCommercialVersion`.

`GET {S}/v1/AncillaryOffers/Orders/Price/{selectionId}` shows the priced basket again in the shopping shape, with `selected` filled. Possible errors:
- 404 / 16819 if the id belongs to an offer selection;
- 410 / 16817 after expiry;
- 409 / 16820 if the order changed since pricing (another commercial version or other order facts).

### 4.3 Hand over to Ordering

The customer surface sends Ordering the `selectionId` (and seat-map selection ids for seats). This endpoint is yours to design.

### 4.4 Resolve the selection (Ordering → Ancillary)

`POST Service/v1/AncillaryOffers/Resolve` with body `{ "selectionId": "s_…" }`.

Resolve reads only the stored selection and the current catalog. It does not re-read the order. It fails as follows:
- 409 / 16820 when a price, rule or product is no longer active;
- 410 / 16817 when expired;
- 404 / 16819 when unknown.

The answer is:

```text
{
  orderId, orderCommercialVersion,          (null for an offer selection)
  baggages[], meals[], pets[], assistedTravel[], airportServices[],
  priorityServices[], connectivity[], upgrades[], extraSeats[], travelInsurance[]
}
```

Each item has these fields:

| Field | Content |
|---|---|
| `offerItemId`, `variantCode`, `selectionScope`, `unit`, `coverage` | as in shopping |
| `sourceBinding` | `{ offerHash, salesHash }`: the source and sales bindings of the signed id. Keep them for audit; for an order source you cannot recompute `offerHash` |
| `definition` | `definitionVersionId`, `serviceDefinitionRef`, `variantCode`, `serviceTypeCode`, `serviceSubCode`, `rfic`, `rfisc`, `ssrCode` |
| `provision` | `provisionId`, `coverageScope` (`Sector`, `Portion`, `Journey`, `Order`), `purchaseStage`, `bookingMethod`, `bookingRequired`, `confirmationRequirement`, `fulfillmentProviderKey`, `documentRouting` (`NoAncillaryDocument`, `Emd`, `TicketOrExchange`), `documentType` (`None`, `EmdAssociated`, `EmdStandalone`), `documentRequired`, `requiresCheckAtReserve` |
| `pricing` | `pricingId`, `pricingRateId`, `pricingVersion`, `currencyId`, `quantity`, `unitAmount`, `totalAmount`, `pricingUnit`, `baseAmount`, `taxLines[]`, `feeLines[]` (`code`, `amount`, `currencyId`, `countryId`, `stationAirportId`, `taxTreatment`, `feeApplicationUnit`) |
| `details` | family details (weights, meal code, lounge airport and so on) |
| `selected` | `quantity` and the chosen `options` |

Check before you store anything:

1. `orderId` equals your order.
2. `orderCommercialVersion` equals the order's current commercial version. If it doesn't, ask the customer to price again.

**Seats.** `POST Service/v1/Ancillaries/SeatMaps/Resolve` with body `{ "ids": ["<seat-map selection id>", …] }`.

The answer is `seats[]`. Each seat carries `id`, `travellerRef`, `flightId`, `seatNumber`, plus:
- `serviceDefinition`: the full definition with its seat specification;
- `provision`;
- `pricing`: with all its `rates`, each with `currencyId`, `baseAmount`, `unitTotal` and `components`.

Seat Resolve is a pure catalog lookup. Seat eligibility and occupancy are checked again at the seat Hold.

### 4.5 Create the ancillary order services (Ordering)

Create one order service per Resolve item, or per seat, with a new `orderServiceId`. The fields to keep are in §5. The `orderServiceId` is the key Ancillary uses for every later hold call.

### 4.6 Hold (Ordering → Ancillary)

**General hold.** `POST Service/v1/Ancillaries/Hold` answers 201 Created.

```json
{
  "idempotencyKey": "ord-1552605112424202240-chg-7",
  "orderId": "1552605112424202240",
  "reference": "ORDER-CHANGE-7",
  "requestedExpiresAt": "2026-11-02T10:30:00+03:30",
  "baggages": [
    { "variantCode": "A01", "products": [
      { "provisionId": "1564…", "units": [
        { "orderServiceId": "1570…01",
          "unit": { "travellerId": "1552605112482922496" },
          "coverage": { "flightIds": ["1544009211417985664"] },
          "selected": { "quantity": 1 } } ] } ] }
  ],
  "meals": [
    { "variantCode": "A11", "products": [
      { "provisionId": "1564…", "units": [
        { "orderServiceId": "1570…02",
          "unit": { "travellerId": "1552605112482922496" },
          "coverage": { "flightIds": ["1550116065948729635"] },
          "selected": { "quantity": 1 } } ] } ] }
  ],
  "priorityServices": [
    { "variantCode": "A23", "products": [
      { "provisionId": "1564…", "units": [
        { "orderServiceId": "1570…03",
          "unit": { "travellerId": "1552605112482922496" },
          "coverage": { "flightIds": ["1544009211417985664"] },
          "selected": { "quantity": 1, "options": { "priority": { "optIn": true } } } } ] } ] }
  ]
}
```

| Field | Rule |
|---|---|
| `idempotencyKey` | 1 to 128 characters; the same key with the same content replays the stored receipt; the same key with other content gives 409 / 16403 |
| `orderId` | > 0 |
| `reference` | 1 to 128 characters, your own reference (for example the order change) |
| `requestedExpiresAt` | later than now; it becomes the hold expiry |
| families | `baggages`, `meals`, `assistedTravel`, `airportServices`, `priorityServices`, `connectivity`, `upgrades`; there is **no** `pets`, `extraSeats` or `travelInsurance` |
| `variantCode` | must equal the variant of the provision's product |
| `provisionId` | from Resolve `provision.provisionId`; must be Active |
| `orderServiceId` | > 0, once per request, and not in another live hold (409 / 16406) |
| `unit.travellerId` | your travellerId (= Resolve `travellerRef`); required unless `coverageScope` is `Order`, and forbidden for `Order` |
| `coverage.flightIds` | your segment `flightId`s, distinct; `Sector`: exactly one; `Portion`: the flights of that bound; `Journey`: the flights of the itinerary; `Order`: none |
| `selected.quantity` | inside the provision's quantity rule; may be omitted only when the rule has min = max |
| `selected.options` | `baggage { bagRef, measuredWeightKg, dimensions, weightKg }`, `assistedTravel { assistanceSsrCode }`, `airportService { timeWithOffset, guestCount }`, `priority { optIn }` (`optIn: true` is required for A23), `connectivity { deviceCount }`; meals and upgrades take none. Copy them from Resolve `selected.options` |

The Hold verifies the catalog (provision, product, variant and options) and the inventory. It does **not** re-price and does **not** re-read the order:
- **Prices** always come from Resolve.
- **Ids** (travellers and flights) are Ordering's responsibility.

The answer, also returned by every later hold call:

```json
{ "holdId": "1571…", "reference": "ORDER-CHANGE-7", "status": "Held", "expiresAt": "2026-11-02T07:00:00+00:00",
  "units": [ { "unitId": "1571…11", "orderServiceId": "1570…01", "status": "Held" } ] }
```

Store `holdId` on the order change and `unitId` on each order service.

**Seat hold.** `POST Service/v1/Ancillaries/SeatMaps/Holds` answers 201 Created. Ancillary re-checks every seat with the order facts you send:

```json
{
  "idempotencyKey": "ord-1552605112424202240-seat-1",
  "orderId": "1552605112424202240",
  "orderCommercialVersion": 1,
  "reference": "ORDER-CHANGE-7",
  "requestedExpiresAt": "2026-11-02T10:30:00+03:30",
  "pointOfSaleId": "1",
  "currencyId": 70,
  "ticketedAtUtc": null,
  "holdId": null,
  "seats": [
    { "orderServiceId": "1570…04", "travellerId": "1552605112482922496",
      "seatMapSelectionId": "<id from the seat map>", "passengerTypeCode": "ADT", "dateOfBirth": "2000-09-23",
      "flight": { "flightNumber": "1597", "marketingAirlineId": 1, "operatingAirlineId": 1, "originAirportId": 2,
                  "destinationAirportId": 6, "departureAt": "2026-12-27T13:13:00+03:30", "arrivalAt": "2026-12-27T15:15:00+03:00",
                  "aircraftId": 1, "cabinClassId": 1, "rbdId": "25" },
      "fare": { "airFareId": "1469435125056929792", "airFareType": "Public", "fareBasis": "YIKAIST/OW", "bookingClass": "Y" },
      "couponOpenForUse": null }
  ]
}
```

| Rule | Detail |
|---|---|
| `orderCommercialVersion`, `pointOfSaleId`, `currencyId` | required, > 0 |
| `couponOpenForUse` | required on every seat when `ticketedAtUtc` is set |
| adding seats to an existing hold | set `holdId`, and repeat the same `orderId`, `reference` and `requestedExpiresAt` |
| failure | a seat that is no longer sellable answers 422 / 16415 with the reason |

### 4.7 Confirm (after payment and EMD issuance)

`POST Service/v1/Ancillaries/Hold/{holdId}/Confirmations`, no body. Every Held unit becomes Confirmed.

It is idempotent: calling it again on a Confirmed hold returns the same receipt. It fails with 409 / 16407 after expiry, or with 16412 / 16413 when the hold was released or cancelled.

### 4.8 Release, expiry and cancellation

| Action | Call | Applies to |
|---|---|---|
| Release (customer abandons before confirm) | `DELETE Service/v1/Ancillaries/Hold/{holdId}` (whole hold) or `…?unitIds=1&unitIds=2` | Held units; idempotent |
| Expiry | automatic at `expiresAt` by Ancillary's poller | Held units |
| Cancel (after confirm, for example a refund) | `POST Service/v1/Ancillaries/Hold/{holdId}/Cancellations` with `{ "reasonCode": "VOLUNTARY_REFUND", "unitIds": null }` (null = all) | Confirmed units; idempotent with the same reason |

### 4.9 Read a hold

`GET Service/v1/Ancillaries/Hold/{holdId}` returns:
- `holdId`, `orderId`, `reference`, `status` (`Held`, `Confirmed`, `Released`, `Expired`, `Cancelled`) and `expiresAt`;
- the units grouped by family → variant → product (`provisionId`, `serviceDefinitionRef`);
- per unit: `unitId`, `orderServiceId`, `status`, `cancellationReasonCode`, `unit`, `coverage`, `selected`, and for seats `seatNumber` and `seatMapSelectionId`.

---

## 5. What Ordering must keep

### 5.1 Per ancillary order service

| Group | Fields | From | Used for |
|---|---|---|---|
| identity | `orderServiceId`, service type `Ancillary` (today your `OrderServiceType` has only `AirTransportation`) | yours | every hold call, your AncillaryContext |
| who and where | `travellerId` (null for order-level services), covered segments or air services (`flightId` per segment) | Resolve `unit`, `coverage` mapped with §2 | Hold `unit` and `coverage`; your AncillaryContext `coveredAirServiceIds` |
| product | `serviceDefinitionRef`, `definitionVersionId`, `variantCode`, family, `serviceTypeCode`, `serviceSubCode`, `rfic`, `rfisc`, `ssrCode` | Resolve `definition` | EMD (RFIC/RFISC), SSR to the DCS, display |
| rule | `provisionId`, `coverageScope`, `purchaseStage`, `bookingMethod`, `bookingRequired`, `confirmationRequirement`, `fulfillmentProviderKey`, `documentRouting`, `documentType`, `documentRequired`, `requiresCheckAtReserve` | Resolve `provision` | Hold, document decision (EMD-A, EMD-S, ticket exchange or none) |
| price | `pricingId`, `pricingRateId`, `pricingVersion`, `currencyId`, `quantity`, `unitAmount`, `totalAmount`, `baseAmount`, `pricingUnit`, tax lines, fee lines | Resolve `pricing` | payment, EMD amounts, revenue accounting |
| choice | `quantity` and the chosen options per family | Resolve `selected` | Hold `selected` |
| source | `selectionId`, `orderCommercialVersion` at pricing, `offerHash`, `salesHash` | Price answer, Resolve | audit; version check |
| reservation | `holdId`, `unitId`, unit status, `expiresAt`, `idempotencyKey`, `reference` | Hold receipt | confirm, release, cancel |
| seat only | `seatNumber`, `seatMapSelectionId`, `flightId` | Seat Resolve | seat hold, boarding pass |
| state | your commercial state (`Active`, `Replaced`, `Cancelled`, `Transferred`) and document numbers (EMD) | yours | your AncillaryContext `ancillaryServices[].commercialState` |

### 5.2 Facts the seat hold needs from your order

Your order must give, per traveller and segment:
- the traveller: `passengerTypeCode` and `dateOfBirth`;
- the flight: `flightNumber`, `marketingAirlineId`, `operatingAirlineId`, `originAirportId`, `destinationAirportId`, `departureAt`, `arrivalAt`, `aircraftId`, `cabinClassId` and `rbdId`;
- the fare: `airFareId`, `airFareType`, `fareBasis` and `bookingClass`;
- the order: `couponOpenForUse` once ticketed, and the order's `pointOfSaleId` and `currencyId`.

---

## 6. Coverage mapping cheat sheet

| Resolve `provision.coverageScope` | Resolve `unit` | Hold `unit` | Hold `coverage.flightIds` |
|---|---|---|---|
| `Order` | `{ scope: "ORDER" }` | omit | omit |
| `Journey` | `{ travellerRef }` | `{ travellerId }` | every segment flight of the itinerary |
| `Portion` | `{ travellerRef, portionRef: "B1" }` | `{ travellerId }` | every segment flight of bound `B1` |
| `Sector` | `{ travellerRef, flightRef: "B1:F1" }` | `{ travellerId }` | the one flight of bound `B1`, segment sequence 1 |

---

## 7. What can be held today

The Hold refuses a product when any of the following is true (error 409 / 16419 or 422 / 16104):

- the product needs supplier confirmation (`confirmationRequirement = SubjectToConfirmation`);
- the variant is A17 medical equipment, A18 bassinet or A19 unaccompanied minor;
- the provision is priced by an external quote (`ExternalQuote`);
- the product's supplier is External (no fulfillment provider is connected yet);
- the product has no Active inventory policy, or its inventory authority is `Supplier` (seats need no policy);
- the family is pets, extra seats or travel insurance (not part of the hold request).

On the Staging catalog seeded on 2026-10-28, 27 of the 63 products can be held. This was checked with SQL against `db-test.alibaba.local/DotAirAncillary`:

| Variant | Holdable products |
|---|---|
| A01 extra bag | `BAG_EXTRA_23`, `BAG_EXTRA_32` |
| A02 extra weight | `BAG_WEIGHT_5`, `BAG_WEIGHT_10`, `BAG_WEIGHT_20` |
| A03 overweight | `BAG_OVERWEIGHT` |
| A05 cabin bag | `BAG_CABIN_EXTRA` |
| A07 standard seat | `SEAT_ECO_STANDARD`, `SEAT_PREMIUM_ECONOMY`, `SEAT_BUSINESS`, `SEAT_FIRST` |
| A08 preferred seat | `SEAT_ECO_PREFERRED`, `SEAT_ECO_LEGROOM`, `SEAT_ECO_EXIT_ROW`, `SEAT_BUSINESS_EXIT_ROW` |
| A11 special meal | `MEAL_VGML`, `MEAL_AVML`, `MEAL_MOML`, `MEAL_KSML`, `MEAL_DBML`, `MEAL_GFML`, `MEAL_CHML`, `MEAL_BBML` |
| A16 disability assistance | `ASSIST_BLND`, `ASSIST_DEAF`, `ASSIST_DPNA` |
| A23 priority | `PRIORITY_BOARDING`, `PRIORITY_CHECKIN` |

The other products can be shopped, priced and resolved, but not held:
- oversize and sports equipment;
- extra seats;
- upgrades;
- paid a-la-carte meals;
- pets;
- wheelchairs;
- medical;
- bassinet;
- minors;
- lounges, fast track and CIP;
- Wi-Fi.

---

## 8. Errors you will meet

| Code | HTTP | When |
|---|---|---|
| 16801 | 422 | invalid shopping input (for example `orderId is required`) |
| 16809 | 403 | caller may not shop this order, or the order is unknown |
| 16815 | 503 | your AncillaryContext could not be read (timeout, non-2xx, rule of §2) |
| 16816 | 422 | an offer item id is not a valid signed id |
| 16817 | 410 | the offer item id or the selection expired |
| 16818 | 403 | the id was issued to another sales context |
| 16819 | 404 | selection not found (or the wrong kind: offer versus order) |
| 16820 | 409 | not sellable any more (order changed, price or rule retired, basket conflict) |
| 16821 | 422 | invalid selection options |
| 16402 | 422 | invalid hold field (the field name is in the message) |
| 16403 | 409 | idempotency key reused with other content |
| 16406 | 409 | the order service already has a live reservation |
| 16407 | 409 | the hold has expired |
| 16409 | 422 | quantity outside the provision rule |
| 16410 | 422 | hold coverage does not match the provision coverage |
| 16411 / 16412 / 16413 / 16414 | 409 | hold already confirmed / released / cancelled / not confirmed |
| 16415 | 422 | seat cannot be held (reason in the message) |
| 16416 | 409 | another hold call is running for the same order; retry |
| 16417 | 409 | not enough inventory |
| 16418 | 404 | a unit id does not belong to the hold |
| 16419 | 409 | product not holdable (see §7) |
| 16104 | 422 | the product's supplier is external and not connected |

---

## 9. Recommended order of work for Ordering

1. Add the ancillary order service type and the fields of §5.1.
2. Add an endpoint that receives a `selectionId` (and seat-map selection ids), calls Resolve, checks `orderId` and `orderCommercialVersion`, and creates the order services.
3. Call Hold, keep `holdId` and the `unitId`s; release the hold when the customer abandons.
4. Take payment, issue EMDs where `documentRequired` is true (RFIC/RFISC from the definition), then call Confirmations.
5. Return the new services in your AncillaryContext `ancillaryServices[]`, but only after Ancillary can read them (see §10, item 1).
6. Use Cancellations when a confirmed ancillary is refunded or removed.

---

## 10. Open points between the two services

1. **`ancillaryServices[]` in your AncillaryContext.** Ancillary refuses any non-empty array with 503 / 16815 until the mapping is added (Owner ruling R2-4, planned with your Stage 6). Once you return the services you have added, the shopping routes for that order stop working. Coordinate the release with the Ancillary side.
2. **Prices at hold time.** The Hold does not re-price. Keep the Resolve price, and re-resolve if your process waits long enough for the selection to expire.
3. **Order changes between Price and Hold.** Resolve does not re-read the order. Compare `orderCommercialVersion` yourself (§4.4).
4. **Products that cannot be held (§7).** Decide whether Ordering offers them as "on request" outside the hold, or hides them, until Ancillary connects external suppliers and supplier confirmation.
5. **Point of sale for the seat hold.** Your AncillaryContext carries no point of sale, but the seat hold needs `pointOfSaleId`. Ordering must know the sale's point of sale.
