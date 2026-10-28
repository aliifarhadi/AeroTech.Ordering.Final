using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SelectionScope
    {
        [Display(Name = "Per Order")] PerOrder = 1,
        [Display(Name = "Per Traveller")] PerTraveller = 2,
        [Display(Name = "Per Traveller Per Portion")] PerTravellerPerPortion = 3,
        [Display(Name = "Per Traveller Per Flight")] PerTravellerPerFlight = 4
    }
}
