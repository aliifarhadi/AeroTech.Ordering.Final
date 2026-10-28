using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SelectionResolutionStatus
    {
        [Display(Name = "Valid")] Valid = 1,
        [Display(Name = "Stale")] Stale = 2,
        [Display(Name = "Expired")] Expired = 3,
        [Display(Name = "Needs Quote")] NeedsQuote = 4,
        [Display(Name = "Needs Verification")] NeedsVerification = 5,
        [Display(Name = "Unavailable")] Unavailable = 6,
        [Display(Name = "Rejected")] Rejected = 7
    }
}
