# 07 — Mandatory operational closure: Reserve, Confirm, Cancel, Issue, EMD

**Owner instruction 2026-10-28:** Reservation and Issue may be included where required to make an airline-safe system. We do **not** pretend they already work. Stage6A code prevents inconsistent behavior immediately; Stage6B makes real active product flows operational and is **required before production release** of that product. Both phases are parts of this **one** final Pack, not an indefinite unplanned follow-up.

## 1. Proven B1 failures on current Ordering source

1. `ReserveService.RequiredServices(order) => order.Services.Where(_providers.RequiresReservation)`; `RequiresReservation` invokes `CapabilityOf` and `Resolve(service.FulfillmentProviderKey)`. No `Ancillary` provider => `NoFulfillmentAdapterRegistered` (2505, HTTP 500), *not* a controlled retail operation error. See current `ReservationProviderResolver.cs` and `ReserveService.cs`.
2. `Order.CancellationScopeOf` includes only Air, dependent Infant Air, and associated Seat. A newly accepted ancillary is **not** canceled by `Cancel(order,[])` and Air cancellation leaves dependent ancillary Active.
3. `IssueOrderService` and `IssuancePlanner` issue ETKT for Air. `ElectronicMiscDocument` has Master schema and message placeholders, **not a proved working EMD issuance pipeline**. Marking the Air Ticketed cannot mean the optional service is documented or fulfilled.
4. `FlightFlowReservationProvider.PlanUnits` groups Air + `OrderSeatService` per passenger/flight into a single physical unit. It already can carry a Seat number, but **an accepted Seat's source POS/price must be preserved**, and double-physical hold must be avoided.

## 2. Stage6A mandatory minimal safety behavior (implement with commercial acceptance)

### R1. Reserve early gate — both whole order and targeted reserve

**Before** calling `_providers.RequiresReservation`, `_providers.CapabilityOf`, `_providers.Resolve`, reading an external provider, or scheduling a FulfillmentTask:

- Determine the **active** services included in the *requested* reserve scope **and their required dependency closure** (Air, Seat and ancillary that must be fulfilled with Air). For whole-Order Reserve, scope is **all active required Order Services**.
- If any purchased general Ancillary **or purchased Seat lacking verified commercial/physical fulfillment** has no **verified** registered provider/capability with actual downstream implementation, raise `ANCILLARY_OPERATION_NOT_READY`, HTTP 409 / proposed code 2831; **zero provider calls, zero reservations, zero tasks, zero changes**.
- Do **not** register a fictitious provider `ReservationMode.None` or silently omit Ancillary from the plan to let Air confirm with purchased but unfulfilled services.
- If a known product is truly `BookingRequired=false`, distinguish that from `fulfillment complete`: a source-approved no-booking/no-document product may not need a provider, but that must be a **verified explicit product rule**, not inferred because an adapter is missing.
- Add a **guard also to Confirm and Issue**: cannot call Confirm/Issue for a basket with unresolved mandatory ancillary fulfillment. **A07/A08 purchased Seat must be part of this gate even though its ProviderKey=FlightFlow is registered**: that provider can reserve the *physical* seat, but does not prove Ancillary commercial Seat Hold/document fulfillment. If source explicitly proves no additional commercial Hold or DocumentRequired, FlightFlow confirmation plus accepted source price can satisfy that seat; otherwise block until the full paired strategy is verified. Read-only quote is not itself a reservation.
- For **targeted** Reserve of Air only when dependent active Ancillary exists, either include the complete dependency closure in plan or reject typed, never bypass via requestedServiceIds subset. Scope rules are shared with full Reserve.

### R2. Cancel correctness now, before full reservation support

- `Cancel(order,[])` includes **every active** Air, Seat, purchased baggage and all other Ancillary services. If there are zero reservations, the command can cancel them all within existing Order/Snapshot code; no provider calls.
- Partial cancel of Air includes its dependent A07/A08 Seats; also includes dependent Ancillary where its **entire** source-backed coverage is contained within the ending AirServices. A single ancillary spanning two flights must not be left Active when one covered flight is removed.
- Where a multi-flight Ancillary overlaps *some but not all* ending AirServices, or is Order-wide/Traveller-wide with ambiguous dependency, **fail closed** with `ANCILLARY_PARTIAL_CANCEL_REQUIRES_REQUOTE` (409) until a sourced servicing/pricing decision exists. Do not arbitrarily cascade whole-product cancellation or prorate a booking-wide fee.
- Pure ancillary-only cancellation before reservation is permitted by **scope rules** if the product can be ended independently; legacy Seat-only cancellation remains explicitly blocked until a validated seat release/replace route exists. Do not expand this into post-confirmed refund/exchange by accident.
- If an affected service has held/confirmed capacity or issued document, current planner must settle/restrict it using verified provider capabilities; do not change commercial state until settlement. Unknown provider effect => no premature cancellation.

