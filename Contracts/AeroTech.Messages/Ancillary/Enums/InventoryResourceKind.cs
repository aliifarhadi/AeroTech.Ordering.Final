using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum InventoryResourceKind
    {
        [Display(Name = "Flight Count")] FlightCount = 1,
        [Display(Name = "Flight Weight")] FlightWeight = 2,
        [Display(Name = "Airport Slot")] AirportSlot = 3
    }
}
