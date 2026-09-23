using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderTravellerStatus
    {
        [Display(Name = "Active")] Active = 1,
        [Display(Name = "Transferred")] Transferred = 2,
    }
}
