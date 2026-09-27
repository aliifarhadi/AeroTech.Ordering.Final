namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm
{
    public interface IConfirmReservationsCommand
    {
        long OrderId { get; }

        IReadOnlyCollection<long> ReservationIds { get; }
    }
}
