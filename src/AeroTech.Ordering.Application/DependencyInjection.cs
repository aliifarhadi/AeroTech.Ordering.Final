using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.ReleaseReservation;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Reserve;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using AeroTech.Ordering.Application.OrderAggregate.Commands.AddRemark;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;
using AeroTech.Ordering.Application.OrderAggregate.Services;
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

            var fulfillment = configuration.GetSection(FulfillmentOptions.SectionName);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                fulfillment.Get<FulfillmentOptions>()?.LockExpirySeconds ?? 0,
                $"{FulfillmentOptions.SectionName}:{nameof(FulfillmentOptions.LockExpirySeconds)}");

            var recordLocator = configuration.GetSection(RecordLocatorOptions.SectionName);
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                recordLocator.Get<RecordLocatorOptions>()?.MaxAllocationAttempts ?? 0,
                $"{RecordLocatorOptions.SectionName}:{nameof(RecordLocatorOptions.MaxAllocationAttempts)}");

            services.Configure<FulfillmentOptions>(fulfillment);
            services.Configure<RecordLocatorOptions>(recordLocator);
            services.AddScoped<IRecordLocatorAllocator, RecordLocatorAllocator>();
            services.AddScoped<IReservationLock, ReservationLock>();
            services.AddScoped<IReservationProviderResolver, ReservationProviderResolver>();
            services.AddScoped<IOrderReservationSummarizer, OrderReservationSummarizer>();
            services.AddScoped<IReservationReleaser, ReservationReleaser>();
            services.AddScoped<IReservationDeadlineService, ReservationDeadlineService>();
            services.AddScoped<IReserveService, ReserveService>();
            services.AddScoped<IReleaseReservationService, ReleaseReservationService>();

            return services;
        }
    }
}
