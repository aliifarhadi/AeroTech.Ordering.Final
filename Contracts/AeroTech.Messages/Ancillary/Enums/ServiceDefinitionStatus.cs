using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ServiceDefinitionStatus
    {
        [Display(Name = "Draft")] Draft = 1,
        [Display(Name = "Active")] Active = 2,
        [Display(Name = "Suspended")] Suspended = 3,
        [Display(Name = "Retired")] Retired = 4
    }
}
