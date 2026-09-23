using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderItemCommercialState
    {
        [Display(Name = "Active")] Active = 1,
        [Display(Name = "Replaced")] Replaced = 2,
        [Display(Name = "Cancelled")] Cancelled = 3,
        [Display(Name = "Transferred")] Transferred = 4,
    }
}
