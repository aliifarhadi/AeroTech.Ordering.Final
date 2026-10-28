using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum PricingUnit
    {
        [Display(Name = "Per Passenger")] PerPassenger = 1,
        [Display(Name = "Per Room")] PerRoom = 2,
        [Display(Name = "Per Item")] PerItem = 3,
        [Display(Name = "Per Vehicle")] PerVehicle = 4,
        [Display(Name = "Per Seat")] PerSeat = 5,
        [Display(Name = "Per Piece")] PerPiece = 6,
        [Display(Name = "Per Kilogram")] PerKilogram = 7
    }
}
