using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum OfferBoundDirection
    {
        [Display(Name = "Outbound")] Outbound = 1,
        [Display(Name = "Inbound")] Inbound = 2
    }
}
