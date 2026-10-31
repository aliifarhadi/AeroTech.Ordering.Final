using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Query._Shared.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AeroTech.Ordering.Synchronizer.OrderAggregate
{
    public sealed class OrderingUnitOfWork : IUnitOfWork
    {
        private readonly OrderingDbContext _commandDbContext;
        private readonly OrderQueryDbContext _queryDbContext;

        public OrderingUnitOfWork(OrderingDbContext commandDbContext, OrderQueryDbContext queryDbContext)
        {
            _commandDbContext = commandDbContext;
            _queryDbContext = queryDbContext;
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            await using var transaction = await _commandDbContext.Database.BeginTransactionAsync(cancellationToken);

            await JoinQueryContextAsync(transaction, cancellationToken);

            try
            {
                var affected = await _commandDbContext.SaveChangesAsync(cancellationToken);
                await _queryDbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return affected;
            }
            finally
            {
                await _queryDbContext.Database.UseTransactionAsync(null, CancellationToken.None);
            }
        }

        private async Task JoinQueryContextAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
        {
            _queryDbContext.Database.SetDbConnection(_commandDbContext.Database.GetDbConnection());

            await _queryDbContext.Database.UseTransactionAsync(transaction.GetDbTransaction(), cancellationToken);
        }
    }
}
