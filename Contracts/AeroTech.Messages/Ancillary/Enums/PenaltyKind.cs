using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PenaltyKind
    {
        [Display(Name = "None")] None = 1,
        [Display(Name = "Fixed Amount")] FixedAmount = 2,
        [Display(Name = "Percentage")] Percentage = 3,
        [Display(Name = "Provider Quoted")] ProviderQuoted = 4,
        [Display(Name = "Manual Review")] ManualReview = 5
    }
}
