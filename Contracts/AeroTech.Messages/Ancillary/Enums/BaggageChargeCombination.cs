using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum BaggageChargeCombination
    {
        [Display(Name = "Separate")] Separate = 1,
        [Display(Name = "Combined")] Combined = 2,
        [Display(Name = "Mutually Exclusive")] MutuallyExclusive = 3
    }
}
