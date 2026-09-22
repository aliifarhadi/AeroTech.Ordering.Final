using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.FlightFlow.Enums;

public enum FlightDisruptionPassengerHandlingStrategy
{
    [Display(Name = "Auto-Rebook")] AutoRebook = 1,
    [Display(Name = "Manual-Queue")] ManualQueue,
    [Display(Name = "Auto-Refund")] AutoRefund
}
