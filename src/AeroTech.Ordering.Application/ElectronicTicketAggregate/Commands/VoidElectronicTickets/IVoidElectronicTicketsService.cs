using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application._Shared.Authorization;

namespace AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets
{
    public interface IVoidElectronicTicketsService
    {
        Task<VoidElectronicTicketsResult> VoidAsync(
            long orderId,
            IReadOnlyList<ElectronicTicketVoidTarget> tickets,
            VoidReason reason,
            string? reasonDetail,
            IOrderAuthorization authorization,
            long actorId,
            long? callerOfficeId,
            CancellationToken cancellationToken = default);
    }
}
