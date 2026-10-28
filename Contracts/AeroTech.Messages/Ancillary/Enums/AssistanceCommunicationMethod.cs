using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AssistanceCommunicationMethod
    {
        [Display(Name = "Verbal")] Verbal = 1,
        [Display(Name = "Written")] Written = 2,
        [Display(Name = "Sign Language")] SignLanguage = 3,
        [Display(Name = "Braille")] Braille = 4
    }
}
