using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.FlightFlow.Enums;

public enum FlightStopType
{
    [Display(Name = "Technical Stop")]
    Technical = 1,
    [Display(Name = "Passenger Stop")]
    Passenger = 2,
    [Display(Name = "Crew Change")]
    Crew = 3,
    [Display(Name = "Refueling Only")] 
    Refueling = 4
}