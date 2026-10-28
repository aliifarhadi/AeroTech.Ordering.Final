using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum CommercialDisposition
    {
        [Display(Name = "Paid")] Paid = 1,
        [Display(Name = "Free")] Free = 2,
        [Display(Name = "Not Available")] NotAvailable = 3
    }
}
