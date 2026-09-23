using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum Gender
    {
        [Display(Name = "Female", Description = "FEMALE")]
        Female = 1,

        [Display(Name = "Male", Description = "MALE")]
        Male = 2,

        [Display(Name = "Unspecified", Description = "UNSPECIFIED")]
        UnSpecified = 3,

        [Display(Name = "Undisclosed", Description = "UNDISCLOSED")]
        UnDisclosed = 4
    }
}
