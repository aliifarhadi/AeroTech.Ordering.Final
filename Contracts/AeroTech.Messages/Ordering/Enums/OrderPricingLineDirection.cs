using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderPricingLineDirection
    {
        [Display(Name = "Debit")]
        Debit = 1,

        [Display(Name = "Credit")]
        Credit = 2
    }
}
