using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SeatPurpose
    {
        [Display(Name = "Standard")] Standard = 1,
        [Display(Name = "Preferred")] Preferred = 2,
        [Display(Name = "Extra Seat")] ExtraSeat = 3
    }
}
