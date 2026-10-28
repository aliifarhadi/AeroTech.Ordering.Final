# Stage 6A: G0 and G1 evidence for the family-split audit

| | |
|---|---|
| Date | 2026-10-28 |
| Audit answered | `01-DEEP-AUDIT-VERDICT.md` (F01–F07) and `03-NEXT-STAGE-DECISION.md` (G0, G1) |
| Code | `stage6a-ancillary-commercial@48f53d9` plus uncommitted G0/G1 changes |
| SQL Server | `AL-FARHADI-L\SQLEXPRESS` 15.0.2200.2, Express Edition (64-bit) |
| Ancillary checked | `k8s-stg@769229f`, read-only |

## Summary

| Finding | Result |
|---|---|
| F01 migration safety | Fixed and tested. The split now stops before dropping anything when a row does not land in exactly one family table matching its `VariantCode`. A real old→new upgrade test runs on data written by the previous model. |
| F02 wide read model | No change. The read side stays one ancillary table, as the audit allows. |
| F03 order detail | Fixed and tested. `GetOrderById` and the Backoffice order detail carry each accepted ancillary's typed details. |
| F04 source contracts | Still open at Ancillary `769229f` (AC-01…AC-06). Logged in handoff OR-005. |
| F05 source-gated options | No change. A09, A17 and A19 stay source-gated. |
| F06 TPT cost | Measured with real plans. The split costs about 2× logical reads on the services query. Plans are seek-based at volume. No speed improvement is claimed and no mapping was changed. |
| F07 fresh evidence | Clean build, 27/27 conformance and 612/612 acceptance on this run (below). |

## F01 — Migration safety

**Guard.** [`SplitAncillaryFamilies`](../src/AeroTech.Ordering.Persistence/Migrations/20261028125838_SplitAncillaryFamilies.cs) copies the rows, then checks every `OrderAncillaryServices` row before any column is dropped:
- its `VariantCode` must map to a family table (A01–A06, A10–A24); A07–A09 or an unknown code fails;
- the row must exist in exactly one family table;
- that table must be the one its `VariantCode` maps to.

On any mismatch it throws `50000` with the number of failing rows. EF runs the migration in one transaction, so the new tables and the copied rows roll back. No fallback values are invented.

**Old→new data.** Commit `a9a4084` does not compile, because it deletes `BaggageChoice.cs` while code still uses it. The seed was therefore written by `372a1e4`, the last commit with the single-table model that builds. Its `AddAncillaryServices` migration and Designer are byte-identical to those at `48f53d9`.

How the seed was produced:
1. The `372a1e4` persistence test stored the fixture order: 19 variants, 21 ancillary rows, 2 priced seats and 29 price lines with allocations. It was stored in a kept database at `AddAncillaryServices`.
2. The rows were exported to [`Ancillaries/Payloads/single-table-ancillary-order.sql`](../tests/AeroTech.Ordering.Application.AcceptanceTests/Ancillaries/Payloads/single-table-ancillary-order.sql): 169 rows in 18 tables, in foreign-key order.

**Tests** ([`AncillaryFamilySplitMigrationTests`](../tests/AeroTech.Ordering.Application.AcceptanceTests/Ancillaries/AncillaryFamilySplitMigrationTests.cs)):

| Test | Proves |
|---|---|
| `An_order_written_by_the_single_table_model_upgrades_to_its_family_tables_unchanged` | After the upgrade, every service (ids, common facts, family type and family facts) and every price line equals what the current model persists for the same order. |
| `A_single_table_row_without_its_family_data_stops_the_split_before_any_column_is_dropped` | One A11 row without `MealKind` stops the migration. Afterwards the last applied migration is still `AddAncillaryServices`, the old columns exist, no family table exists, and the row count is unchanged. |
| `Every_persisted_family_survives_reverting_and_reapplying_the_family_table_split` | `Down` copies the families back and `Up` restores them unchanged. |

The dev database `DotAirOrdering` already ran this migration before the guard existed. It had no ancillary rows, and its 15 orders, 58 services and 189 price lines are unchanged.

## F03 — Typed ancillary details in the order detail

`OrderServiceDto` and `BackofficeOrderServiceDto` gain a nullable `Ancillary` field, so existing consumers are unaffected. It holds:
- `VariantCode`, `Profile`, `Quantity`, `SelectionScope`, `CoverageScope` and `CoveredAirServiceIds`;
- the source definition: `ServiceDefinitionRef`, `ServiceTypeCode`, `ServiceSubCode`, `Rfic`, `Rfisc` and `SsrCode`;
- exactly one typed family record: Baggage, Meal, Pet, Assistance, AirportService, Priority, Connectivity or Upgrade.

