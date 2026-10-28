using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ServiceDateBasis
    {
        [Display(Name = "Flight Departure")] FlightDeparture = 1,
        [Display(Name = "Service Start")] ServiceStart = 2,
        [Display(Name = "Check In")] CheckIn = 3,
        [Display(Name = "Coverage Start")] CoverageStart = 4,
        [Display(Name = "Activation")] Activation = 5
    }
}
