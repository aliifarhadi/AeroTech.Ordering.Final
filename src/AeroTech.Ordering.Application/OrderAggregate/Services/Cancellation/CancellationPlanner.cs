using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Entities;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application.OrderAggregate.Services.Cancellation
{
    public sealed class CancellationPlanner : ICancellationPlanner
    {
        public void EnsureNoActiveDocumentCovers(IReadOnlyCollection<OrderService> endingServices, IReadOnlyCollection<ElectronicTicket> tickets)
        {
            var documentedServiceIds = TicketCoverage.DocumentedServiceIds(tickets);

            if (endingServices.FirstOrDefault(service => documentedServiceIds.Contains(service.Id)) is { } documented)
                throw ExceptionFactory.OrderServiceRequiresDocumentServicing(documented.Id);
        }

        public IReadOnlyList<ReservationSettlement> PlanSettlements(
            Order order,
            IReadOnlyCollection<OrderService> endingServices,
            IReadOnlyCollection<FulfillmentReservation> reservations,
            IReadOnlySet<long> reservationsWithResumableCancellation)
        {
            var scope = ScopeOf(order, endingServices);
            var settlements = new List<ReservationSettlement>();

            foreach (var reservation in reservations)
            {
                var openUnits = scope.EndingUnitsOf(reservation).Where(unit => !unit.Status.IsTerminalNegative()).ToList();

                if (openUnits.Count == 0)
                    continue;

                var openUnitIds = openUnits.Select(unit => unit.Id).ToList();

                if (reservationsWithResumableCancellation.Contains(reservation.Id))
                    settlements.Add(new CancellationRecovery(reservation, openUnitIds));
                else if (reservation.Status == FulfillmentReservationStatus.Held)
                    settlements.Add(HeldReleaseOf(reservation, openUnitIds, scope));
                else if (openUnits.All(unit => unit.Status == ReservationMemberStatus.Confirmed))
                    settlements.Add(new ConfirmedCancellation(reservation, openUnitIds));
                else
                    throw ExceptionFactory.CancellationResourceIsUnresolved(reservation.Id, reservation.Status);
            }

            return settlements;
        }

        public bool IsSettled(
            Order order,
            IReadOnlyCollection<OrderService> endingServices,
            IReadOnlyCollection<FulfillmentReservation> reservations)
        {
            var scope = ScopeOf(order, endingServices);

            return reservations.All(reservation => scope.EndingUnitsOf(reservation).All(unit => unit.Status.IsTerminalNegative()));
        }

        private static HeldRelease HeldReleaseOf(FulfillmentReservation reservation, IReadOnlyList<long> openUnitIds, CancellationScope scope)
            => scope.RetainsAny(reservation)
                ? throw ExceptionFactory.PartialHeldCancellationIsNotSupportedByProvider(reservation.Id)
                : new HeldRelease(reservation, openUnitIds);

        private static CancellationScope ScopeOf(Order order, IReadOnlyCollection<OrderService> endingServices)
        {
            var endingServiceIds = endingServices.Select(service => service.Id).ToHashSet();

            return new CancellationScope(
                endingServiceIds,
                order.Services
                    .Where(service => service.IsActive && !endingServiceIds.Contains(service.Id))
                    .Select(service => service.Id)
                    .ToHashSet());
        }

        private sealed record CancellationScope(IReadOnlySet<long> EndingServiceIds, IReadOnlySet<long> RetainedServiceIds)
        {
            public IReadOnlyList<ReservationUnit> EndingUnitsOf(FulfillmentReservation reservation)
                => reservation.Units
                    .Where(unit => unit.OrderServiceIds.Any(EndingServiceIds.Contains) && !Retains(unit))
                    .ToList();

            public bool RetainsAny(FulfillmentReservation reservation) => reservation.Units.Any(Retains);

            private bool Retains(ReservationUnit unit) => unit.OrderServiceIds.Any(RetainedServiceIds.Contains);
        }
    }
}
