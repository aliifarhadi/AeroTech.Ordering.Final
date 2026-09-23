using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderPricingLineCategory
    {
        [Display(Name = "Fare")]
        Fare = 1,

        [Display(Name = "Tax")]
        Tax = 2,

        [Display(Name = "Fee")]
        Fee = 3,

        [Display(Name = "Penalty")]
        Penalty = 4,

        [Display(Name = "Discount")]
        Discount = 5,

        [Display(Name = "Charge")]
        Charge = 6,

        [Display(Name = "Carrier Imposed Surcharge")]
        CarrierImposedSurcharge = 7,

        [Display(Name = "Ancillary")]
        Ancillary = 8,

        [Display(Name = "Commission")]
        Commission = 9,

        [Display(Name = "Rounding")]
        Rounding = 100
    }
}
