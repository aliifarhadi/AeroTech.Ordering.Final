using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum PricingCalculationKind
    {
        [Display(Name = "Not Recorded")] NotRecorded = 1,
        [Display(Name = "Amount")] Amount = 2,
        [Display(Name = "Percentage")] Percentage = 3,
    }
}
