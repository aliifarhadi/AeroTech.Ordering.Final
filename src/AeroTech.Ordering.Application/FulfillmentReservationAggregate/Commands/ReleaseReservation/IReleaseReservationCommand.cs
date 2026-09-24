namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation
{
    public interface IReleaseReservationCommand
    {
        long OrderId { get; }

        long ReservationId { get; }
    }
}