Commercial status, the item id and the price lines were already on the order detail. No document ids are exposed, because none exist before 6B.

`The_backoffice_order_detail_carries_every_accepted_ancillary_with_its_customer_choices` checks, for every accepted ancillary, the variant, quantity, coverage, covered air services and the full family record against the domain entity.

Remaining limits:
- **OTA/OtaPanel** follow the Amadeus shape and are unchanged. Exposing ancillaries there needs its own Amadeus mapping.
- **Priced seats** cannot reach the read side yet, because no service flow accepts them before AC-03.

## F04 — Source contracts at Ancillary `769229f`

| Contract | Status |
|---|---|
| AC-01 | `ServiceResolvedSelectionsDto` has no `sourceKind`, `sourceReference`, `expiresAtUtc` or `selectedLineCount`. |
| AC-02 | `ServiceResolvedPricingDto` has no `isUnitTotalComplete`. |
| AC-04 | `OrderingAncillaryContextMapper.cs:17` rejects a non-empty `AncillaryServices`. |
| AC-03, AC-05, AC-06 | Still open. The only change since `190aa74` is `ApiGetOrderSeatMapQuery.cs`. |

The Ordering adapter keeps failing closed. Create with selections and live Add stay `BLOCKED_SOURCE`.

## F06 — TPT load profile

**Method.**
1. A temporary profile test, not committed, ran the same code on the current model and on `372a1e4`.
2. Each run used a fresh migrated database with `AUTO_CLOSE OFF`. Without it, SQL Express flushes the plan cache whenever a test database closes.
3. Each run measured `OrderRepository.GetAsync` with a new context per load: 5 warm-up loads, then 40 timed loads per order.
4. Plans and reads come from `sys.dm_exec_query_stats` and `sys.dm_exec_query_plan` for the statements executed 90 times, which is the load path.

Orders:
- **Typical:** 2 travellers, 3 flights, 18 ancillaries and 2 priced seats (26 services).
- **Group:** 9 travellers, 6 flights and 162 ancillaries (216 services).

**At volume:** 300 typical and 30 group background orders (14,306 services), with statistics updated.

| | Current (family split) | `372a1e4` (single table) |
|---|---|---|
| Services query: average logical reads | 2,993 | 1,420 |
| Services query: average elapsed time | 22.2 ms | 12.7 ms |
| All load statements: logical reads per load | 4,150 | 2,577 |
| Typical order load: median / p95 | 35.0 / 47.2 ms | 26.9 / 34.0 ms |
| Group order load: median / p95 | 83.5 / 101.2 ms | 63.6 / 83.5 ms |

**Plans at volume.** The current services query uses:
- an index seek on `OrderServices` by `OrderId`;
- nested-loop clustered index seeks into `OrderAirTransportServices`, `OrderAncillaryServices`, `OrderSeatServices` and every family table except `OrderUpgradeServices`;
- one hash join over a scan of `OrderUpgradeServices` (301 rows).

The single-table model uses the same seeks without the seven extra joins. The other 16 load statements have identical average reads in both models.

**Small data.** With only the two measured orders in the database, both models scan their tiny tables. Those plans do not represent production.

**Conclusion.** The split costs about 2.1× logical reads and about 10 ms on the services query. That makes a whole order load about 30% slower at this volume (typical +30%, group +31% median). The cost grows with the size of the order, not the size of the tables, because every family table is reached by a primary-key seek.

This is a measured cost, not a blocking problem for single-order command rehydration. No index or mapping change was made. Revisit it only against an agreed latency target, re-measured on production-sized data.

## F07 — Fresh verification

| Check | Result |
|---|---|
| Clean solution build (`dotnet build AeroTech.Ordering.sln --no-incremental`) | 0 errors, 53 warnings (same as baseline), 30 s |
| Conformance (`AeroTech.Ordering.Domain.ConformanceTests`) | 27/27 passed |
| Acceptance (`AeroTech.Ordering.Application.AcceptanceTests`, real SQL Server) | 612/612 passed in 30 s |
| Total, build to last test | 71 s (19:04:50–19:06:01) |

The acceptance run uses one migrated database for all SQL test classes and clears its rows before each test. The migration tests each create and drop their own database.
