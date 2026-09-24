# AeroTech Ordering — Stage 2 Reservation Implementation Spec v1.1 FINAL REVISED

**Status:** FINAL — Coding Agent authority for Stage 2  
**Target repository:** `aliifarhadi/AeroTech.Ordering.Final`  
**Target branch:** `k8s-stg`  
**Stage 1:** Complete

This document supersedes `Stage 2 Reservation Implementation Spec v1.0 FINAL`.

`ORDERING-IMPLEMENTATION-PACK-v4.6-FINAL.md` remains authority for the Stage-1 commercial model and the already-frozen later aggregate boundaries.

The goal is not to invent a reservation framework. The goal is to implement a standard OTA/PSS orchestration problem cleanly:

```text
one Order
many heterogeneous Services
many Suppliers/Providers
different supplier reservation capabilities
different operation/unit references
automatic initial reservation of all eligible services
targeted later reservation/retry of a subset
```

---

# 1. Stage-2 business objective

Stage 2 must provide a practical reservation capability with these behaviors:

```text
each fulfillable OrderService has an assigned provider
each service knows whether that provider/service needs reservation
initial ReserveOrder selects every currently eligible reservable service
selected services are grouped by provider
one provider may receive one batch operation containing multiple operational units
each provider operation returns one operation-level reference
each operational unit may return its own provider reference
the normalized result is persisted independently per provider/unit
a later caller can reserve/retry only a selected subset
unknown external effects are retried using the original idempotency identity
definitive rejected/expired/released services may be reserved later with a new operation
direct-issue services are skipped by reservation and handled by Issue later
```

Stage 2 must not implement:

```text
payment-time confirmation
ticket/EMD issue
post-confirm cancel
split
hotel/ground/baggage production adapters
generic provider policy engine
cross-provider compensation saga
SSR/OSI
DCS behavior
```

---

# 2. Allowed Stage-1 extension — explicit

Stage 1 is complete, but Stage 2 **must intentionally extend one existing Stage-1 entity**:

```text
OrderService.FulfillmentProviderKey
```

This is allowed and required.

The Coding Agent is explicitly authorized to:

```text
add FulfillmentProviderKey to OrderService
populate it in CreateOrderFromOffer for Stage-1 Air/Seat services
add the planned Stage-2 migration/schema change for that field
```

No other Stage-1 domain redesign is authorized.

For current products:

```text
OrderAirTransportService -> "FlightFlow"
OrderSeatService         -> same provider as its associated Air Service
```

Provider assignment is:

```text
required before fulfillment
immutable for the lifetime of the Service occurrence
```

A later provider change must create a successor commercial Service occurrence in its servicing slice rather than mutating historical provider ownership.

---

# 3. Provider assignment belongs to the Service occurrence

Provider selection is not an Order-level property.

Different Services in one Order may have different suppliers:

```text
Air Service       -> FlightFlow
Hotel Service     -> HotelProviderA
Baggage Service   -> SupplierB
Direct issue fee  -> ProviderX
```

Therefore every fulfillable Service occurrence must resolve:

```text
FulfillmentProviderKey
```

Automatic Reserve does this:

```text
select all eligible active Services
-> resolve reservation capability
-> discard ReservationMode=None
-> remove already positively covered Services
-> preserve unresolved Unknown operations
-> group remaining Services by provider
-> build provider operational units
-> execute one logical reservation operation per provider group
```

This is not a distributed transaction across providers.

---

# 4. Reservation capability — small strategy, not policy engine

Resolve reservation capability from:

```text
FulfillmentProviderKey + Service/Product semantics
```

Use a closed descriptor:

```text
ReservationMode
BatchResultMode
PreConfirmationReleaseScope
PostConfirmationCancelScope
SupportsExtend
SupportsSplit
ProvidesUnitReference
```

## `ReservationMode`

```text
HoldThenConfirm
ImmediateConfirm
None
```

### HoldThenConfirm

```text
Reserve -> Held
later business/payment gate -> Confirmed
```

