# CONTINUATION PROMPT — After Stage 5 R2 Domain Closure

تمام پاسخ‌ها فارسی باشند.

## Baseline before R2

Ordering:
`k8s-stg@94a84a0a435b6394bffbd2713d709992be37f195`

FlightFlow:
`k8s-stg@d2180b2e4c07789abde75e712e870ece8d2c776b`

Stage 1–4 CLOSED.
Stage 5 awaiting R2 final review.

## R2 canonical decisions

1. `OrderChange` has `ReasonText : string?`, max 500, trimmed; Backoffice `ReasonDetail` maps to it.
2. `FulfillmentReservation.Status` is summary, not Issue eligibility.
3. Issue checks exact latest covering `ReservationUnit.Status == Confirmed`.
4. Mixed reservation root may be issueable for a confirmed target subset.
5. Existing accepted pricing must remain intact after servicing.
6. Derived FarePricingAtom:
   - OneWay: each whole FareComponent;
   - RoundTrip/OpenJaw/CircleTrip: whole PricingUnit;
   - Unspecified/Other: whole PricingUnit conservatively.
7. If one pricing atom has both Active and ended Air services, old pricing is fractured and Issue is blocked pending source-authoritative repricing.
8. A through FareComponent spanning multiple segments is indivisible.
9. Reservation validation is canonical scope/version-aware evidence:
   - CommercialVersion
   - ValidUntil
   - ValidatedAt
   - exact ValidatedOrderServiceIds
10. Timestamp alone is insufficient.
11. CommercialVersion change invalidates old evidence.
12. Issue-time refresh is validation-only:
   - no reservation replanning;
   - no `EnsurePlannedAs` against shrunken scope;
   - no FlightFlow resource mutation.
13. Historical timestamp-only rows must not receive guessed scope/version evidence.
14. Master v2.0 is edited in place and remains the single authority.
15. FlightFlow is unchanged.
16. No Payment/Refund/EMD/Stage6.

## Final review procedure

When Agent returns:

1. Fetch actual Ordering HEAD.
2. Diff from `94a84a0a...`.
3. Verify Master v2.0 was edited in place; no v2.1/v2.2.
4. Inspect actual `OrderChange` domain/config/query/API path.
5. Verify ReasonDetail reaches ReasonText and is queryable.
6. Inspect FarePricingAtom implementation.
7. Check OneWay FareComponent atom includes all covered services.
8. Check RoundTrip/OpenJaw/CircleTrip whole-unit atomicity.
9. Check fracture blocks before AirPrice/stock/task.
10. Inspect IssuancePlanner: root reservation status must not gate.
11. Verify exact latest target ReservationUnit must be Confirmed.
12. Inspect new issuance scope granularity.
13. Inspect canonical ReservationValidationEvidence.
14. Verify CommercialVersion + scope + time checks.
15. Verify partial cancellation invalidates prior evidence even with future timestamp.
16. Verify Issue refresh is validation-only and does not call EnsurePlannedAs.
17. Verify historical evidence is not guessed.
18. Verify independent two-one-way partial cancel then Issue works.
19. Verify roundtrip and through-fare partial cancel then Issue block.
20. Verify Mixed root remains Mixed while Order may become Ticketed.
21. Run/inspect R2 tests + Stage1-5 regression.
22. Verify FlightFlow unchanged and no Payment/Refund/EMD/Stage6.

If clean:

```text
STAGE_5_CLOSED
```

Then explain Stage 6 at domain level first. Do not write Stage-6 coding prompt until the domain shape for Ancillary/EMD is fully closed and Owner approves it.

If a gap exists, do not start another broad redesign. Produce one narrow correction only against the already-frozen R2 domain.
