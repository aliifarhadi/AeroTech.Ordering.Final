# 13 — Master/CLAUDE/CI amendments: precise authority and field policy

**Patch the current LOCAL Master in place. Never replace it with the stale Master from v1.1.** Owner review reports its updated local SHA prefix `1e51a948`; this SHA is not independently accessible over GitHub. The coding agent must read that Master on the working machine, record its SHA, and keep any already-added `FareType: AirFareType?`, `Quantity:int` and AncillaryContext contract.

## A. Master v2.0 normative amendment text

### §6.1 and §6.4 — buyer/beneficiary/service grain

- An `OrderService` is one purchased commercial occurrence. Air transport and ordinary Seat require one traveller; **PerOrder optional services have the Order as beneficiary**, `TravellerId:long?` nullable in the abstract base, but `null` is legal only with `SelectionScope=PerOrder` and `CoverageScope=Order`. No fake passenger or `TravellerId=0`.
- A single sold source unit maps to one OrderItem + OrderService, even if it covers several flights. Quantity remains `int>0` in source and Order. Multiple Flight IDs are coverage links, not separate charged occurrences.
- Source `VariantCode`, broad `OrderServiceType`, `ProductType`, `SelectionScope` and `CoverageScope` have **different meanings** and remain separate. Amend the old passenger-only benchmark sentence and any “Exactly one beneficiary occurrence” blanket rule to explicitly include the Order-level exception.
- Source `FulfillmentProviderKey="Ancillary"` for general ancillary is a **future verified provider route**; before actual adapter registration, all service-affecting Reserve/Issue calls fail with a typed business status before provider effects. Do not register fake `ReservationMode.None` or use a supplier name as the system provider key.

### §6.6 — Seat/ExtraSeat

- A07/A08: one Seat per traveller × flight. An actual source-bound seat acceptance quote gives `SeatNumber`, A07/A08 `VariantCode`, chosen complete rate/price, source definition/provision version and `PointOfSaleId`. The purchased seat gets its own Seat OrderItem and *only* one `OrderSeatService` linked to its Air Service.
- Existing legacy SeatSelections remain compatible, but must not be combined with new seat IDs or expanded into multiple silently chosen seats for new path.
- A09 ExtraSeat is its **own product with ticket/seat safety obligations**, never disguised as A07/A08. Until real source `extraSeats[]` is emitted and validated, it cannot be live sold.

### §§6.7 and 6.8 — general ancillary and baggage

- `OrderAncillaryService.Quantity:int`; `CoveredAirServiceIds:0..N` all in the same Order; `TravellerId:long?` order-level exception as above. Persist `ServiceDefinitionRef`, `DefinitionVersionId`, `VariantCode`, `ProvisionId`, `PricingId`, `PricingRateId`, `PricingVersion`, `PricingUnit`, source typed selected values and source-document route/SSRs when actually supplied. No giant unrelated nullable DTO.
- A01–A06 purchased baggage is separate from *included* checked/cabin baggage in Air Fare. Do not spread a multi-flight ancillary into multiple commercial price lines. Preserve exact source fields, units, per-piece/per-kilogram meaning and coverage.
- A09/A17/A19 fields may be present in the source `CustomerSelectionContract` but absent from current Price/Resolve: Domain fixtures must model intended typed truths, **live source acceptance remains BLOCKED** pending actual verified wire. No invented text/Rate/EMD.

### §§7–9 — pricing and history

