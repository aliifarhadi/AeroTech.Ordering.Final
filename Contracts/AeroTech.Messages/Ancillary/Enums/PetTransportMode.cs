using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PetTransportMode
    {
        [Display(Name = "Cabin")] Cabin = 1,
        [Display(Name = "Hold")] Hold = 2
    }
}