### R3. What happens to historical accepted price after Cancel?

The v1.1 review claimed Stage5 has a Pricing reversal rule; **current `Order.Cancel` does not produce reversal lines or change `CustomerTotal`**. Under Master history semantics, the original accepted PricingLines are immutable **and remain part of historical accepted total**. Therefore:

1. **Mandatory**: after whole-order cancel, zero active Services/Items remain, and `OrderStatus.Cancelled` is accurate; historical prices, changes and source references remain readable.
2. **Never** represent `CustomerTotal` as *current refund amount* or *amount due* after Cancel; it is the last accepted sale total under current implementation. UI/consumer must distinguish it from payment/credit/debt.
3. Do **not** append fabricated negative Tax/Fee/Base lines during Stage6A cancellation; a future authoritative `Refund/Reshop/Settlement` policy creates reversal/credit with reason/source references. If the product must refund automatically on Cancel, that product is **NOT live-sellable** until refund policy/JetPay/EMD behavior is available.
4. Test cancellation with no reservation, cancellation with incomplete provider result, and a flight-pricing atom fractured by partial cancellation (Stage5 issue still blocks unsafe ticketing).

## 3. Stage6B — real `Ancillary` reservation provider, minimal adapter design

**Implement only after verifying actual Hold API semantics per product and reliable operation/readback.** Use existing `IReservationProvider`, `FulfillmentReservation`, `ReservationUnit`, `FulfillmentTask` and lock. No new Saga/aggregate. **One Order Service may map to one provider unit, but preserve supplier grouping** if the source legitimately treats several units atomically. Do not infer operation-level atomicity from 201 response alone.

### Source endpoints verified at Ancillary `190aa74`

| Operation | Actual Service endpoint | Relevant behavior to verify in integration test |
|---|---|---|
| General Hold | `POST /Service/v1/Ancillaries/Hold` | Accepts Baggages, Meals, AssistedTravel, AirportServices, PriorityServices, Connectivity, Upgrades; **NOT Pets/ExtraSeats** in input |
| General Hold readback | `GET /Service/v1/Ancillaries/Hold/{holdId}` | Status/unit identities and recovery after timeout |
| General Confirm | `POST /Service/v1/Ancillaries/Hold/{holdId}/Confirmations` | confirmed vs Unknown; replay semantics |
| General Release | `DELETE /Service/v1/Ancillaries/Hold/{holdId}` with optional `unitIds` query | scope; partial release needs actual source proof |
| General CancelConfirmed | `POST /Service/v1/Ancillaries/Hold/{holdId}/Cancellations` with `reasonCode`, `unitIds` | no partial cancel without proven unit matching |
| Seat commercial Hold | `POST /Service/v1/Ancillaries/SeatMaps/Holds` | requires `PointOfSaleId`, OrderCommercialVersion and exact seat id; **not automatically a physical seat** |
| Flight physical Hold | FlightFlow existing `IFlightFlowProvider` / `FlightFlowReservationProvider` | actual seat+air capacity owner; `PlanUnits` currently groups Air+Seat |

### Provider invariants

