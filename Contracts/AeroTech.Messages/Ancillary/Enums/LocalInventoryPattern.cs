using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum LocalInventoryPattern
    {
        [Display(Name = "Flight Count")] FlightCount = 1,
        [Display(Name = "Flight Weight")] FlightWeight = 2,
        [Display(Name = "Flight Count Plus Weight")] FlightCountPlusWeight = 3,
        [Display(Name = "Airport Slot")] AirportSlot = 4,
        [Display(Name = "Daily Count")] DailyCount = 5,
        [Display(Name = "Room Night")] RoomNight = 6,
        [Display(Name = "Assigned Asset")] AssignedAsset = 7
    }
}
