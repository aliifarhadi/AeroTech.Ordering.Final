using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum MedicalEquipmentKind
    {
        [Display(Name = "Oxygen")] Oxygen = 1,
        [Display(Name = "Stretcher")] Stretcher = 2,
        [Display(Name = "Medical Assistance")] MedicalAssistance = 3
    }
}
