using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ShoppingStage
    {
        [Display(Name = "Pre Order")] PreOrder = 1,
        [Display(Name = "Post Ticketed")] PostTicketed = 2,
        [Display(Name = "On Board")] OnBoard = 3
    }
}
