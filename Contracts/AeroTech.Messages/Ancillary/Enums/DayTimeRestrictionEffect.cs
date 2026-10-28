using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum DayTimeRestrictionEffect
    {
        [Display(Name = "Allow")] Allow = 1,
        [Display(Name = "Deny")] Deny = 2
    }
}
