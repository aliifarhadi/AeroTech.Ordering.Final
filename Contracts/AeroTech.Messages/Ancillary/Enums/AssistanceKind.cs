using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AssistanceKind
    {
        [Display(Name = "Wheelchair")] Wheelchair = 1,
        [Display(Name = "Disability Assistance")] DisabilityAssistance = 2,
        [Display(Name = "Medical Equipment")] MedicalEquipment = 3,
        [Display(Name = "Bassinet")] Bassinet = 4,
        [Display(Name = "Unaccompanied Minor")] UnaccompaniedMinor = 5
    }
}
