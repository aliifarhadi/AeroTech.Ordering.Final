using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum InventoryAuthority
    {
        [Display(Name = "Unlimited")] Unlimited = 1,
        [Display(Name = "Local")] Local = 2,
        [Display(Name = "Supplier")] Supplier = 3
    }
}
