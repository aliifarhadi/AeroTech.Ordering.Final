using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PriorityKind
    {
        [Display(Name = "Boarding")] Boarding = 1,
        [Display(Name = "Checkin")] Checkin = 2
    }
}
