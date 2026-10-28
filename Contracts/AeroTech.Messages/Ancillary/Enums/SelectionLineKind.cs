using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SelectionLineKind
    {
        [Display(Name = "Ancillary")] Ancillary = 1,
        [Display(Name = "Seat Map")] SeatMap = 2
    }
}
