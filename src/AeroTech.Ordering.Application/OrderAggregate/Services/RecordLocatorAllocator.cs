using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.Application.OrderAggregate.Services
{
    public sealed class RecordLocatorAllocator : IRecordLocatorAllocator
    {
        private readonly IRecordLocatorGenerator _generator;
        private readonly IOrderRepository _orders;
        private readonly int _maxAttempts;

        public RecordLocatorAllocator(
            IRecordLocatorGenerator generator,
            IOrderRepository orders,
            IOptions<RecordLocatorOptions> options)
        {
            _generator = generator;
            _orders = orders;
            _maxAttempts = options.Value.MaxAllocationAttempts;
        }

        public async Task<string> AllocateAsync(CancellationToken cancellationToken = default)
        {
            for (var attempt = 0; attempt < _maxAttempts; attempt++)
            {
                var candidate = _generator.Generate();

                if (!await _orders.RecordLocatorExistsAsync(candidate, cancellationToken))
                    return candidate;
            }

            throw ExceptionFactory.CouldNotGenerateRecordLocator();
        }
    }
}
