using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.FlightFlow.Enums;

public enum FlightDisruptionType
{
    [Display(Name = "Cancellation")] Cancellation = 1,
    [Display(Name = "Time Change")] TimeChange
}
