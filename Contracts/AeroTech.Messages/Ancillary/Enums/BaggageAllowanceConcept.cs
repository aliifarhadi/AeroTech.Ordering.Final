using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum BaggageAllowanceConcept
    {
        [Display(Name = "Piece")] Piece = 1,
        [Display(Name = "Weight")] Weight = 2
    }
}
