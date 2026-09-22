using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderServiceCommercialStatus
    {
        [Display(Name = "Pending")] Pending = 1,
        [Display(Name = "Active")] Active = 2,
        [Display(Name = "Cancelled")] Cancelled = 3,
        [Display(Name = "Replaced")] Replaced = 4,
        [Display(Name = "Expired")] Expired = 5
    }
}
