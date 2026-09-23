using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain._Shared.Contracts;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public sealed class OrderInputMapper
    {
        private readonly ICountryCodeResolver _countries;

        public OrderInputMapper(ICountryCodeResolver countries) => _countries = countries;

        public async Task<IReadOnlyList<CreateOrderTravellerArgs>> ToTravellerArgsAsync(
            IReadOnlyList<OrderTraveller> travellers,
            CancellationToken cancellationToken)
        {
            var mapped = new List<CreateOrderTravellerArgs>(travellers.Count);

            foreach (var traveller in travellers)
            {
                var documents = new List<CreateOrderTravellerDocumentArgs>(traveller.Documents.Count);

                foreach (var document in traveller.Documents)
                    documents.Add(new CreateOrderTravellerDocumentArgs(
                        document.DocumentType,
                        document.Number,
                        document.ExpiryDate,
                        await _countries.ResolveAsync(document.IssuanceCountry, cancellationToken),
                        document.Holder ? traveller.Name.FirstName : string.Empty));

                mapped.Add(new CreateOrderTravellerArgs(
                    traveller.Id,
                    traveller.TravelerType,
                    PassengerAgeRange.Of(traveller.TravelerType),
                    traveller.Name.FirstName,
                    traveller.Name.LastName,
                    traveller.Name.NoLastName,
                    traveller.DateOfBirth,
                    traveller.Gender,
                    await _countries.ResolveOptionalAsync(traveller.Nationality, cancellationToken),
                    await _countries.ResolveOptionalAsync(traveller.CountryOfResidence, cancellationToken),
                    traveller.AssociatedAdultId,
                    documents));
            }

            return mapped;
        }

        public static CreateOrderContactArgs ToContactArgs(OrderContact contact)
        {
            var points = new List<CreateOrderContactPointArgs>();

            if (!string.IsNullOrWhiteSpace(contact.EmailAddress))
                points.Add(new CreateOrderContactPointArgs(ContactPointType.Email, contact.EmailAddress, null, points.Count == 0));

            foreach (var phone in contact.Phones)
                points.Add(new CreateOrderContactPointArgs(
                    ToContactPointType(phone.DeviceType),
                    phone.Number,
                    phone.CountryCallingCode,
                    points.Count == 0));

            return new CreateOrderContactArgs(contact.AddresseeName, points);
        }

        public static IReadOnlyList<CreateOrderSeatSelectionArgs> ToSeatArgs(IReadOnlyList<SeatSelection> seats)
            => seats
                .Select(seat => new CreateOrderSeatSelectionArgs(seat.TravelerId, seat.BoundId, seat.SeatNumber))
                .ToList();

        public static IReadOnlyList<CreateOrderRemarkArgs> ToRemarkArgs(IReadOnlyList<OrderRemarkInput> remarks)
            => remarks
                .Select(remark => new CreateOrderRemarkArgs(
                    remark.Type,
                    remark.Visibility,
                    remark.Scope,
                    remark.Text,
                    remark.CategoryCode,
                    remark.IsPrintedOnItinerary,
                    remark.IsPrintedOnInvoice))
                .ToList();

        private static ContactPointType ToContactPointType(PhoneDeviceType deviceType)
            => deviceType switch
            {
                PhoneDeviceType.SMS => ContactPointType.Sms,
                PhoneDeviceType.WAP => ContactPointType.WhatsApp,
                PhoneDeviceType.TLG => ContactPointType.Telegram,
                _ => ContactPointType.Phone
            };
    }
}
