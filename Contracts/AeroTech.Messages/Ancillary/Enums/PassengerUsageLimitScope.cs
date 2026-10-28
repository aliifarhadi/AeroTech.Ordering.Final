using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PassengerUsageLimitScope
    {
        [Display(Name = "Per Order")] PerOrder = 1,
        [Display(Name = "Per Flight Occurrence")] PerFlightOccurrence = 2,
        [Display(Name = "Per Service Date")] PerServiceDate = 3,
        [Display(Name = "Per Portion")] PerPortion = 4
    }
}
