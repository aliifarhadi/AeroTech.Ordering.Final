using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.FulfillmentReservationAggregate
{
    public static class ReservationCoverage
    {
        public static IReadOnlyDictionary<long, ReservationMemberStatus> LatestUnitStatusByService(
            IEnumerable<FulfillmentReservation> reservations)
            => reservations
                .OrderBy(reservation => reservation.CreatedAt)
                .ThenBy(reservation => reservation.Id)
                .SelectMany(reservation => reservation.Units)
                .SelectMany(unit => unit.OrderServiceIds.Select(serviceId => (ServiceId: serviceId, unit.Status)))
                .GroupBy(coverage => coverage.ServiceId)
                .ToDictionary(group => group.Key, group => group.Last().Status);
    }
}
