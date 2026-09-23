using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer
{
    public static class PassengerAgeRange
    {
        public static AgeRange Of(PassengerTypeCode passengerType)
            => passengerType switch
            {
                PassengerTypeCode.INF => AgeRange.Infant,
                PassengerTypeCode.INN => AgeRange.Infant,
                PassengerTypeCode.CHD => AgeRange.Child,
                PassengerTypeCode.CNN => AgeRange.Child,
                _ => AgeRange.Adult
            };
    }
}
