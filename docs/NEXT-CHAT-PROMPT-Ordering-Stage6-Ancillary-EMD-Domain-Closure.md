# NEXT CHAT PROMPT — Ordering Stage 6 Ancillary / EMD Domain Closure

تمام پاسخ‌ها فارسی باشند.

## Current closed baseline

Ordering:
`aliifarhadi/AeroTech.Ordering.Final`
`k8s-stg@cd50a2a5fd18a372f32f2d4efac08dee0510f6ba`

FlightFlow:
`aliifarhadi/Aerotech.FlightFlow`
`k8s-stg@d2180b2e4c07789abde75e712e870ece8d2c776b`

Status:

`STAGE_1_CLOSED`
`STAGE_2_CLOSED`
`STAGE_3_CLOSED`
`STAGE_4_CLOSED`
`STAGE_5_CLOSED`

Master authority:
`docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`

## Owner objective

هدف یک سرویس Ordering واقعی برای ایرلاین واقعی است، نه پروژه‌ای که فقط commandهای جداگانه‌اش کار کنند.

قوانین:
- هیچ concept بدون actual source/contract یا benchmark معتبر اختراع نشود.
- coding agent فقط کد می‌زند؛ تصمیم domain با این chat/Owner است.
- قبل از coding Stage 6 باید domain shape و flowهای Ancillary/EMD کامل بسته شوند.
- کم‌هزینه‌ترین و کم‌تغییرترین راه ترجیح دارد.
- architecture فعلی Stage 1–5 را بدون source contradiction بازطراحی نکن.
- Payment/JetPay همچنان مستقل و additive است.
- flow correctness از اینجا به بعد اجباری است.

## Known production gaps to keep visible but not mix into Stage 6

1. `OFFER_CREATE_AUTHORITY = NOT_VERIFIED`
2. `CHANNEL_ORCHESTRATION_CONTRACT = SOURCE_GATED`
3. Create duplicate/idempotency policy is not yet authoritative.

Do not solve these by inventing Ordering concepts. Keep them parallel source-contract work.

## Stage 6 task — DOMAIN CLOSURE FIRST, NO CODING PROMPT YET

Do a source + benchmark closure for:

### A. Actual AeroTech source inspection

Inspect actual current:
- Ordering Master + Stage 1–5 source
- AirOffer optional-service / baggage / seat / SSR data
- AirPrice optional-service pricing and servicing contracts
- FlightFlow seat/baggage/ancillary capabilities if any
- any existing EMD/document provider contract
- ReferenceData codes relevant to optional services

If a source is inaccessible, mark the exact item `BLOCKED_SOURCE`.
Do not guess.

### B. Benchmark

Benchmark only business semantics against:
- current IATA Offers & Orders / ONE Order service model
- IATA EMD-A / EMD-S concepts
- ATPCO Optional Services / RFIC-RFISC / service subcodes
- public Amadeus ancillary/EMD behavior
- public Sabre ancillary/EMD behavior

Do not infer proprietary internal aggregate shapes.

### C. Freeze exact Stage-6 scope

At minimum determine source-backed support/disposition for:
- paid seat
- prepaid baggage
- other flight-associated ancillary
- standalone service/fee if source exists
- SSR vs paid commercial ancillary distinction
- EMD-A vs EMD-S
- ancillary coverage to passenger + segment/service
- quantity/unit
- service definition/subcode/group/subgroup
- pricing lines/allocations
- commission/refundability/source terms
- fulfillment provider
- issue prerequisites
- association to ETKT coupon when required
- void/cancel semantics
- later refund/exchange compatibility
- document stock vs external issuer
- history after servicing

For every concept classify:
`NOW`
`SOURCE_GATED`
`DEFER`
`REMOVE/NOT-A-DOMAIN-CONCEPT`

### D. Define canonical flows before code

Must define end-to-end flows, not only commands:

1. Create Order with ancillary already selected in Offer.
2. Add ancillary before ticket issue.
3. Add chargeable ancillary after ETKT issue.
4. Reserve/fulfill ancillary with independent provider outcome.
5. Issue EMD-A linked to ETKT coupon.
6. Issue EMD-S standalone when source supports it.
7. ancillary provider Unknown -> recovery.
8. EMD issuer Unknown -> recovery only if real issuer exists.
9. cancel unissued ancillary.
10. void issued EMD when authority permits.
11. cancel Air service with dependent ancillary.
12. partial itinerary cancellation and ancillary association survival.
13. repeated request/idempotency behavior.
14. seat commercial history vs FlightFlow/DCS operational seat truth.
15. later Refund/Exchange compatibility without implementing Refund/Exchange now.

For each flow define:
- starting state
- commercial mutation
- provider/resource effect
- document effect
- commit pivot
- failure/Unknown semantics
- retry/reconciliation
- final state ownership

### E. Field-level domain closure

Produce exact:
- aggregates affected
- entities/specializations
- value objects
- enums and exact values
- fields/types/nullability
- cardinalities
- invariants
- persistence impact
- source provenance per field

Do not create:
- generic ancillary catalog aggregate unless an actual contract requires it
- generic beneficiary graph
- Payment aggregate
- Workflow/Saga aggregate
- speculative RFIC/RFISC values
- guessed cancellation/refund rules

### F. Practicality gate

Before approving Stage 6, answer:
- Can an airline agent actually sell the supported ancillary?
- Can it be fulfilled/reserved?
- Can required accountable document be issued?
- Can it be safely retried?
- Can it be cancelled/voided without corrupting Order/ETKT history?
- Does the flow work before and after ETKT issue?
- Is there any source gap that makes the feature conceptual only rather than working software?

## Required output

1. concise owner verdict
2. exact Stage-6 domain model delta
3. canonical flow contracts
4. source/benchmark matrix
5. source blockers
6. scenarios/stress tests
7. shortest implementation plan

Only after Owner approval:
create the Stage-6 Coding Agent prompt.

Do not let the Coding Agent decide missing domain semantics.