FlightFlow uses this.

### ImmediateConfirm

Provider booking becomes confirmed during Reserve.

Potential hotel supplier pattern.

No concrete adapter now.

### None

No reservation step exists.

Stage 2:

```text
skip the Service
do not call its provider
do not issue it
```

Its fulfillment belongs to Issue/Ancillary later.

---

# 5. FlightFlow capability — frozen

```text
ProviderKey                   = "FlightFlow"
ReservationMode               = HoldThenConfirm
BatchResultMode               = AtomicAllOrNothing
PreConfirmationReleaseScope   = Operation
PostConfirmationCancelScope   = Unit
SupportsExtend                = true
SupportsSplit                 = true
ProvidesUnitReference         = true
```

Team-confirmed decisions:

```text
HoldId == HoldBatchId

ConfirmHoldAsync:
  return without exception = success
  actual confirmation flow belongs to Stage 3

CreateHold:
  all-or-error
  no legitimate partial reservation

SplitHeldSeatsResult:
  insufficient for deterministic servicing
  FlightFlow team must change it before Split stage
```

Therefore:

```text
FlightFlow adapter MUST NEVER emit Partial from CreateHold.
```

---

# 6. Four reference/identity levels

Never mix these:

```text
OrderReference
    commercial Order identity

Order.RecordLocator
    AeroTech host PNR

ProviderOperationRef
    one reservation operation at one supplier
    FlightFlow: HoldId / HoldBatchId

ProviderUnitRef
    one operational unit reference at supplier
    FlightFlow: SeatHoldReference
```

Technical identities:

```text
IdempotencyKey
CorrelationReference
```

Rules:

```text
OrderReference != RecordLocator
RecordLocator != ProviderOperationRef
ProviderOperationRef != ProviderUnitRef
IdempotencyKey != CorrelationReference
```

---

# 7. Reservation aggregate grain

Freeze:

> One `FulfillmentReservation` = one logical reservation operation sent to one provider.

Therefore:

```text
one Order may have many FulfillmentReservations
same provider may have multiple historical operations
one retry of Unknown does not create a new operation
one explicit new business attempt after definitive failure does
```

There is no single global Order reservation aggregate.

---

# 8. ReservationUnit grain

Use:

```text
ReservationUnit
```

not `ReservationService`.

A provider operational unit may cover one or more OrderServices.

Examples:

```text
FlightFlow:
  one passenger x flight unit
  covers Air Service
  + optional Seat Service
  + optional lap-infant Air Service linked to parent

Hotel:
  one room unit
  may cover multiple traveller Hotel Services

Transfer:
  one provider booking/vehicle unit
  may cover multiple passenger Services
```

This solves the mismatch between commercial service granularity and supplier operational granularity without introducing a generic coverage graph.

---

# 9. Automatic and targeted reservation

## 9.1 Automatic initial Reserve

Conceptually:

```text
ReserveOrder(OrderId)
```

It selects every currently eligible reservation-required Service.

Eligibility:

```text
Service is Active
FulfillmentProviderKey exists
ReservationMode != None
Service has no current positive reservation coverage
Service is not inside an unresolved Unknown operation that must be resumed
```

Then group by provider and execute independently.

## 9.2 Targeted Reserve

Conceptually:

```text
ReserveServices(OrderId, OrderServiceIds[])
```

Required use cases:

```text
later-added Service
initially omitted Service
retry after definitive rejection
retry after expiry
retry after release
manual provider-specific recovery
```

It uses exactly the same planner/ACL path as automatic Reserve.

No second reservation framework is created.

---

# 10. Retry semantics — critical

## Same logical operation

For:

```text
Unknown
retriable technical ambiguity
```

reuse:

```text
same FulfillmentReservation
same IdempotencyKey
same CorrelationReference
```

Create a new `FulfillmentTaskAttempt`.

Do not create a new provider booking identity.

## New business attempt

After a definitive result:

```text
Rejected
Released
Expired
Cancelled
```

a later explicit re-reservation creates:

