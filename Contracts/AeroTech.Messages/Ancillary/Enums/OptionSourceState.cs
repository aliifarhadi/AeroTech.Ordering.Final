using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum OptionSourceState
    {
        [Display(Name = "Known")] Known = 1,
        [Display(Name = "Unknown")] Unknown = 2,
        [Display(Name = "Not Applicable")] NotApplicable = 3
    }
}
