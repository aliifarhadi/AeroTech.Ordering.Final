using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ExtraSeatPurpose
    {
        [Display(Name = "Passenger Comfort")] PassengerComfort = 1,
        [Display(Name = "Cabin Baggage")] CabinBaggage = 2
    }
}
