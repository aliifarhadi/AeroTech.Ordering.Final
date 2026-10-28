# Stage 6A - Family Domain Modeling Review (READ-ONLY)

## Authority and pinned source
- Ordering repository: `aliifarhadi/AeroTech.Ordering.Final`, branch `stage6a-ancillary-commercial`, reviewed HEAD `372a1e4b08bcaee029eca8d5035d292fc12bd98f` (2026-10-28). Refresh HEAD and explicitly report any delta.
- Ancillary repository: `aliifarhadi/AeroTech.Ancillary`, `k8s-stg@190aa74eadb103d8556a9fab03c469cb937160e8` unless a newer verified HEAD exists.
- Canonical Ordering Master v2.0, especially sections 6.4, 6.6-6.8, 8.1, 17, 27.8; current `CLAUDE.md`, `.canonical-stage`, `canonical-stage6a-v1.0.yaml`.
- Read `reports/STAGE6A-DECISIONS-AND-BLOCKERS.md` and Stage6A test code. Do not override an explicit owner decision with a generated schema.

## Mandatory safety boundary
READ-ONLY AUDIT. No code edits, migrations, pushes, PRs, fixture re-baselining, schema deletion, or changes to Ancillary. This is a domain-design decision request, not implementation authorization.
Do not claim live acceptance or test results without executing them; distinguish existing agent-reported results from independently verified evidence.

## Source files to examine
- `Domain/OrderAggregate/Entities`: `OrderService`, `OrderAncillaryService`, `OrderBaggageService`, `OrderSeatService`, `OrderItem`.
- `Domain/OrderAggregate/Order.Ancillaries.cs`, `Order.Cancellation.cs`, `Order.Pricing.cs`, `AncillaryVariant.cs`, and every `ValueObjects/*Choice.cs`.
- `Persistence/OrderAggregate`: all Service configurations, AncillarySaleTermsMapping; latest `AddAncillaryServices` migration.
- `Query/OrderAggregate`: `OrderAncillaryServiceReadModel`, its configuration, `OrderServiceReadModel`, `OrderDetailDto`, `GetOrderByIdService`, `GetOrderAncillaryContextService`, and associated query migration.
- `Application/OrderAggregate/Projection/OrderReadModelSnapshotFactory.cs`; Create and Add Application flows, providers and wire.
- Existing Stage6A acceptance/conformance tests and latest source-contract blockers AC-01...AC-06.

## Model question to answer, with field-level evidence
Should the current `OrderAncillaryService` (containing nullable Meal/Pet/Assistance/MedicalEquipment/UMNR/AirportService/Priority/Connectivity/Upgrade/ExtraSeat choices) be replaced with a lean shared ancestor and family-specific concrete *entities within the Order aggregate*? Do not mistake a new entity for a new Aggregate Root.

Compare at least these options:
A. Keep current one-class + optional Owned VOs and wide command/read tables.
B. Family-specific entities (`OrderBaggageService`, `OrderMealService`, `OrderPetService`, `OrderAssistedTravelService`, `OrderAirportService`, `OrderPriorityService`, `OrderConnectivityService`, `OrderUpgradeService`, source-gated `OrderExtraSeatService`), all under a slim shared ancillary base; persist family-only properties separately.
C. A slim common ancillary entity with one-to-one typed family detail records/tables (composition rather than additional entity inheritance).

Assess domain invariant strength, invalid-null states, forward compatibility, line/source identity, coverage, service lifecycle, read-model quality, migrations, rehydration, EF Core query shape, and operational performance. Avoid a table/entity per Axx solely to match variants. Use per-variant typed VOs when *actual variant-specific behavior* warrants them. The source defines exactly A01-A24: baggage A01-A06, seat A07-A08, extra seat A09, upgrade A10, meal A11-A12, pet A13-A14, assisted travel A15-A19, airport A20-A22, priority A23, connectivity A24.

## Verify these potential failures explicitly
1. The command-side `OrderAncillaryServices` migration currently has 74 columns, 47 nullable; query-side `OrderAncillaryServices` has 88 columns, 61 nullable. Independently recompute.
2. The generic constructor accepts ten optional family/variant choices, then manually enforces a choice matrix. Determine whether any invalid state survives SQL deserialization or bypasses validation.
3. A09, A17, A19 detail VOs are currently `Ignore`d in persistence, while domain has constructors. Prove source gating prevents loss of commercially accepted choices, and identify tests for this.
4. `OrderReadModelSnapshotFactory` and standard `GetOrderById` query: does an external reader receive the complete sold variant and customer-selected details? Verify explicitly, not just the private `AncillaryContext` read model.
5. `CreateOrderFromOfferService` versus AddAncillaries: exactly which flow is implemented and which is not? Seat quote AC-03 state? Source AC-01/02/04 state?
6. `Reserve`, `Confirm`, `Issue`, `Cancel`: verify declared pre-provider safety guards and whole/partial cancellation invariants.
7. Source offer line to OrderItem and OrderService cardinality. IATA NDC 21.3 describes ordered-service delivery granularity per passenger and segment; compare with a single OrderService containing N covered AirServiceIds without silently rewriting accepted commercial topology. Determine whether quantity/coverage is a pricing unit or a delivery unit; recommend a future-proof split only when evidence supports it.
8. Reference EF Core's official performance guidance about TPT joins. Do not assume that eight new TPT tables are faster than one wide table. Propose benchmark shape with typical and stress-order fanout; no microbenchmark claims without measurements.
9. Existing historical data and migrations: preserve immutable IDs, pricing allocations, commercial versions, buyer source references, 6A accepted services, and the Command/Query projection contract.
10. Treat `k8s-stg@cd50a2a` as the older Stage 5 branch, not the new Stage 6A code: the pushed Stage6A branch HEAD is the authority for this review.

## Deliverables (under repository-root `reports/`, only after confirming owner explicitly allows report file writes)
- `STAGE6A-FAMILY-DOMAIN-AUDIT.md`: explicit findings with file/line references, impact, severity, evidence.
- `STAGE6A-A01-A24-MAPPING-PARITY.md`: current vs target per-variant field mappings and all required persisted customer choices; indicate SOURCE-GATED variants rather than fabricated columns.
- `STAGE6A-PERSISTENCE-OPTIONS.md`: compare A/B/C by correctness, complexity, data migration, query latency risk; one preferred option and a concrete migration concept, without creating migration code.
- `STAGE6A-READ-API-GAPS.md`: fields lost from standard Order GET, proposed minimal polymorphic read shape, backward compatibility.
- `STAGE6A-REFRACTOR-ACCEPTANCE-TESTS.md`: test cases for rehydration, project/reload, every live variant, lifecycle safety, migration retention, performance checks, and regression to Stage 1-5.
- `STAGE6A-DECISION-READY-SUMMARY.md`: exact changes for owner authorization, distinguishing REQUIRED now, SOURCE-GATED, and LATER.

## Exit verdict
`READY_FOR_OWNER_DECISION` only if all options have verified migration/read/lifecycle tradeoffs and no data-loss risk is overlooked. Otherwise `BLOCKED_EVIDENCE` with precise missing evidence. Do not implement until the owner approves the chosen target.
