using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum BaggagePurchaseApplication
    {
        [Display(Name = "Prepaid")] Prepaid = 1,
        [Display(Name = "Check-In")] CheckIn = 2,
        [Display(Name = "Prepaid And Check-In")] PrepaidAndCheckIn = 3
    }
}