```text
new FulfillmentReservation
new IdempotencyKey
new CorrelationReference
```

Historical reservation remains immutable evidence.

## Positive coverage

Services covered by active units in:

```text
Held
Confirmed
```

must not be reserved again.

---

# 11. AirFare / flight reservation validation — mandatory pre-Hold gate

A real existing Pricing/AirPrice contract exists in the donor code:

```text
v1/BoundReservationValidation
```

Request model includes:

```text
SalesContext
Passengers
PricingUnits
  PricingUnitId
  JourneyType
  BoundIds
  PricingOriginAirportId
  PricingDestinationAirportId
  AirFares
    AirFareId
    Flights
      FlightId
      FlightNumber
      Origin/Destination
      AircraftId
      DepartureDateTime
      FlightStatus
      FlightStopBookDateTime
      RbdId
      RequiredSeats
```

Response:

```text
TimeLimit
```

Important source finding:

> The contract/provider endpoint is real and present in donor `Ordering/k8s-stg`.  
> The inspected `ReserveOrderService` snapshot does not prove that this validation was actually invoked in the runtime Reserve path.

So Stage 2 does **not** claim “V1 runtime definitely did this”. It deliberately activates a real existing validation contract because it is the correct pre-reservation PSS gate.

## Stage-2 rule

Before calling FlightFlow `CreateHold` for any Air reservation operation:

```text
validate that the accepted AirFare / RBD / flight / passenger combination
is still reservation-valid
```

Validation happens before external hold creation.

If validation fails:

```text
do not call FlightFlow
do not create a fake Held/Rejected provider result
return a deterministic reservation-validation failure
Order remains unreserved for those Services
```

If validation succeeds:

```text
use returned TimeLimit as an input to the FlightFlow requested expiry policy
```

Do not silently replace `Order.LastTicketingDate`.

## Required expiry calculation for FlightFlow

Use the earliest applicable boundary:

```text
RequestedExpiry =
  min(
    now + configured AirHoldDuration,
    Order.LastTicketingDate when present,
    AirPrice ReservationValidation.TimeLimit when present
  )
```

Provider-returned:

```text
ExpiresAt
```

is still authoritative after CreateHold succeeds.

---

# 12. Validation grain

Validation must reflect the actual reservation scope being attempted.

For automatic FlightFlow Reserve:

```text
validate all selected FlightFlow air units in that operation
```

For targeted retry/reservation:

```text
validate the selected provider-unit closure only
```

Do not validate unrelated Services that are not being reserved.

RequiredSeats must match the number of actual seat-consuming passengers for the relevant flight/RBD combination.

Lap infants do not consume an independent seat.

---

# 13. Passenger / infant mapping to FlightFlow

This remains an explicit provider-contract question and must not be guessed.

## Domain fact

A lap infant:

```text
is a Traveller in the Order
has an Air Service
does not consume an independent seat
is linked to a parent/adult traveller
```

## FlightFlow question

Before implementation is considered complete, confirm one of these:

### Option A

```text
INF must appear in Passengers[]
but must not appear in Flights[].Seats[]
```

### Option B

```text
INF must not be sent to FlightFlow at all
and FlightFlow does not own infant-count/capacity validation
```

Until the FlightFlow team answers:

```text
INF request mapping = BLOCKED_SOURCE
```

The Agent must not invent the answer.

The reservation-domain shape is unaffected:

```text
infant Air Service is covered by the parent adult ReservationUnit
```

once the provider mapping is known.

---

# 14. PassengerType mapping

There are two different `PassengerTypeCode` enum definitions in the codebase/contracts.

Numeric cast is forbidden.

Use explicit semantic mapping:

```text
Adult -> Adult
Child -> Child
Infant -> Infant
```

and any additional supported type only by named semantic mapping.

Do not rely on enum integer values matching.

Unknown passenger type:

```text
reject / BLOCKED according to source
```

never default.

---

# 15. Gender mapping

`Gender.UnSpecified` exists.

When FlightFlow requires Gender and the current traveller profile has no value:

