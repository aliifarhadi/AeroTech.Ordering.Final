using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.DomainEvents;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Entities;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using Xunit;
using static AeroTech.Ordering.Application.AcceptanceTests.Fixtures.TicketFixture;

namespace AeroTech.Ordering.Application.AcceptanceTests.Servicing;

public sealed class ElectronicTicketVoidDomainTests
{
    private const long VoidTaskId = 900;
    private const long OfficeId = 5;
    private const long ActorId = 77;

    private static readonly DateTimeOffset BeforeDeadline = LocalMidnightAfterIssue.AddMinutes(-1);

    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public void Local_issued_ticket_within_its_deadline_is_voided_with_an_exact_record()
    {
        var ticket = VoidableTicket();
        var record = Record(BeforeDeadline);

        ticket.Void(record, VoidReason.CustomerRequest, ActorId, OfficeId, _ids);

        Assert.Equal(record, ticket.VoidRecord);
        Assert.Equal(
            (VoidTaskId, nameof(VoidReason.CustomerRequest), "Passenger changed plans", (string?)null, (long?)ActorId, BeforeDeadline),
            (ticket.VoidRecord!.VoidFulfillmentTaskId, ticket.VoidRecord.ReasonCode, ticket.VoidRecord.ReasonText, ticket.VoidRecord.ProviderReference, ticket.VoidRecord.ActorId, ticket.VoidRecord.VoidedAt));
    }

    [Fact]
    public void Void_marks_every_coupon_void_the_ticket_voided_and_bumps_the_document_version()
    {
        var ticket = VoidableTicket(Coupon(101), Coupon(202));

        ticket.Void(Record(BeforeDeadline), VoidReason.CustomerRequest, ActorId, OfficeId, _ids);

        Assert.All(ticket.Coupons, coupon => Assert.Equal(TicketCouponFinancialStatus.Void, coupon.FinancialStatus));
        Assert.Equal((ElectronicTicketStatus.Voided, 2, true), (ticket.StatusSummary, ticket.DocumentVersion, ticket.IsVoided));
    }

    [Fact]
    public void Void_keeps_the_issued_document_history()
    {
        var ticket = VoidableTicket(Coupon(101, 100m, 20m));
        var issued = (ticket.DocumentNumber, ticket.IssuedTotal, ticket.IssuedAt, ticket.IssuanceContext, ticket.VoidDeadline, Links: ticket.PriceLinks.Select(link => (link.Id, link.AttributedValue)).ToList());
        var coupon = ticket.Coupons.Single();
        var couponHistory = (coupon.CouponNumber, coupon.IssuanceValue, coupon.IssuedSegment, coupon.FareBasisSnapshot, coupon.OriginalOrderServiceId);

        ticket.Void(Record(BeforeDeadline), VoidReason.CustomerRequest, ActorId, OfficeId, _ids);

        Assert.Equal(issued.DocumentNumber, ticket.DocumentNumber);
        Assert.Equal((issued.IssuedTotal, issued.IssuedAt, issued.IssuanceContext, issued.VoidDeadline), (ticket.IssuedTotal, ticket.IssuedAt, ticket.IssuanceContext, ticket.VoidDeadline));
        Assert.Equal(issued.Links, ticket.PriceLinks.Select(link => (link.Id, link.AttributedValue)));
        Assert.Equal(couponHistory, (coupon.CouponNumber, coupon.IssuanceValue, coupon.IssuedSegment, coupon.FareBasisSnapshot, coupon.OriginalOrderServiceId));
    }

    [Fact]
    public void Void_raises_the_voided_event_once_with_the_void_task_as_operation()
    {
        var ticket = VoidableTicket();
        ticket.ClearEvents();

        ticket.Void(Record(BeforeDeadline), VoidReason.CustomerRequest, ActorId, OfficeId, _ids);

        var voided = Assert.IsType<ElectronicTicketVoided>(Assert.Single(ticket.GetEvents()));
        Assert.Equal(
            (ticket.Id, OrderId, ticket.DocumentNumber, VoidTaskId, VoidReason.CustomerRequest, "Passenger changed plans", ActorId, BeforeDeadline, 2),
            (voided.ElectronicTicketId, voided.OrderId, voided.DocumentNumber, voided.VoidFulfillmentTaskId, voided.Reason, voided.ReasonText, voided.VoidedBy, voided.VoidedAt, voided.DocumentVersion));
    }

