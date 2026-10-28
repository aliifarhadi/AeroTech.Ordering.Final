using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PetAnimalType
    {
        [Display(Name = "Cat")] Cat = 1,
        [Display(Name = "Dog")] Dog = 2,
        [Display(Name = "Registered Other")] RegisteredOther = 3
    }
}
