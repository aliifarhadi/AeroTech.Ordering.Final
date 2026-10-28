using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AncillaryQuantityUnit
    {
        [Display(Name = "Each")] Each = 1,
        [Display(Name = "Piece")] Piece = 2,
        [Display(Name = "Kilogram")] Kilogram = 3
    }
}
