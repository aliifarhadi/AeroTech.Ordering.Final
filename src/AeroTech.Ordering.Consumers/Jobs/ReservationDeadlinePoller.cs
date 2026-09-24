using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.Consumers.Jobs
{
    public sealed class ReservationDeadlinePoller : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ReservationDeadlineOptions _options;
        private readonly ILogger<ReservationDeadlinePoller> _logger;

        public ReservationDeadlinePoller(
            IServiceScopeFactory scopeFactory,
            IOptions<ReservationDeadlineOptions> options,
            ILogger<ReservationDeadlinePoller> logger)
        {
            _scopeFactory = scopeFactory;
            _options = options.Value;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var interval = TimeSpan.FromSeconds(_options.PollIntervalSeconds);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAsync(stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Reservation deadline polling loop failed.");
                }

                await Task.Delay(interval, stoppingToken);
            }
        }

        private async Task PollAsync(CancellationToken cancellationToken)
        {
            IReadOnlyList<long> dueOrderIds;

            using (var scope = _scopeFactory.CreateScope())
                dueOrderIds = await scope.ServiceProvider
                    .GetRequiredService<IReservationDeadlineService>()
                    .ListDueOrderIdsAsync(_options.PollBatchSize, cancellationToken);

            foreach (var orderId in dueOrderIds)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IReservationDeadlineService>().EnforceAsync(orderId, cancellationToken);
                }
                catch (BusinessException exception)
                {
                    _logger.LogInformation(exception, "Reservation deadlines of order {OrderId} were not enforced this round.", orderId);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Enforcing reservation deadlines of order {OrderId} failed.", orderId);
                }
            }
        }
    }
}