```text
send Gender.UnSpecified explicitly
```

Do not rely on numeric default zero unless it is actually the named enum member.

Do not mutate the stored profile merely to satisfy the provider request.

---

# 16. Revenue wire field

Current FlightFlow wire requires:

```text
Revenue
```

The reservation domain itself does not need revenue.

Current donor behavior sent:

```text
0m
```

Stage-2 decision is conditional:

> Confirm once with FlightFlow that `Revenue = 0m` is semantically neutral and accepted by validation.

If FlightFlow confirms:

```text
map Revenue = 0m only inside the FlightFlow adapter
```

Do not copy Order customer price into reservation domain.

If FlightFlow says Revenue has business meaning:

```text
BLOCKED_SOURCE
```

and use the exact required semantic source.

---

# 17. Common reservation ACL

No JSON bag / dictionary metadata.

## Request

```text
ReservationIntent
  ProviderKey
  IdempotencyKey
  CorrelationReference
  RequestedExpiry?
  Units[]

ReservationUnitIntent
  UnitCorrelationKey
  OrderServiceIds[]
  TypedDetails
```

## Response

```text
ReservationOutcome
  OperationOutcome
  ProviderOperationRef?
  EchoedIdempotencyKey?
  EchoedCorrelationReference?
  ExpiresAt?
  Units[]

ReservationUnitOutcome
  UnitCorrelationKey
  OrderServiceIds[]
  ReservationState
  ProviderUnitRef?
  RawStatusCode?
  TypedDetails
```

Operation outcome:

```text
Succeeded
Partial
Rejected
Unknown
```

Normalized unit state:

```text
Pending
Held
Confirmed
Rejected
Unknown
Released
Expired
Cancelled
```

Do not add waitlist/etc. until a real supplier contract needs it.

---

# 18. FlightFlow typed ACL

## Unit intent

```text
FlightReservationUnitIntent
  FlightId
  FlightCapacityId
  PaxReference
  RequestedSeat?
```

## Unit outcome

```text
FlightReservationUnitOutcome
  AssignedSeat?
```

Provider result matching key:

```text
(FlightId, PaxReference)
```

Never map by array order.

Request still sends:

```text
FlightCapacityId -> FlightCapId
```

Response matching uses:

```text
FlightId
```

These identifiers must not be assumed equal.

---

# 19. FlightFlow partial / failure semantics

Team-confirmed:

```text
FlightFlow CreateHold is atomic all-or-error.
```

Therefore:

## Successful response

All expected units must be represented and valid.

Otherwise:

```text
Unknown / protocol inconsistency
```

not Partial.

## Permanent provider failure

```text
Reservation root -> Rejected
all target units  -> Rejected
Task              -> Failed
```

## Retriable technical failure

```text
Reservation -> Unknown
units       -> Unknown
Task        -> Unknown/retriable
```

Retry same logical operation/idempotency.

## Indeterminate timeout/outcome

```text
Reservation -> Unknown
units       -> Unknown
Task        -> Unknown
```

Never blindly create another hold.

---

# 20. Reservation states

## Reservation operation

```text
Pending
Held
Confirmed
Rejected
Unknown
Mixed
Released
Expired
Cancelled
```

`Mixed` is retained for future PerUnit providers.

FlightFlow legitimate CreateHold does not produce Mixed/Partial.

## Reservation unit

```text
Pending
Held
Confirmed
Rejected
Unknown
Released
Expired
Cancelled
```

Raw provider status is preserved separately where useful.

---

# 21. FulfillmentReservation shape

```text
OrderId
FulfillmentProviderKey
Mode
IdempotencyKey
CorrelationReference
ProviderOperationRef?
Status
ExpiresAt?
CreatedAt
LastObservedAt
Units[]
```

Invariants:

```text
at least one Unit
all Units belong to same Order
all covered Services belong to same provider
IdempotencyKey immutable
CorrelationReference immutable
ProviderOperationRef cannot silently change once known
```

---

