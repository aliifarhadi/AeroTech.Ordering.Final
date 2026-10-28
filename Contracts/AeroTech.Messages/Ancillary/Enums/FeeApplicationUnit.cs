using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum FeeApplicationUnit
    {
        [Display(Name = "One Way")] OneWay = 1,
        [Display(Name = "Round Trip")] RoundTrip = 2,
        [Display(Name = "Item")] Item = 3,
        [Display(Name = "Sector Or Portion")] SectorOrPortion = 4,
        [Display(Name = "Ticket")] Ticket = 5,
        [Display(Name = "Per One Kilogram Over")] PerOneKilogramOver = 6,
        [Display(Name = "Per Five Kilograms Over")] PerFiveKilogramsOver = 7,
        [Display(Name = "Half Percent Of Fare Per Kilogram")] HalfPercentOfFarePerKilogram = 8,
        [Display(Name = "One Percent Of Fare Per Kilogram")] OnePercentOfFarePerKilogram = 9,
        [Display(Name = "One And Half Percent Of Fare Per Kilogram")] OneAndHalfPercentOfFarePerKilogram = 10
    }
}
