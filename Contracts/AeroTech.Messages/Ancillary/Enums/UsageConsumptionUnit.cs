using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum UsageConsumptionUnit
    {
        [Display(Name = "Purchased Unit")] PurchasedUnit = 1,
        [Display(Name = "Kilogram")] Kilogram = 2
    }
}
