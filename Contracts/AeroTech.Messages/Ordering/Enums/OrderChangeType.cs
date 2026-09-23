using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderChangeType
    {
        [Display(Name = "Create")] Create = 1,
        [Display(Name = "Add Service")] AddProduct = 2,
        [Display(Name = "Cancel")] Cancel = 3,
        [Display(Name = "Change Service")] ChangeService = 4,
        [Display(Name = "Traveller Correction")] TravellerCorrection = 8,
        [Display(Name = "Split")] Split = 9,
        [Display(Name = "Remove Service")] RemoveService = 12,
        [Display(Name = "Refund")] Refund = 13,
        [Display(Name = "Contact Correction")] ContactCorrection = 15,
    }
}
