using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AvailabilityCheckState
    {
        [Display(Name = "Not Checked")] NotChecked = 1,
        [Display(Name = "Available As Of")] AvailableAsOf = 2,
        [Display(Name = "Unavailable")] Unavailable = 3,
        [Display(Name = "Unknown")] Unknown = 4,
        [Display(Name = "Source Unavailable")] SourceUnavailable = 5
    }
}
