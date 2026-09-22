using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum ValidityState
    {
        [Display(Name = "Known")] Known = 1,
        [Display(Name = "Not Supplied")] NotSupplied = 2,
        [Display(Name = "Not Applicable")] NotApplicable = 3,
    }
}
