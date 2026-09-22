using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Identity.Enums
{
    public enum VerificationChallengeStatus
    {
        [Display(Name = "Pending")] Pending = 1,
        [Display(Name = "Verified")] Verified = 2,
        [Display(Name = "Consumed")] Consumed = 3,
        [Display(Name = "Expired")] Expired = 4,
        [Display(Name = "Failed")] Failed = 5
    }
}
