using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FarePricingUnitType
    {
        [Display(Name = "Unspecified")] Unspecified = 0,
        [Display(Name = "One Way")] OneWay = 1,
        [Display(Name = "Round Trip")] RoundTrip = 2,
        [Display(Name = "Open Jaw")] OpenJaw = 3,
        [Display(Name = "Circle Trip")] CircleTrip = 4,
        [Display(Name = "Other")] Other = 5
    }
}
