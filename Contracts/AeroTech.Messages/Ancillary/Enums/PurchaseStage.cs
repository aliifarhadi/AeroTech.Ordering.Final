using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PurchaseStage
    {
        [Display(Name = "Pre Order")] PreOrder = 1,
        [Display(Name = "Post Ticketed")] PostTicketed = 2,
        [Display(Name = "Both")] Both = 3,
        [Display(Name = "Legacy Unspecified")] LegacyUnspecified = 4,
        [Display(Name = "On Board")] OnBoard = 5
    }
}
