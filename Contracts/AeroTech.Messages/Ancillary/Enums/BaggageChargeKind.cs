using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum BaggageChargeKind
    {
        [Display(Name = "Extra Piece")] ExtraPiece = 1,
        [Display(Name = "Weight Package")] WeightPackage = 2,
        [Display(Name = "Overweight")] Overweight = 3,
        [Display(Name = "Oversize")] Oversize = 4,
        [Display(Name = "Special Equipment")] SpecialEquipment = 5
    }
}
