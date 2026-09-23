namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public interface IOrderOwnedReadModel
    {
        long Id { get; set; }

        long OrderId { get; set; }
    }
}
