using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AirportServiceDirection
    {
        [Display(Name = "Departure")] Departure = 1,
        [Display(Name = "Arrival")] Arrival = 2,
        [Display(Name = "Transfer")] Transfer = 3,
        [Display(Name = "Any")] Any = 4
    }
}
