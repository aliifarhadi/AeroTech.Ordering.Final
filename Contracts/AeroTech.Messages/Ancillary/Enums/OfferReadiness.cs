using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum OfferReadiness
    {
        [Display(Name = "Not Eligible")] NotEligible = 1,
        [Display(Name = "Needs Selection")] NeedsSelection = 2,
        [Display(Name = "Needs Quote")] NeedsQuote = 3,
        [Display(Name = "Needs Verification")] NeedsVerification = 4,
        [Display(Name = "Selectable")] Selectable = 5,
        [Display(Name = "Unavailable")] Unavailable = 6
    }
}
