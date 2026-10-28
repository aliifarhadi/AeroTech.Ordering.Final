using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AncillaryProfile
    {
        [Display(Name = "Baggage")] Baggage = 1,
        [Display(Name = "Seat")] Seat = 2,
        [Display(Name = "Upgrade")] Upgrade = 3,
        [Display(Name = "Meal")] Meal = 4,
        [Display(Name = "Pet")] Pet = 5,
        [Display(Name = "Assisted Travel")] AssistedTravel = 6,
        [Display(Name = "Airport Service")] AirportService = 7,
        [Display(Name = "Priority")] Priority = 8,
        [Display(Name = "Connectivity")] Connectivity = 9
    }
}
