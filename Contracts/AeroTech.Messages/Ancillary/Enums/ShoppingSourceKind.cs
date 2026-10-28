using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ShoppingSourceKind
    {
        [Display(Name = "Offer")] Offer = 1,
        [Display(Name = "Order")] Order = 2,
        [Display(Name = "Direct")] Direct = 3
    }
}
