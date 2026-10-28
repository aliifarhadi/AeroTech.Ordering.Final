using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ProvisionApplicationType
    {
        [Display(Name = "Standard")] Standard = 1,
        [Display(Name = "Baggage")] Baggage = 2,
        [Display(Name = "Seat")] Seat = 3
    }
}
