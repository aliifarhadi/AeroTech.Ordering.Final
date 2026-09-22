using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum AcceptanceAssurance
    {
        [Display(Name = "Owner Bound")] OwnerBound = 1,
        [Display(Name = "Local Candidate Only")] LocalCandidateOnly = 2,
    }
}
