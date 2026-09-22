using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderItemKind
    {
        [Display(Name = "Offer Package")] OfferPackage = 1,
        [Display(Name = "Product")] Product = 2,
        [Display(Name = "Monetary Charge")] MonetaryCharge = 3,
    }
}
