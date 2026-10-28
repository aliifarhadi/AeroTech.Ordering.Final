using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum UpgradeKind
    {
        [Display(Name = "Fixed Ancillary")] FixedAncillary = 1,
        [Display(Name = "Dynamic Quote")] DynamicQuote = 2,
        [Display(Name = "Ticket Reprice")] TicketReprice = 3
    }
}