    [Fact]
    public void Voided_ticket_cannot_be_voided_again()
    {
        var ticket = VoidableTicket();
        ticket.Void(Record(BeforeDeadline), VoidReason.CustomerRequest, ActorId, OfficeId, _ids);

        var exception = Assert.Throws<BusinessException>(() => ticket.Void(Record(BeforeDeadline), VoidReason.CustomerRequest, ActorId, OfficeId, _ids));

        Assert.Equal((2814, 2), (exception.Code, ticket.DocumentVersion));
    }

    [Theory]
    [InlineData(TicketCouponFinancialStatus.Used)]
    [InlineData(TicketCouponFinancialStatus.Exchanged)]
    [InlineData(TicketCouponFinancialStatus.Refunded)]
    [InlineData(TicketCouponFinancialStatus.Suspended)]
    [InlineData(TicketCouponFinancialStatus.Void)]
    public void Coupon_that_is_not_open_blocks_the_void(TicketCouponFinancialStatus status)
    {
        var ticket = VoidableTicket(Coupon(101), Coupon(202));
        Set(ticket.Coupons.Last(), nameof(TicketCoupon.FinancialStatus), status);

        AssertRefundRequired(ticket, 2815, BeforeDeadline, OfficeId);
    }

    [Fact]
    public void Flown_coupon_blocks_the_void_even_while_still_marked_open()
    {
        var ticket = VoidableTicket();
        Set(ticket.Coupons.Single(), nameof(TicketCoupon.UsedAt), (DateTimeOffset?)IssuedAt);

        AssertRefundRequired(ticket, 2815, BeforeDeadline, OfficeId);
    }

    [Theory]
    [InlineData(TicketCouponControlStatus.External)]
    [InlineData(TicketCouponControlStatus.ReleasePending)]
    [InlineData(TicketCouponControlStatus.Unknown)]
    public void Coupon_outside_local_control_blocks_the_void(TicketCouponControlStatus status)
    {
        var ticket = VoidableTicket();
        Set(ticket.Coupons.Single(), nameof(TicketCoupon.ControlStatus), status);

        AssertRefundRequired(ticket, 2816, BeforeDeadline, OfficeId);
    }

    [Fact]
    public void External_document_authority_has_no_local_void()
    {
        var ticket = VoidableTicket();
        Set(ticket, nameof(ElectronicTicket.Authority), DocumentAuthority.External);

        AssertRefundRequired(ticket, 2817, BeforeDeadline, OfficeId);
    }

    [Fact]
    public void Ticket_without_a_void_deadline_is_refund_only()
        => AssertRefundRequired(Issue(_ids, Args([Coupon(101)])), 2811, BeforeDeadline, OfficeId);

    [Fact]
    public void Void_at_the_exact_deadline_is_refused()
        => AssertRefundRequired(VoidableTicket(), 2812, LocalMidnightAfterIssue, OfficeId);

    [Theory]
    [InlineData(6L)]
    [InlineData(null)]
    public void Only_the_issuing_office_may_void(long? callerOfficeId)
        => AssertRefundRequired(VoidableTicket(), 2813, BeforeDeadline, callerOfficeId);

    [Fact]
    public void Ticket_issued_without_an_office_has_no_local_void_authority()
    {
        var ticket = Issue(_ids, Args([Coupon(101)]) with
        {
            VoidDeadline = LocalMidnightAfterIssue,
            IssuanceContext = new DocumentIssuanceContext(10, null, null, 77, null, null, null, null, null)
        });

        AssertRefundRequired(ticket, 2813, BeforeDeadline, OfficeId);
    }

