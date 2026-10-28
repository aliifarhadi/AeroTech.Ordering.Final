using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum BaggageTravelApplication
    {
        [Display(Name = "All Sectors")] AllSectors = 1,
        [Display(Name = "At Least One Sector")] AtLeastOneSector = 2,
        [Display(Name = "Most Significant Sector")] MostSignificantSector = 3,
        [Display(Name = "Any Sector On Journey")] AnySectorOnJourney = 4
    }
}