# 22. ReservationUnit shape

```text
FulfillmentReservationId
UnitCorrelationKey
Status
ProviderUnitRef?
RawStatusCode?
ObservedSeat?
OrderServiceIds[]
```

Invariants:

```text
at least one Service
no duplicate Service ID
all Services same Order
all Services same provider
unit correlation immutable in same logical operation
provider unit ref cannot silently change once known
```

No room/vehicle/provider-generic fields now.

---

# 23. FlightFlow operational unit

Normal seated passenger:

```text
one ReservationUnit per passenger x flight
```

May cover:

```text
Air Service
optional Seat Service
```

Lap infant domain coverage:

```text
infant Air Service belongs to parent adult unit
```

Exact FlightFlow passenger-array mapping remains blocked until the provider team answers Section 13.

---

# 24. FlightFlow request grouping

For one FlightFlow provider operation:

```text
all currently selected/uncovered FlightFlow operational units
```

are sent in one CreateHold request.

This is correct because current FlightFlow:

```text
supports multiple flights/passengers
is atomic all-or-error
```

A future provider may require different partitioning. Add provider-specific operation partitioning only when that real contract arrives.

Do not build a generic partition engine now.

---

# 25. PNR

Stage 2 adds:

```text
Order.RecordLocator
```

Semantics:

```text
Ordering-generated host PNR
opaque
unique
immutable once assigned
never reused
not cleared on later release/expiry
```

Generate exactly once when the first Air reservation reaches positive state:

```text
Held
Confirmed
```

For FlightFlow:

```text
successful CreateHold -> Held -> PNR can be generated
```

Do not wait for `ConfirmHold`.

Hotel/direct-issue-only Order does not create the AeroTech Air PNR under current Owner decision.

---

# 26. OrderStatus summary

Evaluate active reservation-required Services, not reservation roots alone.

## Every required Service positively covered

```text
Order.Status = Confirmed
```

Positive:

```text
Held
Confirmed
```

## All definitive reject, none positive/unknown

```text
Order.Status = ReserveFailed
```

## Any unresolved or mixed result

```text
Order.Status = ReservationUnconfirmed
```

Examples:

```text
Provider A Held + Provider B Rejected
=> ReservationUnconfirmed

Provider A Held + Provider B Unknown
=> ReservationUnconfirmed
```

Do not rollback Provider A automatically.

## No reservable Services

If every Service is:

```text
ReservationMode=None
```

then Reserve makes no supplier call.

Those services continue toward later fulfillment/issue.

Stage 2 does not issue them.

---

# 27. FulfillmentTask responsibility

`FulfillmentReservation` owns business reservation truth.

`FulfillmentTask` owns external execution/recovery.

Stage-2 Task types:

```text
ReserveInventory
ReleaseReserved
```

Task owns:

```text
attempts
technical outcome
failure kind/reason
unknown/retry lifecycle
provider interaction history
```

Do not duplicate provider reservation state there.

---

# 28. FulfillmentTask / Attempt / Interaction minimum semantics

## FulfillmentTask

```text
OrderId
FulfillmentReservationId
TaskType
FulfillmentProviderKey
IdempotencyKey
CorrelationReference
Status
AttemptCount
CreatedAt
CompletedAt?
LastFailureKind?
LastFailureReason?
LastError?
```

Status:

```text
Pending
InProgress
Succeeded
Failed
Unknown
Cancelled
```

## FulfillmentTaskAttempt

```text
AttemptNumber
StartedAt
CompletedAt?
Outcome
FailureKind?
FailureReason?
Error?
```

Attempt outcome:

```text
Succeeded
Failed
Unknown
```

## ProviderInteraction

Child of Task, not aggregate.

Stage-2 interaction types:

```text
CreateHold
ReleaseHold
```

Request/response payload logging is operational detail, not domain truth.

---

# 29. Pre-confirm release

Implement explicit release for FlightFlow Held operation.

Call:

```text
ReleaseHeld(ProviderOperationRef)
```

On definitive success:

