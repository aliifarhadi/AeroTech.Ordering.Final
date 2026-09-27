# CONTINUATION PROMPT — Ordering after Stage 5 R2 + Flow Conformance

تمام پاسخ‌ها فارسی باشند.

## Owner objective

هدف «کد کارکن» نیست؛ هدف یک Ordering واقعی برای ایرلاین واقعی است.

هیچ concept جدیدی بدون authority اختراع نشود.
هیچ Stage صرفاً با pass شدن commandهای مستقل بسته نشود.
هر Stage باید در flow واقعی end-to-end هم درست باشد.

اولویت:
1. actual AeroTech contracts/source
2. Master v2.0
3. current IATA / Amadeus / Sabre benchmark
4. donor code only as evidence

کم‌هزینه‌ترین، کم‌تغییرترین و production-practical مسیر را انتخاب کن.

## Reviewed baseline before R2

Ordering:
k8s-stg@94a84a0a435b6394bffbd2713d709992be37f195

FlightFlow:
k8s-stg@d2180b2e4c07789abde75e712e870ece8d2c776b

Stage 1–4 historically CLOSED.
Stage 5 awaiting R2 + flow-conformance final review.

## Frozen architecture

Do not redesign:
- Order = commercial truth
- FulfillmentReservation/ReservationUnit = reservation/resource evidence
- FulfillmentTask = provider mutation/recovery
- ETKT = document truth
- DocumentStock = accountable local stock
- OrderStatus and reservation root status = summaries, not sole eligibility
- Unknown != failure
- exact persisted request = recovery authority
- PNR != provider HoldId
- Cancel != Void != Refund
- Payment is external/additive
- fare/pricing topology preserved from actual source

No generic Workflow/Saga/BusinessOperation aggregate.

## R2 frozen decisions

1. OrderChange.ReasonText : string?, max 500, trimmed.
2. ReasonDetail -> ReasonText.
3. Issue checks exact latest covering ReservationUnit = Confirmed.
4. Mixed reservation root may be issueable for Confirmed target subset.
5. FarePricingAtom:
   - OneWay => whole FareComponent
   - RoundTrip/OpenJaw/CircleTrip => whole PricingUnit
   - Unspecified/Other => whole PricingUnit conservatively
6. Atom with both Active and ended Air services => pricing fractured.
7. Fractured atom => REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION; no Issue.
8. Reservation validation evidence:
   - CommercialVersion
   - ValidUntil
   - ValidatedAt
   - exact ValidatedOrderServiceIds
9. timestamp alone is insufficient.
10. CommercialVersion change invalidates prior validation evidence.
11. Issue validation refresh is validation-only; no reservation replan, no EnsurePlannedAs, no FlightFlow mutation.
12. historical timestamp-only rows are not backfilled by guess.
13. Master v2.0 edited in place; no v2.1/v2.2.

## Mandatory flow view

Final review must inspect:
- FLOW-01 Create -> Reserve -> Confirm
- FLOW-02 Create -> Reserve -> Confirm -> Issue
- FLOW-03 Reserve Unknown -> recovery
- FLOW-04 Confirm Unknown -> recovery
- FLOW-05 Held -> Cancel -> Release -> commercial Cancel
- FLOW-06 Confirmed -> CancelConfirmed -> commercial Cancel
- FLOW-07 scoped CancelConfirmed lost response
- FLOW-08 Issue -> Void -> Cancel
- FLOW-09 late expiry after Issue
- FLOW-10 partial independent OneWay cancel -> issue remainder
- FLOW-11 partial RoundTrip cancel -> repricing required
- FLOW-12 partial through-fare cancel -> repricing required
- FLOW-13 commercial-version change invalidates validation
- FLOW-14 terminal reservation -> new reservation identity
- FLOW-15 repeated Issue safety
- FLOW-16 repeated Cancel/Void safety

## Channel/product audit

Current reviewed surfaces appeared to be:
- Backoffice: granular lifecycle available across controllers
- OTA / OTA Panel: Create/Get/Remarks only
- Internal: reservation operations, incomplete full purchase flow
- Service-to-Service controller largely empty

Do not infer intent.

If no product contract exists, keep orchestration SOURCE_GATED rather than inventing endpoints.

## Final review steps

1. Fetch actual Ordering k8s-stg HEAD.
2. Diff from 94a84a0a...
3. Verify no source drift outside prompt.
4. Inspect Master v2.0 actual edits.
5. Inspect actual production code, not report only.
6. Verify ReasonText path/persistence/query.
7. Verify pricing atom logic against actual fare topology.
8. Check OneWay FareComponent atom includes all covered services.
9. Check RoundTrip/OpenJaw/CircleTrip whole-unit atomicity.
10. Verify reservation root status no longer gates Issue.
11. Verify exact target ReservationUnit must be Confirmed.
12. Verify validation evidence scope/version/time.
13. Verify cancellation invalidates prior evidence through CommercialVersion.
14. Verify Issue refresh has zero reservation-resource mutation.
15. Verify AirOffer acceptance authority from actual source; do not accept guesses.
16. Inspect FLOW-01..FLOW-16.
17. Inspect actual channel matrix/controllers.
18. Verify no generic workflow aggregate.
19. Verify no Payment/Refund/EMD/DCS/Group/Stage6.
20. Re-run source-based regression evidence.

If core/flows are clean:
STAGE_5_CLOSED

Then before Stage 6 coding:
- first close Ancillary/EMD domain and canonical flows
- benchmark against actual AirOffer/AirPrice contracts + IATA/ATPCO + Amadeus/Sabre
- only then issue Stage-6 coding prompt

If any gap exists:
- state exact production consequence
- provide one narrow correction prompt
- do not reopen unrelated closed design
