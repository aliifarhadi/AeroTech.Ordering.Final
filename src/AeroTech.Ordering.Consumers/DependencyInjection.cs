using AeroTech.Ordering.Consumers.Inbox;
using AeroTech.Ordering.Consumers.Jobs;
using AeroTech.Ordering.Consumers.Ordering.OrderAggregate.WhenOrderCreated;
using AeroTech.Ordering.Consumers.Outbox;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AeroTech.Ordering.Consumers
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddConsumers(this IServiceCollection services, IConfiguration configuration)
        {
            var broker = configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();

            ArgumentException.ThrowIfNullOrWhiteSpace(broker.Server, "RabbitMq:Server");
            ArgumentException.ThrowIfNullOrWhiteSpace(broker.UserName, "RabbitMq:UserName");
            ArgumentException.ThrowIfNullOrWhiteSpace(broker.Password, "RabbitMq:Password");

            services.AddScoped(typeof(InboxConsumeFilter<>));

            services.AddMassTransit(bus =>
            {
                bus.SetKebabCaseEndpointNameFormatter();

                bus.AddConsumer<SyncQueryDbWhenOrderCreated>();

                bus.UsingRabbitMq((context, rabbit) =>
                {
                    rabbit.Host(broker.Server, broker.Port, broker.VirtualHost, configurator =>
                    {
                        configurator.Username(broker.UserName);
                        configurator.Password(broker.Password);
                    });

                    rabbit.UseConsumeFilter(typeof(InboxConsumeFilter<>), context);

                    rabbit.ReceiveEndpoint("DotAir.AeroTech.Ordering.QuerySynchronizer", endpoint =>
                    {
                        endpoint.UseMessageRetry(retry => retry.Immediate(3));
                        endpoint.ConfigureConsumer<SyncQueryDbWhenOrderCreated>(context);
                    });
                });
            });

            var reservationDeadline = configuration.GetSection(ReservationDeadlineOptions.SectionName);
            var reservationDeadlineOptions = reservationDeadline.Get<ReservationDeadlineOptions>();

            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                reservationDeadlineOptions?.PollIntervalSeconds ?? 0,
                $"{ReservationDeadlineOptions.SectionName}:{nameof(ReservationDeadlineOptions.PollIntervalSeconds)}");
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
                reservationDeadlineOptions?.PollBatchSize ?? 0,
                $"{ReservationDeadlineOptions.SectionName}:{nameof(ReservationDeadlineOptions.PollBatchSize)}");

            services.Configure<OutboxPublisherOptions>(configuration.GetSection("Outbox"));
            services.Configure<MessageRetentionOptions>(configuration.GetSection("MessageRetention"));
            services.Configure<ReservationDeadlineOptions>(reservationDeadline);
            services.AddHostedService<OutboxPublisher>();
            services.AddHostedService<MessageRetentionPoller>();
            services.AddHostedService<ReservationDeadlinePoller>();

            return services;
        }
    }
}
