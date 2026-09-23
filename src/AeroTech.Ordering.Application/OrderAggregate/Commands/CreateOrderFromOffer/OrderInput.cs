using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public sealed record OrderTraveller(
        int Id,
        int? AssociatedAdultId,
        PassengerTypeCode TravelerType,
        TravelerName Name,
        DateOnly? DateOfBirth,
        Gender? Gender,
        string? Nationality,
        string? CountryOfResidence,
        IReadOnlyList<TravelerDocument> Documents);

    public sealed record TravelerName(
        string FirstName,
        string? LastName,
        bool NoLastName);

    public sealed record TravelerDocument(
        TravellerDocumentType DocumentType,
        string Number,
        DateOnly? ExpiryDate,
        string IssuanceCountry,
        bool Holder);

    public sealed record OrderContact(
        string? AddresseeName,
        string? EmailAddress,
        IReadOnlyList<ContactPhone> Phones);

    public sealed record ContactPhone(
        PhoneDeviceType DeviceType,
        string CountryCallingCode,
        string Number);

    public sealed record SeatSelection(
        int TravelerId,
        string BoundId,
        string SeatNumber);

    public sealed record OrderRemarkInput(
        OrderRemarkType Type,
        OrderRemarkVisibility Visibility,
        OrderRemarkScope Scope,
        string Text,
        string? CategoryCode,
        bool IsPrintedOnItinerary,
        bool IsPrintedOnInvoice);
}
