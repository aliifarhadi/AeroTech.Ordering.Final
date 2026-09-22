using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Framework.Infrastructure.HealthChecks;
using AeroTech.Ordering.Persistence.Inbox;
using AeroTech.Ordering.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AeroTech.Ordering.Persistence
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("CommandDbContext")
                                   ?? configuration.GetConnectionString("OrderingDbContext");

            services.AddDbContext<OrderingDbContext>(options => options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsHistoryTable(OrderingDbContext.MigrationsHistoryTable, OrderingDbContext.MigrationsHistorySchema)));
            services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<OrderingDbContext>());
            services.Configure<IntegrationEventOptions>(configuration.GetSection("IntegrationEvents"));
            services.AddScoped<IOutboxWriter, OutboxWriter>();
            services.AddScoped<IInboxStore, InboxStore>();

            services.AddHealthChecks().AddDbContextReadinessCheck<OrderingDbContext>("sql-server-command");

            return services;
        }
    }
}
