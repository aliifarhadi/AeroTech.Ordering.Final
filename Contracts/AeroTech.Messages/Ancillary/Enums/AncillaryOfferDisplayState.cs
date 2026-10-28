using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AncillaryOfferDisplayState
    {
        [Display(Name = "Available For Selection")] AvailableForSelection = 1,
        [Display(Name = "Needs Input")] NeedsInput = 2,
        [Display(Name = "Quote Required")] QuoteRequired = 3,
        [Display(Name = "Needs Verification")] NeedsVerification = 4,
        [Display(Name = "Unavailable")] Unavailable = 5,
        [Display(Name = "Not Eligible")] NotEligible = 6
    }
}
