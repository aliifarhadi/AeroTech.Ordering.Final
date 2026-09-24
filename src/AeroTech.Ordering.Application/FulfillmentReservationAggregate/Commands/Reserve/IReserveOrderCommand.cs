namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve
{
    public interface IReserveOrderCommand
    {
        long OrderId { get; }
    }
}
