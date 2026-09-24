using AeroTech.Framework.Core.Domain.Queries;
using AeroTech.Ordering.Query._Shared.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Dto
{
    public sealed class OrderPaginatedRowDto
    {
        public string Id { get; set; } = null!;

        [Grid("PNR")] public string? RecordLocator { get; set; }

        [Grid("Reference")] public string OrderReference { get; set; } = null!;

        [Grid("Created")] public string CreatedAt { get; set; } = null!;

        [Grid("Channel")] public EnumValueDto Channel { get; set; } = null!;

        [Grid("Office")] public SellingOfficeDto? SellingOffice { get; set; }

        [Grid("Customer")] public string? Customer { get; set; }
        public string? CustomerNumber { get; set; }
        public long CustomerId { get; set; }

        [Grid("Passengers")] public int Passengers { get; set; }
        public PassengerSummaryDto PassengerSummary { get; set; } = new();

        [Grid("Status")] public EnumValueDto Status { get; set; } = null!;

        [Grid("Amount")] public string CustomerTotal { get; set; } = null!;

        [Grid("Currency")] public string? Currency { get; set; }

        [Grid("Ver.")] public int CommercialVersion { get; set; }

        [Grid("Ticketing")] public DateTimeOffset? LastTicketingDate { get; set; }

        public string SourceOfferId { get; set; } = null!;
        public FlightSummaryDto FlightSummary { get; set; } = new();
    }

    public sealed class PassengerSummaryDto
    {
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Infants { get; set; }
        public int Total { get; set; }
        public IReadOnlyList<string> Initials { get; set; } = Array.Empty<string>();
    }

    public sealed class FlightSummaryDto
    {
        public string? FlightNumber { get; set; }
        public DateTimeOffset? DepartureDateTime { get; set; }
        public int Stops { get; set; }
        public IReadOnlyList<string> AirportIataCode { get; set; } = Array.Empty<string>();
    }
}
