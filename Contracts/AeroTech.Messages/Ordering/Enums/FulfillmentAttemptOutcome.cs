using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FulfillmentAttemptOutcome
    {
        [Display(Name = "Succeeded")] Succeeded = 1,

        [Display(Name = "Failed")] Failed = 2,

        [Display(Name = "Unknown")] Unknown = 3
    }
}
