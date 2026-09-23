using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum PricingLineTreatment
    {
        [Display(Name = "Customer Price")] CustomerPrice = 1,
        [Display(Name = "Settlement Only")] SettlementOnly = 2,
    }
}
