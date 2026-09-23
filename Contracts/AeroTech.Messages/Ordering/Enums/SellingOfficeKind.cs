using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum SellingOfficeKind
    {
        [Display(Name = "Airline Office")] AirlineOffice = 1,

        [Display(Name = "Travel Agency Office")] TravelAgencyOffice = 2
    }
}
