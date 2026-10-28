using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ServiceCoverageScope
    {
        [Display(Name = "Sector")] Sector = 1,
        [Display(Name = "Portion")] Portion = 2,
        [Display(Name = "Journey")] Journey = 3,
        [Display(Name = "Order")] Order = 4
    }
}
