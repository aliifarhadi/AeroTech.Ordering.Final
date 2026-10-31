# FlightFlow OR-006 — verified safety finding, NOT an execution prompt for Stage 6A.2

Repo: `aliifarhadi/Aerotech.FlightFlow` @ `962f41880df0f10a1d105559c233af63ef9db968`, `k8s-stg`.

Exact evidence:
- `AeroTech.FlightFlow/FlightFlow/src/AeroTech.FlightFlow.Domain/FlightAggregate/Entities/Capacities/FlightCapacity.cs`, method `HoldSeat(FlightCapacityHoldArguments, IIdGenerator)`, lines ~120-183: checks capacity and creates `FlightSeatHold` with `SeatNumber`, but does not verify another unexpired Held or Confirmed hold with the same flight/seat.
- `.../FlightAggregate/Flight.cs`, method `HoldSeats` around lines ~393-470 calls the capacity Hold path.
- `.../RestApi/V1/FlightAggregate/Controllers/ServiceController.cs`, `POST Seat-Holds` and `POST OccupiedSeats`: the latter is a read-only query, not an atomic lock.
- `.../FlightAggregate/Entities/Capacities/FlightSeatHold.cs`: stores seat number, status and expiry.
- Ancillary `SeatSelectionVerifier.SeatsAsync` constructs `SeatMapSeat(..., false)` in seat Hold verification. Source indicates no atomic collision re-check there.

Verified conclusion: FlightFlow Hold currently records a physical seat number but does **not** establish a demonstrated cross-request unique atomic physical-seat lock. Do not assert seat reservation exclusivity at Create or at downstream Hold.

Scope boundary: **Do not modify this code in 6A.2.** Ownership and physical inventory semantics must be closed in Stage 6B by the owner with exact concurrency, allotment, DB uniqueness or equivalent lock design and tested race behavior. Also verify two concurrent holds across the *same flight but different capacity records*, expiry/release/confirm, and no false collision across different flights. A non-atomic `OccupiedSeats` GET/POST precheck is insufficient.

Benchmark: IATA differentiates accepting OfferItems/Order from the later service delivery/inventory lifecycle; Breeze Create can hold a booking but AeroTech's current Create is a **commercial-only** stage. References: official IATA 21.3 implementation guide p.178/p.205; Breeze `OrderCreate` https://pro-docs.flybreeze.com/api/ndc-xml/endpoints/ordercreate.

Owner action after 6A.2: authorize narrowly-scoped FlightFlow OR-006 domain+atomicity remediation before lifting Ordering's `EnsureFulfillmentIsSupported` guard for priced seats. Generate a fresh implementation prompt only after comparing the actual persistence isolation/flight capacity aggregate transaction boundary; this deferred finding does not authorize code.
