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
            if (LastTicketingDate <= now)
                throw ExceptionFactory.LastTicketingDatePassed(Id, LastTicketingDate);
        }

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
            if (requiredServiceIds.Count == 0 || !ReservableStatuses.Contains(Status))
                return;

            var states = requiredServiceIds
                .Select(serviceId => latestUnitStatusByService.TryGetValue(serviceId, out var status) ? status : (ReservationMemberStatus?)null)
                .ToList();

            var positive = states.Count(status => status?.IsPositive() == true);

            if (positive == states.Count)
                TransitionTo(OrderStatus.Confirmed);
            else if (positive > 0 || states.Any(status => status?.IsUnresolved() == true))
                TransitionTo(OrderStatus.ReservationUnconfirmed);
            else if (states.Any(status => status?.IsTerminalNegative() == true))
                TransitionTo(OrderStatus.ReserveFailed);
        }

        public bool IsExpirableAt(
            DateTimeOffset now,
            IReadOnlyCollection<long> requiredServiceIds,
            IReadOnlyDictionary<long, DateTimeOffset?> validationTimeLimitByService)
            => ReservableStatuses.Contains(Status)
               && (LastTicketingDate <= now
                   || (requiredServiceIds.Count > 0
                       && requiredServiceIds.All(serviceId => validationTimeLimitByService.TryGetValue(serviceId, out var limit) && limit <= now)));

        public void Expire()
        {
            if (!ReservableStatuses.Contains(Status))
                throw ExceptionFactory.OrderCannotExpire(Id, Status);

            TransitionTo(OrderStatus.Expired);
        }
    }
}
