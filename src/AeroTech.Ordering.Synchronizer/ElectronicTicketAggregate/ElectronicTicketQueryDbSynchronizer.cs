using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Query.ElectronicTicketAggregate.Models;
using AeroTech.Ordering.Query._Shared.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Synchronizer.ElectronicTicketAggregate
{
    public sealed class ElectronicTicketQueryDbSynchronizer : IElectronicTicketQueryDbSynchronizer
    {
        private readonly OrderQueryDbContext _dbContext;

        public ElectronicTicketQueryDbSynchronizer(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public Task ProjectIssuedAsync(IReadOnlyList<ElectronicTicketReadModelSnapshot> tickets, CancellationToken cancellationToken = default)
            => UpsertAsync(tickets, cancellationToken);

        public Task ProjectVoidedAsync(IReadOnlyList<ElectronicTicketReadModelSnapshot> tickets, CancellationToken cancellationToken = default)
            => UpsertAsync(tickets, cancellationToken);

        private async Task UpsertAsync(IReadOnlyList<ElectronicTicketReadModelSnapshot> tickets, CancellationToken cancellationToken)
        {
            var ticketIds = tickets.Select(ticket => ticket.TicketId).ToList();
            var storedTickets = await _dbContext.ElectronicTickets
                .Where(row => ticketIds.Contains(row.Id))
                .ToDictionaryAsync(row => row.Id, cancellationToken);
            var storedCoupons = await _dbContext.TicketCoupons
                .Where(row => ticketIds.Contains(row.ElectronicTicketId))
                .ToDictionaryAsync(row => row.Id, cancellationToken);

            foreach (var ticket in tickets)
            {
                if (!storedTickets.TryGetValue(ticket.TicketId, out var ticketRow))
                    _dbContext.ElectronicTickets.Add(ticketRow = new ElectronicTicketReadModel { Id = ticket.TicketId });

                Apply(ticket, ticketRow);

                foreach (var coupon in ticket.Coupons)
                {
                    if (!storedCoupons.TryGetValue(coupon.CouponId, out var couponRow))
                        _dbContext.TicketCoupons.Add(couponRow = new TicketCouponReadModel { Id = coupon.CouponId });

                    Apply(ticket, coupon, couponRow);
                }
            }
        }

        private static void Apply(ElectronicTicketReadModelSnapshot source, ElectronicTicketReadModel target)
        {
            target.OrderId = source.OrderId;
            target.TravellerId = source.TravellerId;
            target.DocumentNumber = source.DocumentNumber;
            target.Authority = source.Authority;
            target.IssuedAt = source.IssuedAt;
            target.IssuedTotal = source.IssuedTotal;
            target.CurrencyId = source.CurrencyId;
            target.StatusSummary = source.StatusSummary;
            target.DocumentVersion = source.DocumentVersion;
            target.VoidDeadline = source.VoidDeadline;
            target.VoidFulfillmentTaskId = source.VoidRecord?.VoidFulfillmentTaskId;
            target.VoidReasonCode = source.VoidRecord?.ReasonCode;
            target.VoidReasonText = source.VoidRecord?.ReasonText;
            target.VoidProviderReference = source.VoidRecord?.ProviderReference;
            target.VoidActorId = source.VoidRecord?.ActorId;
            target.VoidedAt = source.VoidRecord?.VoidedAt;
        }

        private static void Apply(ElectronicTicketReadModelSnapshot ticket, TicketCouponReadModelSnapshot source, TicketCouponReadModel target)
        {
            target.OrderId = ticket.OrderId;
            target.ElectronicTicketId = ticket.TicketId;
            target.CouponNumber = source.CouponNumber;
            target.OriginalOrderServiceId = source.OriginalOrderServiceId;
            target.CurrentOrderServiceId = source.CurrentOrderServiceId;
            target.OrderSegmentId = source.OrderSegmentId;
            target.FareBasisSnapshot = source.FareBasisSnapshot;
            target.BookingClassSnapshot = source.BookingClassSnapshot;
            target.RbdIdSnapshot = source.RbdIdSnapshot;
            target.CabinClassIdSnapshot = source.CabinClassIdSnapshot;
            target.IssuanceValue = source.IssuanceValue;
            target.CurrencyId = source.CurrencyId;
            target.FinancialStatus = source.FinancialStatus;
            target.ControlStatus = source.ControlStatus;
        }
    }
}
