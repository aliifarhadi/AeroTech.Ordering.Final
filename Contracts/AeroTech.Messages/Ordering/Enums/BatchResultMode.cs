using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum BatchResultMode
    {
        [Display(Name = "Atomic All Or Nothing")] AtomicAllOrNothing = 1,

        [Display(Name = "Per Unit")] PerUnit = 2
    }
}
