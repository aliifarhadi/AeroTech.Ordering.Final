using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PriceAssessmentStatus
    {
        [Display(Name = "Complete")] Complete = 1,
        [Display(Name = "Quote Required")] QuoteRequired = 2,
        [Display(Name = "Incomplete")] Incomplete = 3,
        [Display(Name = "Unavailable")] Unavailable = 4
    }
}
