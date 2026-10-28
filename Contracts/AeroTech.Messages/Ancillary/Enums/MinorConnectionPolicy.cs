using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum MinorConnectionPolicy
    {
        [Display(Name = "Direct Only")] DirectOnly = 1,
        [Display(Name = "Approved Connections")] ApprovedConnections = 2
    }
}
