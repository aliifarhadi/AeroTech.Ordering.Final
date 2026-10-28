using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ShoppingEvidenceKind
    {
        [Display(Name = "Source")] Source = 1,
        [Display(Name = "Design")] Design = 2,
        [Display(Name = "Blocked")] Blocked = 3
    }
}