1. Set `FulfillmentProviderKeys.Ancillary="Ancillary"` for general purchased services (not supplier's arbitrary providerKey). **Only register the real adapter** after capability semantics are verified; before then early guard applies.
2. `PlanUnits` must use stable `OrderServiceId`, exact accepted `VariantCode`, `ProvisionId`, quantity, coverage, typed selected option snapshot. **Never** call Price again during Hold or use mutable catalog definition as sold truth. Source's Hold may independently verify provision validity — a refusal is business failure, not silent repricing.
3. `Hold` is independent of Ordering from the Ancillary side; Ordering orchestrates it, no HTTP cycle. Send requested expiry from actual commercial ticketing/hold rules, not a hardcoded 30/90-second TTL.
4. Operation idempotency from **FulfillmentTask**/reservation intent, not client Add key. Persist provider request and correlation before invoking non-retriable effects, following Stage2/3 pattern.
5. Timeout/HTTP disconnect => `Unknown`, readback by `HoldId` or stable reference if source allows; do NOT blind-retry with changed idempotency key. Confirm/Release/Cancel replay behavior must be source-tested.
6. Some variant demands manual approval, external quote, unavailable stock or unsupported source Hold route. **Do not** call that product `HoldThenConfirm` capability; gate sale or expose a real alternative provider only when implemented.
7. A07/A08: physical FlightFlow hold and ancillary commercial quote must reference **one** seat occurrence. Prove any Ancillary Seat Holds complement, not duplicate, FlightFlow physical inventory; never fabricate a second held seat. `PointOfSaleId` from accepted seat quote survives to hold.
8. For multiple suppliers, operation can succeed partially unless provider explicitly guarantees all-or-none. Existing `IReservationProvider` `BatchResultMode` must match **observed** behavior; plan/release/recovery per unit and test mixed outcomes.

## 4. Stage6B — Issuance and document behavior (not optional for live EMD-required sale)

1. **Air ETKT:** current IssuancePlanner issues ETKT for `OrderAirTransportService`; keep it. Before Issue, verify every active necessary Ancillary has confirmed required fulfillment or an explicitly approved no-booking service contract. Never mark a whole composed purchase "fully fulfilled" from an Air Ticketed status.
2. **EMD-A:** only after corresponding ETKT Coupon exists and association is valid. `ElectronicMiscDocument` + `EmdCoupon` + `EmdPriceLink` + `EmdAssociationHistory` per Master §17; one correct RFIC/RFISC from actual source. Do not infer them from A-code.
3. **EMD-S:** standalone where source `DocumentRouting/DocumentType` says so; no invented ticket coupon association. `NoAncillaryDocument`: do not issue one. `TicketOrExchange`: may require ETKT/change and cannot be disguised as EMD.
4. For source states `DocumentRequired=true` and missing EMD issuer, **sell capability disabled**. A zero-priced Wheelchair/Meal may legitimately need SSR/provision confirmation but not an EMD: require its actual booking/delivery outcome, not blanket "no charge, nothing to do".
5. Payment authorization remains a **channel orchestration/policy** responsibility under Master payment-neutral core. Do not issue unpaid traffic documents from unvalidated actual sales flow. DocumentStock allocates local document numbers only for supported stock type; never invent/simulate EMD number or coupon.
6. Flight+Ancillary with unfulfilled required item must not be reported as completely fulfilled; expose per-service outstanding fulfillment/document facts. Root `OrderStatus.Ticketed` means Air ETKT if that is the agreed summary, **not** that every EMD has been issued. Guard API consumer claims accordingly.
7. **Unknown issuance** and externally issued EMD require provider readback/operation idempotency; no fake success or double issuance. Capacity and document operations are independently tracked.

## 5. Mandatory live proof scenarios before airline rollout

- Air + two regular seats + purchased baggage: Create, priced acceptance, FlightFlow physical Hold, Ancillary baggage Hold, both Confirm, ETKT Issue, EMD-A for baggage when required, positive association, restart/readback, OrderView matches.
- Free SSR meal and wheelchair: accepted zero source price, proper booking/confirmation without invented EMD; verify SSR/service outcome.
- Airport Lounge/CIP depending on document type: explicit provider Hold or immediate confirm, EMD-S if required, release and cancel from actual provider.
- Partial reservation timeout: one side Confirmed and other Unknown => root **not** fully confirmed, recovery/release and no unchecked Issue.
- Cancel all before reserve: all active Order Services cancelled, no orphan; source sale price historical and not misrepresented as refunded.
- Cancel after Hold: positive provider release evidence; failure remains unresolved, not commercial Cancelled.
- Multi-flight baggage: a partial itinerary cancellation fails pending service re-quote; never retains false coverage or charges only half by guess.
- A09 extra seat, A13/A14 pets, A17 medical, A19 UMNR: **never live** until actual provider/approval/document support and typed quote succeed, regardless of Domain tests.

## 6. Release taxonomy

`6A_DOMAIN_ONLY`, `6A_HTTP_COMMERCIALLY_ACCEPTED_BUT_OPERATION_BLOCKED`, `6B_PROVIDER_VERIFIED`, `6B_DOCUMENT_VERIFIED`, `AIRLINE_PILOT_READY` (last requires end-to-end tests, financial policy and operations approval). A Domain PASS without provider never equals airline pilot readiness.
