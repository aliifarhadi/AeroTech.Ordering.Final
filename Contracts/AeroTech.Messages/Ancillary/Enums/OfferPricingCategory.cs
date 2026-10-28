using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum OfferPricingCategory
    {
        [Display(Name = "Fare")] Fare = 0,
        [Display(Name = "Tax")] Tax = 1,
        [Display(Name = "Fee")] Fee = 2,
        [Display(Name = "Surcharge")] Surcharge = 3
    }
}
