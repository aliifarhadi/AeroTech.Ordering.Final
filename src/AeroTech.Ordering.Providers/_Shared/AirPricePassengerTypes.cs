using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain._Shared.Resources;
using AirPricePassengerTypeCode = AeroTech.Messages.AirPrice.Enums.PassengerTypeCode;

namespace AeroTech.Ordering.Providers._Shared
{
    internal static class AirPricePassengerTypes
    {
        public static AirPricePassengerTypeCode From(PassengerTypeCode passengerType, string providerName)
            => Enum.TryParse<AirPricePassengerTypeCode>(passengerType.ToString(), ignoreCase: false, out var mapped)
               && Enum.IsDefined(mapped)
                ? mapped
                : throw ExceptionFactory.PassengerTypeIsNotSupportedByProvider(passengerType, providerName);
    }
}
