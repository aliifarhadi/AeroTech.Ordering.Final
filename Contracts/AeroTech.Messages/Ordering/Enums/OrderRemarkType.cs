using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderRemarkType
    {
        [Display(Name = "General")] General = 1,

        [Display(Name = "Warning")] Warning = 2,

        [Display(Name = "Servicing")] Servicing = 3,

        [Display(Name = "Waiver")] Waiver = 4,

        [Display(Name = "System")] System = 5
    }
}
