using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum FormOfRefund
    {
        [Display(Name = "Original Payment")] OriginalPayment = 1,
        [Display(Name = "E-Voucher")] EVoucher = 2
    }
}
