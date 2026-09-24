namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve
{
    public interface IReserveServicesCommand
    {
        long OrderId { get; }

        IReadOnlyCollection<long> OrderServiceIds { get; }
    }
}
