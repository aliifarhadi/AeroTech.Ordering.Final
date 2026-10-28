using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum FactEvidence
    {
        [Display(Name = "Verified")] Verified = 1,
        [Display(Name = "Partial")] Partial = 2,
        [Display(Name = "Missing")] Missing = 3
    }
}