    [Fact]
    public void Deadline_is_the_next_local_midnight_of_the_issuing_office_not_twenty_four_hours()
    {
        var issuedAt = new DateTimeOffset(2026, 10, 1, 18, 0, 0, TimeSpan.Zero);

        var deadline = LocalVoidPolicy.DeadlineFor(issuedAt, "Asia/Tehran");

        Assert.Equal(new DateTimeOffset(2026, 10, 1, 20, 30, 0, TimeSpan.Zero), deadline!.Value.ToUniversalTime());
        Assert.NotEqual(issuedAt.AddHours(24), deadline);
    }

    [Fact]
    public void Issue_just_before_local_midnight_has_a_short_window()
    {
        var issuedAt = new DateTimeOffset(2026, 10, 1, 20, 29, 0, TimeSpan.Zero);

        var deadline = LocalVoidPolicy.DeadlineFor(issuedAt, "Asia/Tehran");

        Assert.Equal(TimeSpan.FromMinutes(1), deadline!.Value - issuedAt);
    }

    [Fact]
    public void Deadline_follows_the_offices_daylight_saving_offset()
    {
        var winter = LocalVoidPolicy.DeadlineFor(new DateTimeOffset(2026, 1, 15, 12, 0, 0, TimeSpan.Zero), "Europe/Berlin");
        var summer = LocalVoidPolicy.DeadlineFor(new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero), "Europe/Berlin");

        Assert.Equal(new DateTimeOffset(2026, 1, 15, 23, 0, 0, TimeSpan.Zero), winter!.Value.ToUniversalTime());
        Assert.Equal(new DateTimeOffset(2026, 7, 15, 22, 0, 0, TimeSpan.Zero), summer!.Value.ToUniversalTime());
    }

    [Fact]
    public void Deadline_on_a_day_whose_midnight_is_skipped_is_the_first_instant_of_that_day()
    {
        var deadline = LocalVoidPolicy.DeadlineFor(new DateTimeOffset(2021, 3, 21, 12, 0, 0, TimeSpan.Zero), "Asia/Tehran");

        Assert.Equal(new DateTimeOffset(2021, 3, 21, 20, 30, 0, TimeSpan.Zero), deadline!.Value.ToUniversalTime());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Mars/Olympus_Mons")]
    public void Missing_or_unknown_office_time_zone_has_no_deadline(string? timeZoneId)
        => Assert.Null(LocalVoidPolicy.DeadlineFor(IssuedAt, timeZoneId));

    private ElectronicTicket VoidableTicket(params Domain.ElectronicTicketAggregate.Arguments.IssueTicketCouponArgs[] coupons)
        => Issue(_ids, Args(coupons.Length == 0 ? [Coupon(101)] : coupons) with { VoidDeadline = LocalMidnightAfterIssue });

    private static DocumentVoidRecord Record(DateTimeOffset voidedAt)
        => new(VoidTaskId, nameof(VoidReason.CustomerRequest), "Passenger changed plans", null, ActorId, voidedAt);

    private void AssertRefundRequired(ElectronicTicket ticket, int code, DateTimeOffset voidedAt, long? callerOfficeId)
    {
        var state = (ticket.StatusSummary, ticket.DocumentVersion);
        var couponStatuses = ticket.Coupons.Select(coupon => coupon.FinancialStatus).ToList();

        var exception = Assert.Throws<BusinessException>(() => ticket.Void(Record(voidedAt), VoidReason.CustomerRequest, ActorId, callerOfficeId, _ids));

        Assert.Equal((code, 409), (exception.Code, exception.HttpStatus));
        Assert.Contains("refund", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(state, (ticket.StatusSummary, ticket.DocumentVersion));
        Assert.Equal(couponStatuses, ticket.Coupons.Select(coupon => coupon.FinancialStatus));
        Assert.Null(ticket.VoidRecord);
    }

    private static void Set<TTarget, TValue>(TTarget target, string property, TValue value)
        => typeof(TTarget).GetProperty(property)!.SetValue(target, value);
}