```text
reservation -> Released
all units   -> Released
```

Then recompute Order summary.

This is not the full Cancel Order flow.

`CancelConfirmed` remains for the later Cancel slice.

---

# 30. Expiry

FlightFlow requested expiry:

```text
min(
  now + configured AirHoldDuration,
  Order.LastTicketingDate if present,
  AirPrice validation TimeLimit if present
)
```

Successful provider response:

```text
FlightHeldSeatsResult.ExpiresAt
```

becomes authoritative reservation expiry.

Current:

```text
FlightReservationExpiredEvent.ReferenceId
```

has insufficient identity semantics.

Do not wire an event consumer by guessing.

The aggregate supports expiry transition; event integration waits for the provider contract clarification.

---

# 31. Split

Current `SplitHeldSeatsResult` cannot deterministically map old provider unit references to new ones.

Therefore:

```text
Stage 7 Split = BLOCKED_SOURCE
```

until FlightFlow returns deterministic mapping such as:

```text
old SeatHoldReference -> new SeatHoldReference
```

Never infer by collection order.

---

# 32. Multi-provider behavior example

Order:

```text
Air Service       -> FlightFlow / HoldThenConfirm
Hotel Service     -> HotelProvider / ImmediateConfirm
DirectIssue Fee   -> ProviderX / None
```

Initial Reserve:

```text
group Air and Hotel separately
skip DirectIssue Fee
validate FlightFlow AirFare before hold
call each reservation provider independently
persist each actual result
```

If:

```text
FlightFlow = Held
Hotel      = Rejected
```

then:

```text
FlightFlow truth remains Held
Hotel truth remains Rejected
Order = ReservationUnconfirmed
PNR exists because Air is Held
```

No automatic compensation.

---

# 33. Stage-2 exact implementation scope

## Implement now

```text
Order.RecordLocator
OrderService.FulfillmentProviderKey
CreateOrderFromOffer provider assignment for Air/Seat
planned Stage-2 migration for the new fields

provider capability resolution
automatic ReserveOrder
targeted ReserveServices
provider grouping

AirFare BoundReservationValidation before FlightFlow hold
validation TimeLimit in requested-expiry calculation

FulfillmentReservation
ReservationUnit

common typed Reservation ACL

FulfillmentTask
FulfillmentTaskTarget
FulfillmentTaskAttempt
ProviderInteraction

FlightFlow CreateHold
FlightFlow ReleaseHeld

FlightFlow all-or-error normalization
provider/unit reference persistence
same-operation Unknown retry
new business attempt after definitive failure
PNR generation
Order reservation summary
```

## Do not implement now

```text
ConfirmHold execution
payment coupling
ExtendHeld flow
CancelConfirmed flow
Split flow
Ticket / EMD
Hotel production adapter
Ground production adapter
Baggage production adapter
generic async callback supplier workflow
cross-provider compensation
group reservation
SSR / OSI
DCS delivery
refund / exchange
```

---

# 34. Blocking external answers before Stage-2 completion

Two provider questions remain genuinely blocking for the relevant behavior.

## FlightFlow INF mapping

Need explicit answer:

```text
Does INF appear in Passengers[] without a Seats[] entry,
or is INF omitted from FlightFlow entirely?
```

Do not guess.

## FlightFlow Revenue

Need explicit answer:

```text
Is Revenue=0m guaranteed semantically neutral/valid for CreateHold?
```

If yes, keep zero in adapter only.

If no, FlightFlow must identify the correct business source.

These questions do not change aggregate boundaries, but the corresponding FlightFlow mapping must not be considered complete until answered.

---

# 35. Acceptance scenarios — required

Stage 2 is complete only when all applicable behaviors pass.

## AirFare validation

```text
valid fare/flight/RBD/passenger combination -> Hold attempted
invalid validation -> no CreateHold call
validation TimeLimit constrains requested expiry
validation only covers targeted reservation scope
```

## Basic air reservation

```text
1 ADT / 1 flight
multiple passengers
round trip
connection
multiple bounds
```

