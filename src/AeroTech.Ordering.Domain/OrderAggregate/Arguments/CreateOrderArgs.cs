using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;

namespace AeroTech.Ordering.Domain.OrderAggregate.Arguments
{
    public sealed record CreateOrderArgs(
        string OfferId,
        long CustomerId,
        SalesContext SalesContext,
        CreateOrderContactArgs Contact,
        IReadOnlyList<CreateOrderTravellerArgs> Travellers,
        IReadOnlyList<CreateOrderSeatSelectionArgs> SeatSelections,
        IReadOnlyList<CreateOrderRemarkArgs> Remarks);

    public sealed record CreateOrderContactArgs(
        string? ContactName,
        IReadOnlyList<CreateOrderContactPointArgs> ContactPoints);

    public sealed record CreateOrderContactPointArgs(
        ContactPointType Type,
        string Value,
        string? CountryCode,
        bool IsPrimary);

    public sealed record CreateOrderTravellerArgs(
        int Index,
        PassengerTypeCode PassengerType,
        AgeRange AgeRange,
        string GivenName,
        string? Surname,
        bool NoSurname,
        DateOnly? DateOfBirth,
        Gender? Gender,
        int? NationalityId,
        int? CountryOfResidenceId,
        int? InfantParentIndex,
        IReadOnlyList<CreateOrderTravellerDocumentArgs> Documents);

    public sealed record CreateOrderTravellerDocumentArgs(
        TravellerDocumentType Type,
        string Number,
        DateOnly? ExpiryDate,
        int IssuanceCountryId,
        string Holder);

    public sealed record CreateOrderSeatSelectionArgs(
        int TravellerIndex,
        string BoundId,
        string SeatNumber);

    public sealed record CreateOrderRemarkArgs(
        OrderRemarkType Type,
        OrderRemarkVisibility Visibility,
        OrderRemarkScope Scope,
        string Text,
        string? CategoryCode,
        bool IsPrintedOnItinerary,
        bool IsPrintedOnInvoice);
}
