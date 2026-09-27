using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Ordering.Domain.DocumentStockAggregate.Contracts;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Contracts;
using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Synchronizer.DocumentStockAggregate;
using AeroTech.Ordering.Synchronizer.ElectronicTicketAggregate;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;

namespace AeroTech.Ordering.Synchronizer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSynchronizer(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, OrderingUnitOfWork>();
            services.AddScoped<IOrderQueryDbSynchronizer, OrderQueryDbSynchronizer>();
            services.AddScoped<IElectronicTicketQueryDbSynchronizer, ElectronicTicketQueryDbSynchronizer>();
            services.AddScoped<IDocumentStockQueryDbSynchronizer, DocumentStockQueryDbSynchronizer>();

            return services;
        }
    }
}
