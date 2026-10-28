using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum SelectionKind
    {
        [Display(Name = "Simple Opt In")] SimpleOptIn = 1,
        [Display(Name = "Quantity Choice")] QuantityChoice = 2,
        [Display(Name = "Typed Form")] TypedForm = 3,
        [Display(Name = "Seat Map Selection")] SeatMapSelection = 4,
        [Display(Name = "External Quote")] ExternalQuote = 5
    }
}
