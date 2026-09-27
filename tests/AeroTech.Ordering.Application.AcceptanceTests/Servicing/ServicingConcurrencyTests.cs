using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fixtures;
using AeroTech.Ordering.Application.ElectronicTicketAggregate.Commands.VoidElectronicTickets;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.OrderAggregate;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class ServicingConcurrencyTests
{
    private readonly ReservationHarness _harness = new();

    [Fact]
    public async Task Cancellation_serializes_with_confirmation_on_the_same_order_lock()
    {
        var order = await ReservedOrderAsync();
        var acquiredBefore = _harness.Lock.Acquired.Count;

        await _harness.ConfirmReservedCapacityAsync(order);
        await _harness.CancelAsync(order);

        Assert.Equal([$"reservation:{order.Id}", $"reservation:{order.Id}"], _harness.Lock.Acquired.Skip(acquiredBefore));
    }

    [Fact]
    public async Task Cancellation_while_another_order_operation_runs_has_no_effect()
    {
        var order = await ReservedOrderAsync();
        await _harness.ConfirmReservedCapacityAsync(order);
        _harness.Lock.Hold($"reservation:{order.Id}");
        var savesBefore = _harness.UnitOfWork.SaveCount;

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order));

        Assert.Equal((2512, savesBefore, 1), (exception.Code, _harness.UnitOfWork.SaveCount, order.CommercialVersion));
        Assert.Empty(_harness.FlightFlow.CancelConfirmedRequests);
    }

    [Fact]
    public async Task Cancellation_after_issue_on_the_same_lock_is_refused_until_the_document_is_serviced()
    {
        var order = await ReservedOrderAsync();
        await _harness.ConfirmReservedCapacityAsync(order);
        await _harness.IssueAsync(order, (await _harness.DefineStockAsync()).DocumentStockId);

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.CancelAsync(order));

        Assert.Equal(2803, exception.Code);
        Assert.Equal(OrderStatus.Ticketed, order.Status);
    }

    [Fact]
    public async Task Void_while_another_order_operation_runs_has_no_effect()
    {
        var (order, ticketId) = await IssuedOrderAsync();
        _harness.Lock.Hold($"reservation:{order.Id}");

        var exception = await Assert.ThrowsAsync<BusinessException>(() => _harness.VoidAsync(order, ReservationHarness.IssuingOfficeId, Ticket(ticketId)));

        Assert.Equal(2512, exception.Code);
        Assert.Equal(ElectronicTicketStatus.Issued, Ticket(ticketId).StatusSummary);
    }

    [Fact]
    public async Task Racing_voids_of_one_version_produce_one_mutation_and_one_task()
    {
        var (order, ticketId) = await IssuedOrderAsync();
        var sameVersion = new[] { new ElectronicTicketVoidTarget(ticketId, 1) };

        await _harness.VoidTargetsAsync(order, ReservationHarness.IssuingOfficeId, sameVersion);
        var second = await _harness.VoidTargetsAsync(order, ReservationHarness.IssuingOfficeId, sameVersion);

        Assert.Single(_harness.Tasks.Committed, task => task.TaskType == OrderFulfillmentTaskType.VoidTicket);
        Assert.Equal(((long?)null, 2), (second.VoidFulfillmentTaskId, Ticket(ticketId).DocumentVersion));
    }

    [Fact]
    public async Task Racing_cancellations_produce_one_commercial_change()
    {
        var order = await ReservedOrderAsync();
        await _harness.ConfirmReservedCapacityAsync(order);

        var first = await _harness.CancelAsync(order);
        var second = await _harness.CancelAsync(order);

        Assert.Single(order.Changes, change => change.ChangeType == OrderChangeType.Cancel);
        Assert.Equal((first.CommercialVersion, (long?)null), (second.CommercialVersion, second.OrderChangeId));
        Assert.Single(_harness.FlightFlow.CancelConfirmedRequests);
    }

    private async Task<Order> ReservedOrderAsync()
    {
        var order = _harness.SeedOrder([TravellerSpec.Adult(1)], [new BoundSpec("OUT", 101)]);
        await _harness.ReserveOrderAsync(order);
        return order;
    }

    private async Task<(Order Order, long TicketId)> IssuedOrderAsync()
    {
        _harness.OfficeTimeZones.TimeZones[ReservationHarness.IssuingOfficeId] = "Asia/Tehran";

        var order = await ReservedOrderAsync();
        await _harness.ConfirmReservedCapacityAsync(order);
        await _harness.IssueAsync(order, (await _harness.DefineStockAsync()).DocumentStockId);

        return (order, _harness.Tickets.Committed.Single().Id);
    }

    private ElectronicTicket Ticket(long ticketId)
        => _harness.Tickets.Committed.Single(ticket => ticket.Id == ticketId);
}
