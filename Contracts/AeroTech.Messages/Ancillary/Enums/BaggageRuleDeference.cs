using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum BaggageRuleDeference
    {
        [Display(Name = "Marketing Carrier")] MarketingCarrier = 1,
        [Display(Name = "Operating Carrier")] OperatingCarrier = 2
    }
}
