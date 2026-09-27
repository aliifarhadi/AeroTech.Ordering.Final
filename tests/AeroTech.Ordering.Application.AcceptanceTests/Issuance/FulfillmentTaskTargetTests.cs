using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Domain.FulfillmentTaskAggregate;
using AeroTech.Ordering.Domain.Providers;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class FulfillmentTaskTargetTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public void Reservation_task_targets_reservation_units()
    {
        var task = Task(fulfillmentReservationId: 20, OrderFulfillmentTaskType.ReserveInventory, FulfillmentTargetKind.ReservationUnit, [31, 32], OrderFulfillmentTargetAction.Reserve);

        Assert.Equal(20, task.FulfillmentReservationId);
        Assert.Equal(
            [(FulfillmentTargetKind.ReservationUnit, 31L, OrderFulfillmentTargetAction.Reserve), (FulfillmentTargetKind.ReservationUnit, 32L, OrderFulfillmentTargetAction.Reserve)],
            task.Targets.Select(target => (target.TargetKind, target.TargetId, target.Action)));
    }

    [Fact]
    public void Document_task_has_no_reservation_and_typed_targets()
    {
        var task = Task(null, OrderFulfillmentTaskType.IssueTicket, FulfillmentTargetKind.OrderService, [41], OrderFulfillmentTargetAction.Issue);

        task.AddTargets(FulfillmentTargetKind.ElectronicTicket, [51], OrderFulfillmentTargetAction.Issue, _ids);
        task.AddTargets(FulfillmentTargetKind.TicketCoupon, [61, 62], OrderFulfillmentTargetAction.Issue, _ids);

        Assert.Null(task.FulfillmentReservationId);
        Assert.Equal(
            [(FulfillmentTargetKind.OrderService, 41L), (FulfillmentTargetKind.ElectronicTicket, 51L), (FulfillmentTargetKind.TicketCoupon, 61L), (FulfillmentTargetKind.TicketCoupon, 62L)],
            task.Targets.Select(target => (target.TargetKind, target.TargetId)));
        Assert.All(task.Targets, target => Assert.Equal(task.Id, target.FulfillmentTaskId));
    }

    [Fact]
    public void Duplicate_target_is_rejected()
    {
        var task = Task(null, OrderFulfillmentTaskType.IssueTicket, FulfillmentTargetKind.OrderService, [41], OrderFulfillmentTargetAction.Issue);

        var exception = Assert.Throws<BusinessException>(() => task.AddTargets(FulfillmentTargetKind.OrderService, [41], OrderFulfillmentTargetAction.Issue, _ids));

        Assert.Equal(2774, exception.Code);
    }

    [Fact]
    public void Local_issue_succeeds_with_one_attempt_and_no_provider_interaction()
    {
        var task = Task(null, OrderFulfillmentTaskType.IssueTicket, FulfillmentTargetKind.OrderService, [41], OrderFulfillmentTargetAction.Issue);

        task.StartAttempt(_ids, Now);
        task.CompleteAttempt(FulfillmentAttemptOutcome.Succeeded, null, Now);

        Assert.Equal((OrderFulfillmentStatus.Succeeded, 1, 0), (task.Status, task.AttemptCount, task.Interactions.Count));
        Assert.Equal(FulfillmentAttemptOutcome.Succeeded, task.Attempts.Single().Outcome);
    }

    private FulfillmentTask Task(
        long? fulfillmentReservationId,
        OrderFulfillmentTaskType taskType,
        FulfillmentTargetKind targetKind,
        long[] targetIds,
        OrderFulfillmentTargetAction action)
        => FulfillmentTask.Create(
            _ids.NewId(),
            1,
            fulfillmentReservationId,
            taskType,
            FulfillmentProviderKeys.LocalDocumentAuthority,
            "task:1",
            "order:1",
            targetKind,
            targetIds,
            action,
            _ids,
            Now);
}
