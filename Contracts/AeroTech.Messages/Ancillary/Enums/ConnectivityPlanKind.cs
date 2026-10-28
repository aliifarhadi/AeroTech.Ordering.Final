using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ConnectivityPlanKind
    {
        [Display(Name = "Messaging")] Messaging = 1,
        [Display(Name = "Time")] Time = 2,
        [Display(Name = "Data")] Data = 3,
        [Display(Name = "Full Flight")] FullFlight = 4
    }
}