- One accepted basket during Create is one `OrderChange(Create)` and CommercialVersion=1. Add is one `OrderChange(AddProduct)` and increments CommercialVersion once. Source prices are immutable accepted evidence, allocated to OrderItem/Service.
- Base + **added** taxes + applied item fees = quoted total. Included taxes are already within Base; unapplied non-item fee/unknown tax treatment makes quote incomplete. No invented exchange rate or tax amount.
- Current `Order.Cancel` does not create historical financial reversal; accepted `CustomerTotal` is **not a refund/due balance**. A future explicit refund/settlement policy owns reversal/payment adjustment. Financially charged cancellation is not live-safe until this is resolved; do not invent implicit 100% refund.
- Proposed append-only `OrderPricingReason.AdditionalSale=9`, `OrderPricingLineSubCategory.AncillaryBase=6`, generic `OrderServiceType.Ancillary` only when actual local enum check shows unused values; preserve all earlier frozen enum numeric values. Existing meaningful OrderServiceTypes are reused, but purchased Baggage is never `BaggageAllowance`.

### §§13–14, §17 and Stage6 — fulfillment, documents, lifecycle

- Commercial accepted != reserved/confirmed/documented. Reserve blocks early with typed business 409 for unfulfillable active ancillary **before any provider interaction**. Confirm requires actual reservation. Whole cancellation closes all active services, including ancillary; Air partial cancellation is coverage/dependency-aware, and overlapping multi-flight products fail closed pending real requote.
- ETKT continues to document Air. Real EMD-A requires associated ETKT coupon, EMD-S is standalone, no fake RFIC/RFISC, no EMD for `NoAncillaryDocument`, no unfulfilled SSR assumed done. `TicketOrExchange` is never converted into an arbitrary EMD.
- `OrderStatus.Ticketed` remains an Air-ticket summary until the authoritative broader status is designed; read-side exposes per-service outstanding fulfillment/document truth. Never declare all-ancillary fulfillment merely because ETKT exists.
- **6A:** commercial Create/Add+24 source-typed variants+operational fail-closed guards+correct Cancel. **6B:** genuine Ancillary hold/confirm/cancel/provider recovery, physical FlightFlow Seat, and real EMD/SSR/financial policy as required for production sellable products. No 6B success based only on fixtures.

## B. CLAUDE.md exact amendment points

- Keep lines 76–82: no client servicing idempotency key; use Order lock and server-generated provider keys. Universal duplicate-Create policy remains a separate gap.
- Around line 149: replace active **Stage5** materialization authority with **Stage6A** (Master v2.0 in-place remains domain authority). Stage1–5 are closed and frozen regression authorities, not deleted. Stage6B is the conditional operational stage with tested real provider evidence.
- Around line166: permit owner-authorized source/implementation/test evidence **under repository-root `reports/` only**, while other architecture docs remain governed by existing repo convention. Keep other CLAUDE.md rules (layers, namespaces, reference projects, no comments, no broken API auth).

## C. Canonical guardrails

- Remote `.canonical-stage` currently contains numeric `5`; when Stage6A is activated, set it to numeric **`6`**, not string `6A`. Existing conformance test helper parses an integer.
- Create `canonical-stage6a-v1.0.yaml` by extending the verified Stage5 structure: `version`, `authority`, `stage_file`, `stage6a.types`, `stage6a.abstract_types`, `stage6a.value_objects`, `stage6a.enums`. Build exact actual type inventory from local Domain assembly **after** applying required changes; YAML is a thin name/enum guard, not a substitute for field-level Master.
- Add `tests/AeroTech.Ordering.Domain.ConformanceTests/Stage6ATypeSetConformanceTests.cs`, mirroring Stage5 tests with `Stage=6`. Freeze new enum values and preserve frozen Stage1–5 values. Do not weaken earlier tests and do not use future-only types to make the manifest look full.

## D. Evidence and precedence

- **Current local** Ordering commit and Master win over old remote for implementation. `cd50a2a` is only independently inspected remote baseline; review describes unpushed `6a7bb71` with already-existing AncillaryContext and migrations. Never recreate this route from old instructions.
- Ancillary actual verified source is `190aa74`; source changes AC-01…AC-07 require newer SHA, field-accurate HTTP contract and passing test before they can be claimed as landed.
- Implementation reports under `reports/`: file/line evidence, exact SHA, migration diff, build/tests/HTTP traces, domain vs source vs operational status, unresolved dependencies and explicit stage verdict.
