using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SelectionEvaluationStatus
    {
        [Display(Name = "Accepted")] Accepted = 1,
        [Display(Name = "Incomplete")] Incomplete = 2,
        [Display(Name = "Rejected")] Rejected = 3
    }
}
