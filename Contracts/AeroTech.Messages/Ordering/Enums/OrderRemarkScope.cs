using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum OrderRemarkScope
    {
        [Display(Name = "Order")] Order = 1,

        [Display(Name = "Traveller")] Traveller = 2,

        [Display(Name = "Segment")] Segment = 3,

        [Display(Name = "Order Item")] OrderItem = 4,

        [Display(Name = "Order Service")] OrderService = 5,

        [Display(Name = "Document")] Document = 6
    }
}
