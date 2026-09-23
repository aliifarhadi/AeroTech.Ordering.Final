using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Application._Shared.Behaviors;
using AeroTech.Ordering.Application._Shared.Caller;
using AeroTech.Ordering.Application._Shared.Events;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AeroTech.Ordering.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
            var assembly = typeof(DependencyInjection).Assembly;

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
            services.AddValidatorsFromAssembly(assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

            services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();

            services.AddScoped<ISalesContextFactory, SalesContextFactory>();
            services.AddScoped<ICallerCustomer, CallerCustomer>();
            services.AddScoped<CallerOwnedOrderAuthorization>();
            services.AddScoped<OrderInputMapper>();
            services.AddScoped<AgencyCreateOrderArgsMapper>();
            services.AddScoped<ICreateOrderFromOfferService, CreateOrderFromOfferService>();
            services.AddScoped<IAddRemarkService, AddRemarkService>();

            return services;
        }
    }
}
