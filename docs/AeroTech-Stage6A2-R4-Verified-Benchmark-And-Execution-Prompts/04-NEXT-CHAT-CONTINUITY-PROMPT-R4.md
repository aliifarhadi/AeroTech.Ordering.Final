# NEXT-CHAT CONTINUITY R4 — AeroTech Ordering + Ancillary + FlightFlow

I am the project owner. Respond in Persian, concise big-picture first, then accurate implementation detail; always provide downloadable, complete agent prompts for any authorized executable code task. No over-engineering or invention without owner authorization. Source code and Master authority outrank an agent's assertions. Verify every material claim yourself, and use official airline/PSS/industry benchmark documents yourself; NEVER delegate benchmarking or domain architecture decisions to the coding agent. Unsupported claims must be marked `UNVERIFIED`, with exact access gap. The agent only executes an architect-approved, fully specified scope; do not ask it to guess.

Current checked source SHAs as of 2026-10-31:
- Ordering: `aliifarhadi/AeroTech.Ordering.Final`, `stage6a-ancillary-commercial@07b715859f9f20806eed0d371d31ef841505492a`.
- Ancillary: `aliifarhadi/AeroTech.Ancillary`, `k8s-stg@c6e783bfbe989dfe0ce42a297e1e497c18b75949`.
- FlightFlow: `aliifarhadi/Aerotech.FlightFlow`, `k8s-stg@962f41880df0f10a1d105559c233af63ef9db968`. Note exact case of repository name.

The canonical domain authority is `docs/AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md`; `CLAUDE.md` governs current Stage 6A authority and code conventions. Active scope: Stage 6A.2 commercial initial Order Create with all **genuinely source-priced, valid** selected ancillaries and priced seats in a single Order at `CommercialVersion=1`, one `OrderChangeType.Create`, `InitialSale` money, correct typed service items and persistence. Source `s_` tokens are multi-line snapshot ids, not OfferItem IDs. No Hold/Confirm/Issue/Cancellation redesign now.

Read these R4 authority artifacts in order before code review:
1. `00-OWNER-ARCHITECTURE-DECISION-AND-BENCHMARK.md` (research, evidence, 24-variant matrix, source gates, closure definition).
2. `01-ANCILLARY-COMPANION-EXECUTION-PROMPT-R4.md` (minimal source Resolve headers and completeness/count; not yet executed unless later reported).
3. `02-ORDERING-CREATE-WITH-ALL-PRICED-SELECTIONS-EXECUTION-PROMPT-R4.md` (precise Ordering Create changes, 14 test gates, Master C4/C5 amendments).
4. `03-DEFERRED-FLIGHTFLOW-SEAT-UNIQUENESS-FINDING.md` (verified physical seat uniqueness gap; 6B only).

Verified R4 design decisions:
- Both Create public optional ids: `AncillaryPricedSelectionId` and `SeatPricedSelectionId`, each singular multi-line `s_` snapshot, not raw seat id arrays; no fake NDC claim.
- General `AncillaryOffers/Resolve` needs `SourceKind`, `SourceReference`, `ExpiresAtUtc`, `SelectedLineCount`, `IsUnitTotalComplete` pricing. Seat `Ancillaries/SeatMaps/Resolve` needs source header and selected line count; it already returns exact selected single seat rate, POS and per-seat expiry.
- Offer-based `Journey` in current Ancillary means all flights from all portions of exact Offer for the selected traveller. Ordering `JourneyAirServicesOf` and one Master row must change accordingly. Order-based Add is not widened by inference.
- A09/A17/A19 are source-gated, cannot honestly be reported live; current `SellableCandidate` also gates supplier confirmation, currency, incomplete/non-filed/PerKilogram and other states. 24/24 domain status is different from 24/24 live sellability.
- FlightFlow `FlightCapacity.HoldSeat` does not atomically prevent duplicate physical seat number. The earlier assertion that FlightFlow Hold already enforces seat uniqueness was disproved by source. `OccupiedSeats` is read-only. 6A.2 does not claim Hold.

When an agent pushes work, re-read fresh commit heads, compare its patch against the R4 acceptance gates and Master; provide actionable correction prompt in downloadable file where needed. Do not repeat the past errors of claiming end-to-end pass based solely on fixture tests, delegating architecture/benchmark, guessing branch/head, or withholding an executable prompt because a separately gated variant is not yet source-sellable.
