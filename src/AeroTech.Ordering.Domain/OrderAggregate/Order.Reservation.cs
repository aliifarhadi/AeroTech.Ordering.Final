using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.FulfillmentReservationAggregate;
using AeroTech.Ordering.Domain.OrderAggregate.Entities;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate
{
    public sealed partial class Order
    {
        public static readonly IReadOnlyCollection<OrderStatus> ReservableStatuses =
        [
            OrderStatus.Created,
            OrderStatus.Confirmed,
            OrderStatus.ReservationUnconfirmed,
            OrderStatus.ReserveFailed
        ];

        public void EnsureReservable()
        {
            if (!ReservableStatuses.Contains(Status))
                throw ExceptionFactory.OrderIsNotReservable(Id, Status);
        }

        public void EnsureNewReservationAllowedAt(DateTimeOffset now)
        {
            if (HasPassedLastTicketingDateAt(now))
                throw ExceptionFactory.LastTicketingDatePassed(Id, LastTicketingDate);
        }

        public bool HasPassedLastTicketingDateAt(DateTimeOffset now) => LastTicketingDate <= now;

        public void AssignRecordLocator(string recordLocator)
        {
            if (RecordLocator is not null)
                return;

            if (string.IsNullOrWhiteSpace(recordLocator))
                throw ExceptionFactory.RecordLocatorIsRequired();

            RecordLocator = recordLocator;
        }

        public bool RequiresRecordLocator(IReadOnlyDictionary<long, ReservationMemberStatus> latestUnitStatusByService)
            => RecordLocator is null
               && _services.OfType<OrderAirTransportService>().Any(service =>
                   latestUnitStatusByService.TryGetValue(service.Id, out var status) && status.IsPositive());

        public void SummarizeReservation(
            IReadOnlyCollection<long> requiredServiceIds,
            IReadOnlyDictionary<long, ReservationMemberStatus> latestUnitStatusByService)
        {
            if (ReservableStatuses.Contains(Status) && ReservationStatusOf(requiredServiceIds, latestUnitStatusByService) is { } status)
                TransitionTo(status);
        }

        public void SummarizeServicing(
            IReadOnlySet<long> documentedServiceIds,
            IReadOnlyCollection<long> requiredServiceIds,
            IReadOnlyDictionary<long, ReservationMemberStatus> latestUnitStatusByService)
        {
            var ticketableAirServices = TicketableAirServices();

            if (!_services.Any(service => service.IsActive))
                TransitionTo(OrderStatus.Cancelled);
            else if (ticketableAirServices.Count > 0 && ticketableAirServices.All(service => documentedServiceIds.Contains(service.Id)))
                TransitionTo(OrderStatus.Ticketed);
            else
                TransitionTo(ReservationStatusOf(requiredServiceIds, latestUnitStatusByService) ?? OrderStatus.Created);
        }

        public bool IsExpirableAt(DateTimeOffset now) => ReservableStatuses.Contains(Status) && HasPassedLastTicketingDateAt(now);

        public void Expire()
        {
            if (!ReservableStatuses.Contains(Status))
                throw ExceptionFactory.OrderCannotExpire(Id, Status);

            TransitionTo(OrderStatus.Expired);
        }

        private static OrderStatus? ReservationStatusOf(
            IReadOnlyCollection<long> requiredServiceIds,
            IReadOnlyDictionary<long, ReservationMemberStatus> latestUnitStatusByService)
        {
            if (requiredServiceIds.Count == 0)
                return null;

            var states = requiredServiceIds
                .Select(serviceId => latestUnitStatusByService.TryGetValue(serviceId, out var status) ? status : (ReservationMemberStatus?)null)
                .ToList();

            if (states.All(status => status == ReservationMemberStatus.Confirmed))
                return OrderStatus.Confirmed;

            if (states.Any(status => status?.IsPositive() == true || status?.IsUnresolved() == true))
                return OrderStatus.ReservationUnconfirmed;

            return states.Any(status => status?.IsTerminalNegative() == true) ? OrderStatus.ReserveFailed : null;
        }
    }
}
