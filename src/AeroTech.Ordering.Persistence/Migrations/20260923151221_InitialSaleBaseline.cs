using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialSaleBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "dbo");

            migrationBuilder.EnsureSchema(
                name: "Order");

            migrationBuilder.CreateTable(
                name: "InboxMessages",
                schema: "dbo",
                columns: table => new
                {
                    MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Consumer = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    MessageType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ReceivedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InboxMessages", x => new { x.MessageId, x.Consumer });
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderReference = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    SalesChannel = table.Column<int>(type: "int", nullable: false),
                    SalesContextType = table.Column<int>(type: "int", nullable: false),
                    SalesPrincipalType = table.Column<int>(type: "int", nullable: false),
                    SalesActorId = table.Column<long>(type: "bigint", nullable: false),
                    SalesAirlineUserId = table.Column<long>(type: "bigint", nullable: true),
                    SalesTravelAgencyId = table.Column<long>(type: "bigint", nullable: true),
                    SalesTravelAgencyUserId = table.Column<long>(type: "bigint", nullable: true),
                    SalesIndividualId = table.Column<long>(type: "bigint", nullable: true),
                    SalesPartnerApiAccessProfileId = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    SourceOfferId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    LastTicketingDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CommercialVersion = table.Column<int>(type: "int", nullable: false),
                    CustomerTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                schema: "dbo",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MessageType = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Payload = table.Column<string>(type: "nvarchar(max)", maxLength: 256, nullable: false),
                    OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProcessedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderChanges",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    CommercialVersion = table.Column<int>(type: "int", nullable: false),
                    ActorChannel = table.Column<int>(type: "int", nullable: false),
                    ActorContextType = table.Column<int>(type: "int", nullable: false),
                    ActorPrincipalType = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<long>(type: "bigint", nullable: false),
                    ActorAirlineUserId = table.Column<long>(type: "bigint", nullable: true),
                    ActorTravelAgencyId = table.Column<long>(type: "bigint", nullable: true),
                    ActorTravelAgencyUserId = table.Column<long>(type: "bigint", nullable: true),
                    ActorIndividualId = table.Column<long>(type: "bigint", nullable: true),
                    ActorPartnerApiAccessProfileId = table.Column<long>(type: "bigint", nullable: true),
                    SourceReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    CommittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderChanges_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderContacts",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderContacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderContacts_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AcceptedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommercialStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedByChangeId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderJourneys",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    BoundId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginAirportId = table.Column<int>(type: "int", nullable: false),
                    DestinationAirportId = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderJourneys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderJourneys_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderRemarks",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Visibility = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    TravellerId = table.Column<long>(type: "bigint", nullable: true),
                    SegmentId = table.Column<long>(type: "bigint", nullable: true),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: true),
                    OrderServiceId = table.Column<long>(type: "bigint", nullable: true),
                    Text = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CategoryCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    IsPrintedOnItinerary = table.Column<bool>(type: "bit", nullable: false),
                    IsPrintedOnInvoice = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SupersedesRemarkId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRemarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderRemarks_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderSegments",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderJourneyId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    FlightId = table.Column<long>(type: "bigint", nullable: false),
                    FlightVersion = table.Column<int>(type: "int", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    OriginAirportId = table.Column<int>(type: "int", nullable: false),
                    OriginAirportTerminalId = table.Column<int>(type: "int", nullable: true),
                    DestinationAirportId = table.Column<int>(type: "int", nullable: false),
                    DestinationAirportTerminalId = table.Column<int>(type: "int", nullable: true),
                    OperatingAirlineId = table.Column<int>(type: "int", nullable: false),
                    MarketingAirlineId = table.Column<int>(type: "int", nullable: false),
                    SoldDeparture = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SoldArrival = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false),
                    AircraftId = table.Column<int>(type: "int", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSegments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderSegments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    TravellerId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    CommercialStatus = table.Column<int>(type: "int", nullable: false),
                    CreatedByChangeId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderServices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderTravellers",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    SourceTravellerRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PassengerType = table.Column<int>(type: "int", nullable: false),
                    AgeRange = table.Column<int>(type: "int", nullable: false),
                    InfantParentTravellerId = table.Column<long>(type: "bigint", nullable: true),
                    CurrentProfileRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTravellers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTravellers_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingLines",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByChangeId = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Scope = table.Column<int>(type: "int", nullable: false),
                    Category = table.Column<int>(type: "int", nullable: false),
                    SubCategory = table.Column<int>(type: "int", nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    Treatment = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    EquivalentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EquivalentCurrencyId = table.Column<int>(type: "int", nullable: false),
                    ExchangeFromCurrencyId = table.Column<int>(type: "int", nullable: true),
                    ExchangeToCurrencyId = table.Column<int>(type: "int", nullable: true),
                    ExchangeRate = table.Column<decimal>(type: "decimal(19,9)", precision: 19, scale: 9, nullable: true),
                    ExchangeDecimalPlaces = table.Column<int>(type: "int", nullable: true),
                    ExchangePeriodId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Refundability = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingLines_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderContactPoints",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderContactId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderContactPoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderContactPoints_OrderContacts_OrderContactId",
                        column: x => x.OrderContactId,
                        principalSchema: "Order",
                        principalTable: "OrderContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderSegmentLegs",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderSegmentId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    LegId = table.Column<long>(type: "bigint", nullable: false),
                    OriginAirportId = table.Column<int>(type: "int", nullable: false),
                    OriginAirportTerminalId = table.Column<int>(type: "int", nullable: true),
                    DestinationAirportId = table.Column<int>(type: "int", nullable: false),
                    DestinationAirportTerminalId = table.Column<int>(type: "int", nullable: true),
                    DepartureDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArrivalDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    StopType = table.Column<int>(type: "int", nullable: true),
                    StopDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    StopPassengersCanBoardOrLeave = table.Column<bool>(type: "bit", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSegmentLegs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderSegmentLegs_OrderSegments_OrderSegmentId",
                        column: x => x.OrderSegmentId,
                        principalSchema: "Order",
                        principalTable: "OrderSegments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderAirTransportServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    SegmentId = table.Column<long>(type: "bigint", nullable: false),
                    FlightCapacityId = table.Column<long>(type: "bigint", nullable: false),
                    AirFareId = table.Column<long>(type: "bigint", nullable: true),
                    BookingClass = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    FareBasis = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FareFamily = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FareType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CheckedBaggagePieces = table.Column<int>(type: "int", nullable: true),
                    CheckedBaggageWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CheckedBaggageUnit = table.Column<int>(type: "int", nullable: true),
                    CabinBaggagePieces = table.Column<int>(type: "int", nullable: true),
                    CabinBaggageWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CabinBaggageUnit = table.Column<int>(type: "int", nullable: true),
                    IsRefundable = table.Column<bool>(type: "bit", nullable: false),
                    IsChangeable = table.Column<bool>(type: "bit", nullable: false),
                    IsUpgradable = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAirTransportServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAirTransportServices_OrderServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderSeatServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    SegmentId = table.Column<long>(type: "bigint", nullable: false),
                    AssociatedAirServiceId = table.Column<long>(type: "bigint", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSeatServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderSeatServices_OrderServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderTravellerDocuments",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderTravellerId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IssuanceCountryId = table.Column<int>(type: "int", nullable: false),
                    Holder = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTravellerDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderTravellerDocuments_OrderTravellers_OrderTravellerId",
                        column: x => x.OrderTravellerId,
                        principalSchema: "Order",
                        principalTable: "OrderTravellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TravellerProfileRevisions",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TravellerId = table.Column<long>(type: "bigint", nullable: false),
                    GivenName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    NoSurname = table.Column<bool>(type: "bit", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    NationalityId = table.Column<int>(type: "int", nullable: true),
                    CountryOfResidenceId = table.Column<int>(type: "int", nullable: true),
                    CreatedByChangeId = table.Column<long>(type: "bigint", nullable: false),
                    SupersededByChangeId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravellerProfileRevisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TravellerProfileRevisions_OrderTravellers_TravellerId",
                        column: x => x.TravellerId,
                        principalSchema: "Order",
                        principalTable: "OrderTravellers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PricingAllocations",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PricingLineId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: true),
                    OrderServiceId = table.Column<long>(type: "bigint", nullable: true),
                    OrderJourneyId = table.Column<long>(type: "bigint", nullable: true),
                    OrderSegmentId = table.Column<long>(type: "bigint", nullable: true),
                    TravellerId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    EquivalentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EquivalentCurrencyId = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PricingAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PricingAllocations_PricingLines_PricingLineId",
                        column: x => x.PricingLineId,
                        principalSchema: "Order",
                        principalTable: "PricingLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InboxMessages_ReceivedOn",
                schema: "dbo",
                table: "InboxMessages",
                column: "ReceivedOn");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAirTransportServices_SegmentId",
                schema: "Order",
                table: "OrderAirTransportServices",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderChanges_OrderId_CommercialVersion",
                schema: "Order",
                table: "OrderChanges",
                columns: new[] { "OrderId", "CommercialVersion" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPoints_OrderContactId",
                schema: "Order",
                table: "OrderContactPoints",
                column: "OrderContactId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContacts_OrderId_Sequence",
                schema: "Order",
                table: "OrderContacts",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_CreatedByChangeId",
                schema: "Order",
                table: "OrderItems",
                column: "CreatedByChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_CommercialStatus",
                schema: "Order",
                table: "OrderItems",
                columns: new[] { "OrderId", "CommercialStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderJourneys_OrderId_Sequence",
                schema: "Order",
                table: "OrderJourneys",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderRemarks_OrderId_Status",
                schema: "Order",
                table: "OrderRemarks",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRemarks_SupersedesRemarkId",
                schema: "Order",
                table: "OrderRemarks",
                column: "SupersedesRemarkId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "Order",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_LastTicketingDate",
                schema: "Order",
                table: "Orders",
                column: "LastTicketingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderReference",
                schema: "Order",
                table: "Orders",
                column: "OrderReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "Order",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSeatServices_AssociatedAirServiceId",
                schema: "Order",
                table: "OrderSeatServices",
                column: "AssociatedAirServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSeatServices_SegmentId",
                schema: "Order",
                table: "OrderSeatServices",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegmentLegs_OrderSegmentId_Sequence",
                schema: "Order",
                table: "OrderSegmentLegs",
                columns: new[] { "OrderSegmentId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegments_FlightId",
                schema: "Order",
                table: "OrderSegments",
                column: "FlightId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegments_OrderId",
                schema: "Order",
                table: "OrderSegments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegments_OrderJourneyId",
                schema: "Order",
                table: "OrderSegments",
                column: "OrderJourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderServices_OrderId_CommercialStatus",
                schema: "Order",
                table: "OrderServices",
                columns: new[] { "OrderId", "CommercialStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderServices_OrderItemId",
                schema: "Order",
                table: "OrderServices",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderServices_TravellerId",
                schema: "Order",
                table: "OrderServices",
                column: "TravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTravellerDocuments_OrderTravellerId",
                schema: "Order",
                table: "OrderTravellerDocuments",
                column: "OrderTravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTravellers_InfantParentTravellerId",
                schema: "Order",
                table: "OrderTravellers",
                column: "InfantParentTravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTravellers_OrderId_Index",
                schema: "Order",
                table: "OrderTravellers",
                columns: new[] { "OrderId", "Index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProcessedOn",
                schema: "dbo",
                table: "OutboxMessages",
                column: "ProcessedOn");

            migrationBuilder.CreateIndex(
                name: "IX_PricingAllocations_OrderItemId",
                schema: "Order",
                table: "PricingAllocations",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingAllocations_OrderServiceId",
                schema: "Order",
                table: "PricingAllocations",
                column: "OrderServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingAllocations_PricingLineId",
                schema: "Order",
                table: "PricingAllocations",
                column: "PricingLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingAllocations_TravellerId",
                schema: "Order",
                table: "PricingAllocations",
                column: "TravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingLines_CreatedByChangeId",
                schema: "Order",
                table: "PricingLines",
                column: "CreatedByChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_PricingLines_OrderId_Treatment",
                schema: "Order",
                table: "PricingLines",
                columns: new[] { "OrderId", "Treatment" });

            migrationBuilder.CreateIndex(
                name: "IX_TravellerProfileRevisions_CreatedByChangeId",
                schema: "Order",
                table: "TravellerProfileRevisions",
                column: "CreatedByChangeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravellerProfileRevisions_TravellerId",
                schema: "Order",
                table: "TravellerProfileRevisions",
                column: "TravellerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InboxMessages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "OrderAirTransportServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderChanges",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderContactPoints",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderJourneys",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderRemarks",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderSeatServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderSegmentLegs",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderTravellerDocuments",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OutboxMessages",
                schema: "dbo");

            migrationBuilder.DropTable(
                name: "PricingAllocations",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "TravellerProfileRevisions",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderContacts",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderSegments",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "PricingLines",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderTravellers",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "Order");
        }
    }
}