## Provider assignment

```text
Air/Seat services have FlightFlow provider from CreateOrder
provider assignment immutable
planned Stage-2 migration applied
```

## Provider grouping

Using stubs for future providers:

```text
Provider A reservable
Provider B reservable
Provider C ReservationMode=None

automatic Reserve:
  one provider operation A
  one provider operation B
  no provider call C
```

## Targeted reserve

```text
reserve one uncovered service/subset
do not touch already Held services
definitive rejected service may create new operation later
Unknown service resumes same operation/idempotency
```

## Seat

```text
Air + selected Seat share the same provider operational unit
Air without Seat reserves correctly
no independent Seat hold is created
```

## Infant

After FlightFlow answer:

```text
lap infant does not consume independent seat
infant Air Service covered by parent unit
request mapping follows confirmed FlightFlow contract
```

## FlightFlow atomic semantics

```text
success -> all expected units Held
permanent failure -> all target units Rejected
retriable/indeterminate -> Unknown
incomplete success payload -> Unknown protocol inconsistency
adapter never emits Partial
```

## Idempotency

```text
duplicate Reserve does not duplicate positive booking
Unknown retry uses same idempotency
definitive rejected/released/expired later retry gets new idempotency
```

## PNR

```text
first positive Air hold generates one PNR
subsequent reservation success keeps same PNR
retry never creates second PNR
release/expiry does not clear PNR
non-air-only reservation does not generate Air PNR
```

## References

```text
operation HoldId stored once
SeatHoldReference stored per FlightFlow unit
Order reference / PNR / provider operation / provider unit remain distinct
```

## Release

```text
Held FlightFlow operation released by ProviderOperationRef
all units -> Released
Order summary recomputed
```

## Multi-provider truth

```text
one provider success + another reject
successful reservation is preserved
Order becomes ReservationUnconfirmed
no automatic compensation
```

---

# 36. Agent implementation sequence

## Step 1 — minimal Stage-1 extension

```text
add FulfillmentProviderKey to OrderService
assign FlightFlow in CreateOrder for Air/Seat
add RecordLocator to Order
create planned Stage-2 migration
```

No other Stage-1 redesign.

## Step 2 — reservation capability + provider planner

Implement:

```text
ReservationMode
BatchResultMode
release/cancel scopes
capability resolver
automatic / targeted service selection
provider grouping
```

## Step 3 — Pricing reservation validation port

Implement/restore the real:

```text
BoundReservationValidation
```

port/provider contract against current target structure.

Place it before FlightFlow hold creation.

Do not copy old aggregate/domain shapes.

## Step 4 — reservation domain

Implement:

```text
FulfillmentReservation
ReservationUnit
status/invariants/reference ownership
```

## Step 5 — execution/recovery

Implement only Reserve/Release portions of:

```text
FulfillmentTask
Target
Attempt
ProviderInteraction
```

## Step 6 — ACL and FlightFlow adapter

Implement:

```text
common ReservationIntent/Outcome
typed FlightFlow unit details
deterministic response matching
CreateHold
ReleaseHeld
failure normalization
```

## Step 7 — Order summary / PNR

Implement:

```text
service-based reservation coverage summary
PNR idempotent assignment
```

## Step 8 — acceptance suite

Do not call Stage 2 complete until Section 35 passes.

---

# 37. Coding Agent prompt

