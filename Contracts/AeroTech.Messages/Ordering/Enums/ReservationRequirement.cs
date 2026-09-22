using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum ReservationRequirement
    {
        [Display(Name = "None")] None = 1,
        [Display(Name = "Flight Capacity")] FlightCapacity = 2,
        [Display(Name = "Supplier")] Supplier = 3,
        [Display(Name = "Unresolved")] Unresolved = 4,
    }
}
