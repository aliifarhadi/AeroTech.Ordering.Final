using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderItemCommercialStatus
    {
        [Display(Name = "Active")] Active = 1,
        [Display(Name = "Replaced")] Replaced = 2,
        [Display(Name = "Cancelled")] Cancelled = 3,
        [Display(Name = "PartiallyChanged")] PartiallyChanged = 4,
        [Display(Name = "Partitioned")] Partitioned = 5,
        [Display(Name = "Expired")] Expired = 6,
        [Display(Name = "Inactive")] Inactive = 7,
    }
}
