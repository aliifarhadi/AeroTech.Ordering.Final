using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AncillaryPriceLineCategory
    {
        [Display(Name = "Ancillary")] Ancillary = 1,
        [Display(Name = "Tax")] Tax = 2,
        [Display(Name = "Fee")] Fee = 3
    }
}
