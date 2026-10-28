using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum MealKind
    {
        [Display(Name = "Special Request")] SpecialRequest = 1,
        [Display(Name = "Paid Preorder")] PaidPreorder = 2
    }
}
