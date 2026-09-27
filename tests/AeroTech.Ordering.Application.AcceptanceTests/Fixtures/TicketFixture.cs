using AeroTech.Framework.Core.ServiceContracts;
using AeroTech.Messages.AirPrice.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.Arguments;
using AeroTech.Ordering.Domain.ElectronicTicketAggregate.ValueObjects;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fixtures;

public static class TicketFixture
{
    public const long OrderId = 5;
    public const long TravellerId = 11;
    public const long IssueFulfillmentTaskId = 9;
    public const int CurrencyId = 1;
    public const string DocumentNumber = "09910000000001";

    public static readonly DateTimeOffset IssuedAt = new(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset LocalMidnightAfterIssue = new(2026, 10, 2, 0, 0, 0, TimeSpan.Zero);

    public static ElectronicTicket Issue(IIdGenerator ids, IssueElectronicTicketArgs args)
        => ElectronicTicket.IssueLocally(ids.NewId(), args, ids, IssuedAt);

    public static IssueElectronicTicketArgs Args(IReadOnlyList<IssueTicketCouponArgs> coupons, string documentNumber = DocumentNumber)
        => new(
            OrderId,
            TravellerId,
            12,
            IssueFulfillmentTaskId,
            documentNumber,
            new DocumentIssuanceContext(10, null, 5, 77, null, null, null, SalesChannel.BackOffice, null),
            null,
            CurrencyId,
            coupons);

    public static IssueTicketCouponArgs Coupon(long serviceId, params decimal[] values)
        => new(
            serviceId,
            TravellerId,
            serviceId + 1000,
            serviceId + 2000,
            Segment(),
            "YOW",
            "Y",
            25,
            4,
            new BaggageAllowance(1, 20m, WeightUnit.Kg),
            (values.Length == 0 ? [100m] : values)
                .Select((value, index) => new IssueTicketPriceLinkArgs(serviceId * 10 + index, serviceId * 100 + index, value, CurrencyId))
                .ToList());

    public static IssuedSegmentSnapshot Segment()
        => new(10, 10, "DA101", 10, 11, IssuedAt.AddDays(10), IssuedAt.AddDays(10).AddHours(1), "Y", 25, 4, null);
}
