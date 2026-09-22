using System.ComponentModel.DataAnnotations;

namespace AeroTech.Messages.FlightFlow.Enums;

public enum FlightDisruptionReason
{
    [Display(Name = "Weather Related")] WeatherRelated = 1,
    [Display(Name = "Technical Failures")] TechnicalFailures,
    [Display(Name = "Air Traffic Control Restrictions")] AirTrafficControlRestrictions,
    [Display(Name = "Crew Availability")] CrewAvailability,
    [Display(Name = "Airport Issues")] AirportIssues,
    [Display(Name = "Security Incidents")] SecurityIncidents,
    [Display(Name = "Strike/Industrial Action")] StrikeIndustrialAction,
    [Display(Name = "IT Outages/System Failures")] ITOutageSystemFailures,
    [Display(Name = "Ground Handling/Baggage System Failures")] GroundHandlingBaggageSystemFailures,
    [Display(Name = "Operational Scheduling Changes")] OperationalSchedulingChanges,
    [Display(Name = "Diversions")] Diversions,
    [Display(Name = "Interline/Third Party Failures")] InterlineThirdPartyFailures,
    [Display(Name = "Others")] Others
}
