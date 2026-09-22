using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.FlightFlow.Enums;

public enum FlightDisruptionTimeChangeType
{
    [Display(Name = "Delay")] Delay = 1,
    [Display(Name = "Advancement")] Advancement
}