```text
TARGET
aliifarhadi/AeroTech.Ordering.Final / k8s-stg

STAGE
Stage 1 is complete.
Implement Stage 2 Reservation only.

SOLE STAGE-2 DOMAIN/BEHAVIOR AUTHORITY
AeroTech-Ordering-Stage2-Reservation-Implementation-Spec-v1.1-FINAL-REVISED.md

BASELINE AUTHORITY
ORDERING-IMPLEMENTATION-PACK-v4.6-FINAL.md for frozen Stage-1 commercial semantics
and later aggregate boundaries.

IMPORTANT ALLOWED STAGE-1 CHANGE
Stage 2 MUST extend Stage-1 OrderService with required immutable
FulfillmentProviderKey and populate it in CreateOrderFromOffer for Air/Seat.
Stage 2 also adds Order.RecordLocator.
These are authorized Stage-2 changes and require the planned Stage-2 migration.
Do not make any other Stage-1 domain redesign.

DO NOT
- audit/redesign the framework;
- copy donor V2 reservation code wholesale;
- implement Payment/ConfirmHold/Ticket/EMD/Split/Refund;
- implement hotel/ground/baggage production adapters;
- invent provider statuses or JSON metadata;
- auto-compensate another provider after one provider fails;
- map provider results by array order;
- numeric-cast PassengerTypeCode enums;
- guess FlightFlow INF mapping;
- guess Revenue semantics.

IMPLEMENT
1. immutable FulfillmentProviderKey per OrderService;
2. FlightFlow assignment for existing Air/Seat Services;
3. automatic ReserveOrder for all eligible reservation-required Services;
4. targeted ReserveServices for a subset;
5. group reservation execution by provider;
6. simple provider capability descriptor;
7. one FulfillmentReservation per logical provider operation;
8. ReservationUnit covering 1..N OrderServices;
9. common typed Reservation ACL;
10. AirPrice BoundReservationValidation before FlightFlow CreateHold;
11. validation TimeLimit in FlightFlow requested-expiry calculation;
12. FulfillmentTask/Target/Attempt/ProviderInteraction for Reserve/Release recovery;
13. FlightFlow CreateHold and ReleaseHeld;
14. FlightFlow atomic all-or-error normalization;
15. same-operation retry for Unknown with same IdempotencyKey;
16. new reservation operation only after definitive Rejected/Released/Expired/Cancelled;
17. PNR generation on first positive Air reservation;
18. service-based Order reservation summary.

FROZEN FLIGHTFLOW DECISIONS
- HoldId == HoldBatchId;
- ConfirmHold success = no exception, but Confirm execution belongs to Stage 3;
- CreateHold never legitimate Partial;
- Split mapping is BLOCKED until FlightFlow changes its result contract;
- provider response mapping uses (FlightId, PaxReference), never array order;
- request sends FlightCapacityId as FlightCapId;
- PaxReference = OrderTraveller.Id.ToString();
- lap infant consumes no independent reservation seat unit.

OPEN PROVIDER QUESTIONS — DO NOT GUESS
A. Must INF appear in FlightFlow Passengers[] without a Seats[] entry,
   or must INF be omitted entirely?
B. Is Revenue=0m guaranteed semantically neutral/accepted by FlightFlow?

Until A/B are answered, isolate those mappings and report BLOCKED for the exact
provider-specific acceptance scenario; do not change aggregate design.

DONE ONLY WHEN
all non-blocked acceptance scenarios in Section 35 pass and the provider-specific
INF/Revenue scenarios pass after their authoritative answers are supplied.
```

---

# 38. Final Stage-2 decision

The final stable model is:

```text
OrderService
  -> immutable FulfillmentProviderKey

ReserveOrder
  -> all eligible reservable services
  -> validate AirFare reservation eligibility where applicable
  -> group by provider
  -> build provider operational units
  -> execute each provider independently

ReserveServices
  -> same pipeline for a selected subset

FulfillmentReservation
  = one logical provider reservation operation

ReservationUnit
  = one supplier operational unit
  -> covers 1..N OrderServices

FulfillmentTask
  = attempts / timeout / Unknown / retry

Order.RecordLocator
  = one immutable AeroTech host PNR
```

For FlightFlow:

```text
one initial operation can cover many passenger x flight units
CreateHold is atomic all-or-error
HoldId is operation reference
SeatHoldReference is unit reference
Unknown retry reuses same provider-effect identity
definitive new attempt creates a new reservation operation
AirFare reservation validity is checked before hold
```

This is a standard multi-supplier OTA/PSS reservation orchestration model, adapted only where AeroTech/FlightFlow contracts require it.
