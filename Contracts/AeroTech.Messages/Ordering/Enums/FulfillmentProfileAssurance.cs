using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FulfillmentProfileAssurance
    {
        [Display(Name = "Certified")] Certified = 1,
        [Display(Name = "Not Certified")] NotCertified = 2,
    }
}
