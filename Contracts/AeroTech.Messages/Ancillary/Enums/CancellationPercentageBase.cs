using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum CancellationPercentageBase
    {
        [Display(Name = "Service Base")] ServiceBase = 1,
        [Display(Name = "Service Total")] ServiceTotal = 2
    }
}
