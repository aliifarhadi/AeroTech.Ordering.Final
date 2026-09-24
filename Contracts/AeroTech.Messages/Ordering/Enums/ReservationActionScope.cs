using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum ReservationActionScope
    {
        [Display(Name = "Operation")] Operation = 1,

        [Display(Name = "Unit")] Unit = 2
    }
}
