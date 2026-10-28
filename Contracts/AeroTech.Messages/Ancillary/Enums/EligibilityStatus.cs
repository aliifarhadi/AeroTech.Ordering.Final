using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum EligibilityStatus
    {
        [Display(Name = "Eligible")] Eligible = 1,
        [Display(Name = "Not Eligible")] NotEligible = 2,
        [Display(Name = "Insufficient Context")] InsufficientContext = 3
    }
}
