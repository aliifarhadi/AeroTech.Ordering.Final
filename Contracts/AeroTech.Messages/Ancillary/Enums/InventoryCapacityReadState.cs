using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum InventoryCapacityReadState
    {
        [Display(Name = "Not Configured")] NotConfigured = 1,
        [Display(Name = "Unlimited")] Unlimited = 2,
        [Display(Name = "Configured Not Guaranteed")] ConfiguredNotGuaranteed = 3,
        [Display(Name = "Closed For Sale")] ClosedForSale = 4,
        [Display(Name = "Delegated Check Required")] DelegatedCheckRequired = 5,
        [Display(Name = "Unknown")] Unknown = 6,
        [Display(Name = "Unsupported Pattern")] UnsupportedPattern = 7,
        [Display(Name = "Not Applicable")] NotApplicable = 8
    }
}
