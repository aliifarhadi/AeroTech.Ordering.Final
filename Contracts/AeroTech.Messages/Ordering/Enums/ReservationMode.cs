using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum ReservationMode
    {
        [Display(Name = "Hold Then Confirm")] HoldThenConfirm = 1,

        [Display(Name = "Immediate Confirm")] ImmediateConfirm = 2,

        [Display(Name = "None")] None = 3
    }
}
