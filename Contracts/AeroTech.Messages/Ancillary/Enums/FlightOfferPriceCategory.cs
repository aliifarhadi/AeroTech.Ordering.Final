using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum FlightOfferPriceCategory
    {
        [Display(Name = "Fare")] Fare = 1,
        [Display(Name = "Tax")] Tax = 2,
        [Display(Name = "Fee")] Fee = 3,
        [Display(Name = "Surcharge")] Surcharge = 4
    }
}
