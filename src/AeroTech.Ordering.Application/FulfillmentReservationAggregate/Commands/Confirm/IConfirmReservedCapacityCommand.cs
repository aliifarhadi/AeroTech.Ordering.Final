namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm
{
    public interface IConfirmReservedCapacityCommand
    {
        long OrderId { get; }
    }
}
