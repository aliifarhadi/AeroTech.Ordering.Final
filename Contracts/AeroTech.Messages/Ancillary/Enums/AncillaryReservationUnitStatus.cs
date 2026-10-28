using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AncillaryReservationUnitStatus
    {
        [Display(Name = "Held")] Held = 1,
        [Display(Name = "Confirmed")] Confirmed = 2,
        [Display(Name = "Released")] Released = 3,
        [Display(Name = "Expired")] Expired = 4,
        [Display(Name = "Cancelled")] Cancelled = 5
    }
}
