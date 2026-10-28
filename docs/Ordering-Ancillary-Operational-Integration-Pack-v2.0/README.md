# AeroTech Ordering — Stage6A + conditional Stage6B operational integration Pack v2.0

**Date:** 2026-10-28. **Scope:** all 24 variants A01–A24 including SeatMap; real Order commercial Create/Add, plus minimum Reserve/Cancel/Issue safety and source-gated true operational completion. This replaces all prior 6A v1.0/v1.1 packs; **do not merge older prompts/DTO requirements**.

## Read order

| Document | Purpose |
|---|---|
| [00-START-HERE](00-START-HERE.md) | final frozen decisions and readiness taxonomy |
| [01-VERIFIED-SOURCE-DELTA](01-VERIFIED-SOURCE-DELTA.md) | source SHA, review corrections, old/new baseline limitations |
| [02-AUDITED-INDUSTRY-BENCHMARK](02-AUDITED-INDUSTRY-BENCHMARK.md) | IATA, Amadeus, Sabre, Travelport, Duffel, flydubai, Breeze |
| [03-CANONICAL-DOMAIN-AND-FIELDS](03-CANONICAL-DOMAIN-AND-FIELDS.md) | Order DDD fields, enums, beneficiary, coverage, selected choices |
| [04-HTTP-CONTRACTS-AND-FLOWS](04-HTTP-CONTRACTS-AND-FLOWS.md) | Create/Add across 3 surfaces, versioning, status and errors |
| [05-QUOTE-MAPPING-PRICING](05-QUOTE-MAPPING-PRICING.md) | source-bound selection, exact taxes/fees/money/coverage |
| [06-SEATMAP-COMMERCIAL-AND-POS](06-SEATMAP-COMMERCIAL-AND-POS.md) | truthful Seat quote and PointOfSale, physical-vs-commercial boundary |
| [07-RESERVE-CANCEL-ISSUE-OPERATIONAL-CLOSURE](07-RESERVE-CANCEL-ISSUE-OPERATIONAL-CLOSURE.md) | early safety guards, cancellation and actual 6B provider/document gates |
| [08-ORDERING-CONTEXT-AND-SECOND-SHOPPING](08-ORDERING-CONTEXT-AND-SECOND-SHOPPING.md) | extend existing local AncillaryContext; populated Order repeat shopping |
| [09-A01-A24-COMPLETE-VARIANT-MATRIX](09-A01-A24-COMPLETE-VARIANT-MATRIX.md) | exact all-24 family/variant/source/operation matrix |
| [10-PERSISTENCE-AND-MASTER-CODE-CHANGE-MAP](10-PERSISTENCE-AND-MASTER-CODE-CHANGE-MAP.md) | .NET/EF/CQRS source file and migration plan, reports and CI |
| [11-ANCILLARY-COMPANION-HANDOFF](11-ANCILLARY-COMPANION-HANDOFF.md) | AC-01…AC-07 exact companion-service requests |
| [12-ORDERING-CODING-AGENT-PROMPT](12-ORDERING-CODING-AGENT-PROMPT.md) | exact executable agent instructions and stage gates |
| [13-MASTER-CLAUDE-CI-AUTHORITY-AMENDMENTS](13-MASTER-CLAUDE-CI-AUTHORITY-AMENDMENTS.md) | in-place Master/CLAUDE/canonical-stage source of truth |
| [14-GATE-AND-REVIEW-CLOSURE-MATRIX](14-GATE-AND-REVIEW-CLOSURE-MATRIX.md) | B1…B4, C1…C11, Q1…Q9 closure, acceptance and rollout |

## References, for audit only

- `reference/REVIEW-v1.1-2026-10-28.md` — owner-provided review, all blockers and findings.
- `reference/ANCILLARY-INTEGRATION-GUIDE-2026-10-28.md` — source handoff guidance.
- **The old Master copy is not included.** Agent reads current local `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md` in place and never resets unpushed owner work.

## Critical reality check

- **Ordering remote verified:** `cd50a2a5fd18a372f32f2d4efac08dee0510f6ba`; owner review reports **unpublished** local `6a7bb71` with existing AncillaryContext. The pack is not falsely claimed rebased on uninspectable local source.
- **Ancillary latest verified:** `190aa74eadb103d8556a9fab03c469cb937160e8`; proposed AC-01…AC-07 are **not currently implemented** by just writing this pack.
- **Coverage:** 24/24 typed Domain mapping; some source/hold/document variants are not operationally supported yet. `Domain PASS` and `HTTP/Operational PASS` are separate verdicts. Never open an airline sell path on fixture-only proof.
- **No extraneous fields:** do not invent `pricedForTravelAgencyId`, client-facing idempotency keys or a receipt table; existing authorization retained and later entitlement policy separated.

## Shortcut for implementation

1. Hand the agent file `12-ORDERING-CODING-AGENT-PROMPT.md` and all other pack docs; keep the current local Master and CLAUDE nearby.
2. Send `11-ANCILLARY-COMPANION-HANDOFF.md` separately to Ancillary team; **do not** let Ordering agent implement it in companion repo.
3. Each phase has a precise exit verdict. Verify `reports/` artifact evidence rather than trusting a positive natural-language summary.
