using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum RoutePairDirection
    {
        [Display(Name = "Directional")] Directional = 1,
        [Display(Name = "Both Directions")] BothDirections = 2
    }
}
