using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum ReissueRefundPolicy
    {
        [Display(Name = "Refundable")] Refundable = 1,
        [Display(Name = "Non Refundable")] NonRefundable = 2,
        [Display(Name = "Non Refundable Reusable")] NonRefundableReusable = 3
    }
}
