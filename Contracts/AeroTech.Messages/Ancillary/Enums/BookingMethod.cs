using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum BookingMethod
    {
        [Display(Name = "SSR")] Ssr = 1,
        [Display(Name = "Auxiliary Segment")] AuxiliarySegment = 2,
        [Display(Name = "Display Price Contact Carrier For Booking")] DisplayPriceContactCarrierForBooking = 3,
        [Display(Name = "No Booking Process Required")] NoBookingProcessRequired = 4,
        [Display(Name = "Per Service Record")] PerServiceRecord = 5
    }
}
