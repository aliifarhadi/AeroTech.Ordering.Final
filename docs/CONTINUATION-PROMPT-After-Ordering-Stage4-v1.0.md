# CONTINUATION PROMPT — Stage 4 Final Review / Closure

تمام پاسخ‌ها فارسی باشند.

## Role

Act as Airline PSS Product Owner + Airline Ordering Domain Expert + DDD Architect + final source reviewer. Coding Agent only codes and reports; it does not make domain decisions.

## Authority

1. `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`
2. `docs/CODING-AGENT-PROMPT-Ordering-Stage4-Issue-v1.0-FINAL.md`
3. current AeroTech source contracts
4. current IATA/ATPCO/public Amadeus/Sabre semantics

Historical donor/Pack material is evidence only.

## Pre-Stage-4 baseline

Ordering: `k8s-stg@ff51dd04cf7254f97f570744a06368599d8b2a80`
FlightFlow: `k8s-stg@fe70d1cb584765cb5ddff170538d912ad1ca380a`

Stage 1 = CLOSED
Stage 2 = CLOSED
Stage 3 = CLOSED

## Frozen Stage-4 decisions

- Stage 4 = Issue.
- executable profile is Local Document Authority only.
- no fake external issuer.
- EMD execution is SOURCE-GATED and currently deferred because source lacks complete RFIC/RFISC/EMD profile/value/authority.
- SeatAssignment does not imply EMD.
- no FlightFlow ticketing mutation.
- Confirmed FlightFlow capacity remains Confirmed after ETKT.
- no Payment/JetPay/Funding gate.
- ETKT, coupon, price link and DocumentStock are separate domain concepts.
- stock allocation stable by `(IssueFulfillmentTaskId, DocumentRole)`.
- FulfillmentTask generalized to typed targets; no parallel Operation aggregate.
- repeat local Issue returns existing truth and no new number.
- issue eligibility uses commercial/reservation/task/deadline/validation facts, not OrderStatus only.
- stale AirPrice validation refreshes before stock allocation using persisted fare topology.
- current one-ticket-per-traveller plan is capability behavior, not uniqueness invariant.
- lap INF can receive ETKT while consuming no independent seat.
- EMD/external issuer absence does not block local ETKT closure.

## Final review procedure

When Agent reports completion:

1. Fetch actual GitHub HEAD.
2. Diff from `ff51dd04`.
3. Inspect production code, not report only.
4. Inspect migration/backfill.
5. Inspect ETKT aggregate/invariants.
6. Inspect coupon fare/segment/baggage snapshots.
7. Inspect price links against PricingAllocation truth.
8. Inspect stock allocation/idempotency/concurrency.
9. Inspect FulfillmentTask typed target migration and Stage3 compatibility.
10. Inspect reservation Unknown/Mixed/Held issue gates.
11. Inspect LTD.
12. Inspect AirPrice revalidation.
13. Verify one-transaction local issuance.
14. Verify repeat Issue no second task/number/document.
15. Verify zero FlightFlow mutation.
16. Verify zero Payment/JetPay.
17. Verify EMD not fabricated.
18. Verify read side/outbox/event.
19. Verify normal + sabotage tests.
20. Verify DB migration evidence.

Re-benchmark changed/uncertain document semantics against current official IATA/ATPCO/Amadeus/Sabre sources before accepting.

If all pass, explicitly close:

`STAGE_4_CLOSED`

Then explain Stage 5 — Cancel / Void briefly before any Stage-5 coding prompt.

Do not start Stage 5 automatically unless Owner approves.
