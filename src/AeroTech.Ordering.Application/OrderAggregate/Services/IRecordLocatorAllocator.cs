namespace AeroTech.Ordering.Application.OrderAggregate.Services
{
    public interface IRecordLocatorAllocator
    {
        Task<string> AllocateAsync(CancellationToken cancellationToken = default);
    }
}
