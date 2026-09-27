using AeroTech.Ordering.Application.FulfillmentReservationAggregate.Commands.Confirm;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Persistence;
using AeroTech.Ordering.Providers.FlightFlow.Services;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Reservation;

public sealed class ConfirmationPaymentNeutralityTests
{
    [Fact]
    public void No_ordering_layer_defines_a_payment_capability()
    {
        var paymentTypes = new[]
            {
                typeof(Order).Assembly,
                typeof(ConfirmService).Assembly,
                typeof(FlightFlowReservationProvider).Assembly,
                typeof(OrderingDbContext).Assembly
            }
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.Name.Contains("Payment", StringComparison.OrdinalIgnoreCase)
                           || type.Name.Contains("JetPay", StringComparison.OrdinalIgnoreCase))
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(paymentTypes);
    }
}
