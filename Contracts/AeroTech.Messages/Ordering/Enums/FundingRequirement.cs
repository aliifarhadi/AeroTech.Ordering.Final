using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FundingRequirement
    {
        [Display(Name = "Unresolved")] Unresolved = 1,
        [Display(Name = "Not Required")] NotRequired = 2,
        [Display(Name = "Required")] Required = 3,
    }
}
