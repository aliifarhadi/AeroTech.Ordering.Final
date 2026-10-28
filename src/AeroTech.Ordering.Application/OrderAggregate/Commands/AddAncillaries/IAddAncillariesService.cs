using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries
{
    public interface IAddAncillariesService
    {
        Task<AddAncillariesResult> AddAsync(
            long orderId,
            int expectedCommercialVersion,
            string selectionId,
            IOrderAuthorization authorization,
            SalesContext actorContext,
            CancellationToken cancellationToken = default);
    }
}
