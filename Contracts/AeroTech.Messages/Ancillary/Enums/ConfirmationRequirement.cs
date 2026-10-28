using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ConfirmationRequirement
    {
        [Display(Name = "Immediate")] Immediate = 1,
        [Display(Name = "Subject To Confirmation")] SubjectToConfirmation = 2
    }
}
