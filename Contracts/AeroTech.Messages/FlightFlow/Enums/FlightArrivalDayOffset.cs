using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.FlightFlow.Enums;

public enum FlightArrivalDayOffset
{
    [Display(Name = "Same Day")]SameDay = 0,
    [Display(Name = "+1 Day")]Plus1Day = 1,
    [Display(Name = "+2 Days")]Plus2Days = 2,
    [Display(Name = "-1 Day")]Minus1Day = -1,
    [Display(Name = "-2 Days")] Minus2Days = -2
}
