using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderPricingReason
    {
        [Display(Name = "Initial Sale", Description = "Initial Sale")]
        InitialSale = 1,

        [Display(Name = "Exchange", Description = "Exchange")]
        Exchange = 2,

        [Display(Name = "Refund", Description = "Refund")]
        Refund = 3,

        [Display(Name = "Upgrade", Description = "Upgrade")]
        Upgrade = 4,

        [Display(Name = "Penalty Collection", Description = "Penalty Collection")]
        Penalty = 5,

        [Display(Name = "Residual Issuance", Description = "Residual Issuance")]
        ResidualIssuance = 6,

        [Display(Name = "Void", Description = "Void")]
        Void = 7,

        [Display(Name = "Cancel", Description = "Cancel")]
        Cancel = 8
    }
}
