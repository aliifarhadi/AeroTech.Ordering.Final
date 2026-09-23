using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderRemarkVisibility
    {
        [Display(Name = "Internal")] Internal = 1,

        [Display(Name = "Visible To Agent")] PublicToAgent = 2,

        [Display(Name = "Visible To Customer")] PublicToCustomer = 3,

        [Display(Name = "Office Confidential")] ConfidentialOffice = 4,

        [Display(Name = "Agency Confidential")] ConfidentialAgency = 5,

        [Display(Name = "System Only")] SystemOnly = 6
    }
}
