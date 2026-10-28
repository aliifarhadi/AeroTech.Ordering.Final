using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SignedSelectionStatus
    {
        [Display(Name = "Valid")] Valid = 1,
        [Display(Name = "Expired")] Expired = 2,
        [Display(Name = "Rejected")] Rejected = 3
    }
}
