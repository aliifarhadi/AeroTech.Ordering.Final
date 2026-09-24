using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum PricingUnitKind
    {
        [Display(Name = "One Way")] OneWay = 1,
        [Display(Name = "Round Trip Fare")] RoundTripFare = 2,
        [Display(Name = "Through One Way")] ThroughOneWay = 3
    }
}
