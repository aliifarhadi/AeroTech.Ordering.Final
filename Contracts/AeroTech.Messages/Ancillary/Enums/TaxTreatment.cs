using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum TaxTreatment
    {
        [Display(Name = "Added To Base")] AddedToBase = 1,
        [Display(Name = "Included In Base")] IncludedInBase = 2,
        [Display(Name = "Legacy Unknown")] LegacyUnknown = 3
    }
}
