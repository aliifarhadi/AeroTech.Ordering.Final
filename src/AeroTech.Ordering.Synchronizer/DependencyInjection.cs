using AeroTech.Framework.Core.Domain.Repository;
using AeroTech.Ordering.Synchronizer.OrderAggregate;
using Microsoft.Extensions.DependencyInjection;

namespace AeroTech.Ordering.Synchronizer
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddSynchronizer(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, OrderingUnitOfWork>();

            return services;
        }
    }
}
