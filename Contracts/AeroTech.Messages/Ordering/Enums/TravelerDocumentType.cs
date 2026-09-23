using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum TravellerDocumentType
    {
        [Display(Name = "Unknown")]
        Unknown = 0,

        [Display(Name = "Passport")]
        Passport = 1,

        [Display(Name = "Identity Card")]
        Identity = 2,

        [Display(Name = "Visa")]
        Visa = 3,

        [Display(Name = "Redress")]
        Redress = 4,

        [Display(Name = "Known Traveller")]
        KnownTraveller = 5
    }
}
