using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderRemarkStatus
    {
        [Display(Name = "Active")] Active = 1,

        [Display(Name = "Superseded")] Superseded = 2,

        [Display(Name = "Deleted")] Deleted = 3
    }
}
