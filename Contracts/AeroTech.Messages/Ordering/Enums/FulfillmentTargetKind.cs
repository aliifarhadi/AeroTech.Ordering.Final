using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.Ordering.Enums
{
    public enum FulfillmentTargetKind
    {
        [Display(Name = "Reservation Unit")] ReservationUnit = 1,

        [Display(Name = "Order Service")] OrderService = 2,

        [Display(Name = "Electronic Ticket")] ElectronicTicket = 3,

        [Display(Name = "Ticket Coupon")] TicketCoupon = 4,

        [Display(Name = "Electronic Misc Document")] ElectronicMiscDocument = 5,

        [Display(Name = "EMD Coupon")] EmdCoupon = 6,

        [Display(Name = "Document Stock Allocation")] DocumentStockAllocation = 7,

        [Display(Name = "Reservation Group Space")] ReservationGroupSpace = 8
    }
}
