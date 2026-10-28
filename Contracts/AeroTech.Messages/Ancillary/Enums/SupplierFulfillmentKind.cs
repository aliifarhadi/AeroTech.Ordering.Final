using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SupplierFulfillmentKind
    {
        [Display(Name = "Local")] Local = 1,
        [Display(Name = "External")] External = 2
    }
}
