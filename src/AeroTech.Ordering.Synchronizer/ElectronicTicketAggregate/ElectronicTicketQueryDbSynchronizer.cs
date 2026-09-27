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

        public async Task ProjectIssuedAsync(IReadOnlyList<ElectronicTicketReadModelSnapshot> tickets, CancellationToken cancellationToken = default)
        {
            var ticketIds = tickets.Select(ticket => ticket.TicketId).ToList();
            var projectedIds = await _dbContext.ElectronicTickets
                .Where(row => ticketIds.Contains(row.Id))
                .Select(row => row.Id)
                .ToListAsync(cancellationToken);

            foreach (var ticket in tickets.Where(ticket => !projectedIds.Contains(ticket.TicketId)))
            {
                _dbContext.ElectronicTickets.Add(new ElectronicTicketReadModel
                {
                    Id = ticket.TicketId,
                    OrderId = ticket.OrderId,
                    TravellerId = ticket.TravellerId,
                    DocumentNumber = ticket.DocumentNumber,
                    Authority = ticket.Authority,
                    IssuedAt = ticket.IssuedAt,
                    IssuedTotal = ticket.IssuedTotal,
                    CurrencyId = ticket.CurrencyId,
                    StatusSummary = ticket.StatusSummary,
                    DocumentVersion = ticket.DocumentVersion
                });

                _dbContext.TicketCoupons.AddRange(ticket.Coupons.Select(coupon => new TicketCouponReadModel
                {
                    Id = coupon.CouponId,
                    OrderId = ticket.OrderId,
                    ElectronicTicketId = ticket.TicketId,
                    CouponNumber = coupon.CouponNumber,
                    OriginalOrderServiceId = coupon.OriginalOrderServiceId,
                    CurrentOrderServiceId = coupon.CurrentOrderServiceId,
                    OrderSegmentId = coupon.OrderSegmentId,
                    FareBasisSnapshot = coupon.FareBasisSnapshot,
                    BookingClassSnapshot = coupon.BookingClassSnapshot,
                    RbdIdSnapshot = coupon.RbdIdSnapshot,
                    CabinClassIdSnapshot = coupon.CabinClassIdSnapshot,
                    IssuanceValue = coupon.IssuanceValue,
                    CurrencyId = coupon.CurrencyId,
                    FinancialStatus = coupon.FinancialStatus,
                    ControlStatus = coupon.ControlStatus
                }));
            }
        }
    }
}
