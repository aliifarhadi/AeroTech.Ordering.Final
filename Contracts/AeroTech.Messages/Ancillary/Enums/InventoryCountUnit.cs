using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ancillary.Enums
{
    public enum InventoryCountUnit
    {
        [Display(Name = "Person")] Person = 1,
        [Display(Name = "Piece")] Piece = 2,
        [Display(Name = "Item")] Item = 3,
        [Display(Name = "Animal Carrier")] AnimalCarrier = 4,
        [Display(Name = "Equipment")] Equipment = 5
    }
}
