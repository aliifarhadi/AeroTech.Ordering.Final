using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FundingObligationPurpose
    {
        [Display(Name = "Original Sale")] OriginalSale = 1,
        [Display(Name = "Added Service")] AddedService = 2,
        [Display(Name = "Exchange Additional Collection")] ExchangeAdditionalCollection = 3,
        [Display(Name = "Fee")] Fee = 4,
        [Display(Name = "Refund Disposition")] RefundDisposition = 5,
    }
}
