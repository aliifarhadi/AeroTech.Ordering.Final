using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum RefundabilityRule
    {
        [Display(Name = "Full If All Unused", Description = "FULL IF UNUSED")]
        FullIfAllUnused = 1,

        [Display(Name = "Pro Rata", Description = "PRORATE")]
        ProRata = 2,

        [Display(Name = "Non-Refundable", Description = "NON REFUNDABLE")]
        NonRefundable = 3,

        [Display(Name = "Refundable", Description = "REFUNDABLE")]
        Refundable = 4,

        [Display(Name = "Conditional", Description = "CONDITIONAL")]
        Conditional = 5
    }
}
