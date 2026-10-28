# 02 — Audited airline benchmark: what is documented, and what is NOT proved

**Method:** primary public IATA/Amadeus/Sabre/Travelport/Duffel and airline websites were opened/rechecked on 2026-10-28. This document separates **observable API semantics** from **our design choices**. No vendor UI is treated as proof of its internal PSS schema.

| Authority | Verifiable behavior | AeroTech consequence | Strict limit |
|---|---|---|---|
| **IATA Enhanced & Simplified Distribution / NDC implementation guide 21.3** | `OrderCreate` uses selected offer; `OrderReshop` returns offers for changing existing Order; `OrderChange` applies new/changed OfferItems. Worked UC06 explicitly shows flight change cancelling dependent ancillaries. | Create and Add have a shared commercial acceptance kernel; OrderChange history and dependency-aware cancellation; do not conflate OrderChange with confirmation/fulfillment. | Standard messaging model; not proof of AeroTech provider features or a specific REST URL. |
| **Amadeus for Developers** | Price confirmation precedes OrderCreate; chargeable seat is selected per `travelerId` **and `segmentId`** inside `fareDetailsBySegment.additionalServices.chargeableSeatNumber`; priced flight offer then goes to Flight Create Orders. | Exactly one Seat selection per traveller × flight; an authoritative seat price quote must be accepted; external API maps to an Amadeus-inspired `services[]`/offer-item reference, not the internal DTO. | Self-Service cannot be used to claim unrestricted post-ticketed Add or a particular internal DTO. |
| **Sabre NDC API** | `/offers/getSeats`, `/offers/getAncillaries`, `/orders/change`, `/orders/create`, `/offers/price` exist; documented add/cancel seat and ancillary, separate fulfillment actions, pre/post order flows. | Ancillary own shopping/price; Ordering owns Order mutation; a product's fulfillment is a separate capability, never a consequence of merely accepting pricing. | Sabre NDC capabilities differ by carrier/content; API name is not an unconditional capability. |
| **Travelport JSON API 11** | Existing booking ancillary flow: new workbench → ancillary shop → **ancillary price (NDC)** → ancillary book → commit; seats are a separate guide; EMD/FOP depend on carrier. | Quote validation and commit boundary; post-create Add is a real operation, but quote/booking/payment/EMD eligibility must be checked independently. | Not universal for GDS/NDC/carriers; cannot infer Hold/Issue implementation. |
| **Duffel** | Create Order can contain `services`; GET order available services then POST `/air/orders/{id}/services` adds service. Documentation explicitly excludes **hold-type orders** from post-order service addition and from create-with-services in hold cases. | Two entrypoints, source-issued references, and **status/capability-specific** support: not all Orders admit Add. | Duffel's payment-coupled semantics are **not copied** into AeroTech's payment-neutral Order core. |
| **flydubai** | Manage Booking allows extras after booking; paid seats are passenger- and leg-specific; extra seat (EXST/CBBG) requires distinct handling and can carry adult-fare/ticket implications. | A07/A08 regular Seat ≠ A09 ExtraSeat; passenger×flight identity; don't treat generic ancillary as operationally bookable. | Consumer help pages; not internal PSS/API proof. |
| **Breeze** | Bags can differ by outbound/return leg, and paid seats are selected/modified per guest/flight after booking. | Correct source coverage/segment scope and journey-dependent changes; a partial flight change must not silently retain a mismatched product. | Consumer workflow; no proprietary pricing-reservation model disclosed. |

## Reference URLs (primary and independently checked)

- IATA 21.3 Implementation Guide: https://guides.developer.iata.org/docs/21-3_ImplementationGuide.pdf (OrderCreate, OrderReshop, OrderChange, UC02/03/06)
- Amadeus official developer guide: https://github.com/amadeus4dev/developer-guides/blob/master/docs/resources/flights.md (seat/segment/price flow)
- Amadeus FAQ: https://admin.developers.amadeus.com/self-service/apis-docs/guides/developer-guides/faq/
- Sabre NDC Product Collection: https://developer.sabre.com/product-collection/new-distribution-capability-ndc/v1/index.html
- Travelport API Reference: https://support.travelport.com/webhelp/jsonapis/airv11/content/air11/APIReferences.htm
- Travelport Ancillary and EMD Guide: https://support.travelport.com/webhelp/JSONAPIs/Airv11/Content/Air11/Ancillaries/AncillaryGuide.htm
- Duffel Orders: https://duffel.com/docs/api/orders
- flydubai Manage Booking: https://www.flydubai.com/en/book-and-manage/view-or-change-booking/
- flydubai Seat/ExtraSeat: https://www.flydubai.com/en/book-and-manage/add-extras/select-seats/
- Breeze bags: https://www.flybreeze.com/support?a=Add-or-Change-a-Bag---id--j_f8IfxkR5iipZi3ZN8UcQ
- Breeze seats: https://www.flybreeze.com/support?a=Add-or-Modify-Seats-on-an-Existing-Reservation---id--tyEPBgv5SnKpOyGspJOHOw

## Derived ADRs, not alleged vendor rules

1. **Source selection references** are carried from Ancillary to Ordering; customer cannot supply quantity, price, tax, POS, traveler/flight or supplier metadata during final acceptance. (AeroTech design; vendor pattern compatible.)
2. **Operational completeness is separate from accepted commercial total.** No success label indicating held, booked, ticketed, paid or delivered merely because Order Item exists.
3. **A09 cannot ride the A07/A08 seat path.** ExtraSeat may consume another seat/ETKT and airline-specific accounting; its future operational model needs explicit FlightFlow and ticketing proof.
4. **Payment orchestration** stays external as Master mandates. A real carrier's channel may require payment/credit authorization before committing fulfillment; this is an airline deployment policy, not a domain-intrinsic JetPay dependency.
5. **No `pricedForTravelAgencyId`, `pricedForSurface` or similar stopgap fields.** Price/binding evidence belongs to Ancillary's general quote and POS, and existing authorization remains in place.
