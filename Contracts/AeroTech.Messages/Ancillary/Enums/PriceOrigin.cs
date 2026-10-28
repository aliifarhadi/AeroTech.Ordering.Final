using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PriceOrigin
    {
        [Display(Name = "Filed")] Filed = 1,
        [Display(Name = "External Quote")] ExternalQuote = 2,
        [Display(Name = "Free")] Free = 3,
        [Display(Name = "Not Available")] NotAvailable = 4,
        [Display(Name = "Legacy Unspecified")] LegacyUnspecified = 5
    }
}
