using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ExistingServiceCommercialState
    {
        [Display(Name = "Active")] Active = 1,
        [Display(Name = "Cancelled")] Cancelled = 2,
        [Display(Name = "Refunded")] Refunded = 3
    }
}
