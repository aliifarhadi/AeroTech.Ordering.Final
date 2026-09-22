using AeroTech.Messages.FlightFlow.Enums;

namespace AeroTech.Messages.FlightFlow.IntegrationEvents;

public class FlightUpdated
{
    public long? Id { get; set; }
    public string? FlightNumber { get; set; }
    public int? OriginAirportId { get; set; }
    public int? OriginAirportTerminalId { get; set; }
    public int? DestinationAirportId { get; set; }
    public int? DestinationAirportTerminalId { get; set; }
    public int? OperatingAirlineId { get; set; }
    public int? MarketingAirlineId { get; set; }
    public DateTimeOffset? DepartureDateTime { get; set; }
    public DateTimeOffset? ArrivalDateTime { get; set; }
    public int? Duration { get; set; }
    public int? AircraftId { get; set; }
    public FlightStatus? Status { get; set; }
    public int? Version { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public bool? IsDirect { get; set; }
    public int? Stops { get; set; }
    public int? StopBookCutoffMinutes { get; set; }
    public long? DisruptionId { get; set; }
}
