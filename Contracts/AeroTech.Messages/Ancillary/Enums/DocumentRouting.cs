using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum DocumentRouting
    {
        [Display(Name = "No Ancillary Document")] NoAncillaryDocument = 1,
        [Display(Name = "Emd")] Emd = 2,
        [Display(Name = "Ticket Or Exchange")] TicketOrExchange = 3
    }
}
