using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.FulfillmentReservationAggregate
{
    public static class ReservationStatusRules
    {
        public static bool IsPositive(this ReservationMemberStatus status)
            => status is ReservationMemberStatus.Held or ReservationMemberStatus.Confirmed;

        public static bool IsUnresolved(this ReservationMemberStatus status)
            => status is ReservationMemberStatus.Pending or ReservationMemberStatus.Unknown or ReservationMemberStatus.Waitlisted;

        public static bool IsTerminalNegative(this ReservationMemberStatus status)
            => status is ReservationMemberStatus.Rejected
                or ReservationMemberStatus.Released
                or ReservationMemberStatus.Expired
                or ReservationMemberStatus.Cancelled;
    }
}
