using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum AirportServiceKind
    {
        [Display(Name = "Lounge")] Lounge = 1,
        [Display(Name = "Fast Track")] FastTrack = 2,
        [Display(Name = "Cip")] Cip = 3
    }
}
