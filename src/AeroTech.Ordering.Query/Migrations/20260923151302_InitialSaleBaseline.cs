using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Query.Migrations
{
    /// <inheritdoc />
    public partial class InitialSaleBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ReadModel");

            migrationBuilder.CreateTable(
                name: "OrderContactPoints",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderContactId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CountryCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderContactPoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderContacts",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AcceptedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommercialStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderJourneys",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    BoundId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    OriginAirportId = table.Column<int>(type: "int", nullable: false),
                    DestinationAirportId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderJourneys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderPricingAllocations",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    PricingLineId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: true),
                    OrderServiceId = table.Column<long>(type: "bigint", nullable: true),
                    OrderJourneyId = table.Column<long>(type: "bigint", nullable: true),
                    OrderSegmentId = table.Column<long>(type: "bigint", nullable: true),
                    TravellerId = table.Column<long>(type: "bigint", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    EquivalentAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    EquivalentCurrencyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPricingAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderPricingLines",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
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
                    Refundability = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPricingLines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderRemarks",
                schema: "ReadModel",
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
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderRemarks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderReference = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceOfferId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    ActorId = table.Column<long>(type: "bigint", nullable: false),
                    TravelAgencyId = table.Column<long>(type: "bigint", nullable: true),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    CustomerTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommercialVersion = table.Column<int>(type: "int", nullable: false),
                    LastTicketingDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastProjectedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderSegments",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderJourneyId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    FlightId = table.Column<long>(type: "bigint", nullable: false),
                    FlightNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    MarketingAirlineId = table.Column<int>(type: "int", nullable: false),
                    OperatingAirlineId = table.Column<int>(type: "int", nullable: false),
                    OriginAirportId = table.Column<int>(type: "int", nullable: false),
                    DestinationAirportId = table.Column<int>(type: "int", nullable: false),
                    SoldDeparture = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SoldArrival = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Duration = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSegments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderServices",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderItemId = table.Column<long>(type: "bigint", nullable: false),
                    TravellerId = table.Column<long>(type: "bigint", nullable: false),
                    SegmentId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceType = table.Column<int>(type: "int", nullable: false),
                    CommercialStatus = table.Column<int>(type: "int", nullable: false),
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
                    IsUpgradable = table.Column<bool>(type: "bit", nullable: false),
                    SeatNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderTravellerDocuments",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    TravellerId = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IssuanceCountryId = table.Column<int>(type: "int", nullable: false),
                    Holder = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTravellerDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderTravellers",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    Index = table.Column<int>(type: "int", nullable: false),
                    SourceTravellerRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PassengerType = table.Column<int>(type: "int", nullable: false),
                    AgeRange = table.Column<int>(type: "int", nullable: false),
                    InfantParentTravellerId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    GivenName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Surname = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<int>(type: "int", nullable: true),
                    NationalityId = table.Column<int>(type: "int", nullable: true),
                    CountryOfResidenceId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderTravellers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPoints_OrderContactId",
                schema: "ReadModel",
                table: "OrderContactPoints",
                column: "OrderContactId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContactPoints_OrderId",
                schema: "ReadModel",
                table: "OrderContactPoints",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderContacts_OrderId_Sequence",
                schema: "ReadModel",
                table: "OrderContacts",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId_CommercialStatus",
                schema: "ReadModel",
                table: "OrderItems",
                columns: new[] { "OrderId", "CommercialStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderJourneys_OrderId_Sequence",
                schema: "ReadModel",
                table: "OrderJourneys",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderPricingAllocations_OrderId",
                schema: "ReadModel",
                table: "OrderPricingAllocations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPricingAllocations_PricingLineId",
                schema: "ReadModel",
                table: "OrderPricingAllocations",
                column: "PricingLineId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPricingAllocations_TravellerId",
                schema: "ReadModel",
                table: "OrderPricingAllocations",
                column: "TravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderPricingLines_OrderId_Treatment",
                schema: "ReadModel",
                table: "OrderPricingLines",
                columns: new[] { "OrderId", "Treatment" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRemarks_OrderId_Status",
                schema: "ReadModel",
                table: "OrderRemarks",
                columns: new[] { "OrderId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderRemarks_SupersedesRemarkId",
                schema: "ReadModel",
                table: "OrderRemarks",
                column: "SupersedesRemarkId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CreatedAt",
                schema: "ReadModel",
                table: "Orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                schema: "ReadModel",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderReference",
                schema: "ReadModel",
                table: "Orders",
                column: "OrderReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Status",
                schema: "ReadModel",
                table: "Orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegments_OrderId",
                schema: "ReadModel",
                table: "OrderSegments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegments_OrderJourneyId",
                schema: "ReadModel",
                table: "OrderSegments",
                column: "OrderJourneyId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegments_SoldDeparture",
                schema: "ReadModel",
                table: "OrderSegments",
                column: "SoldDeparture");

            migrationBuilder.CreateIndex(
                name: "IX_OrderServices_OrderId_CommercialStatus",
                schema: "ReadModel",
                table: "OrderServices",
                columns: new[] { "OrderId", "CommercialStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderServices_OrderItemId",
                schema: "ReadModel",
                table: "OrderServices",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderServices_SegmentId",
                schema: "ReadModel",
                table: "OrderServices",
                column: "SegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderServices_TravellerId",
                schema: "ReadModel",
                table: "OrderServices",
                column: "TravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTravellerDocuments_OrderId",
                schema: "ReadModel",
                table: "OrderTravellerDocuments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTravellerDocuments_TravellerId",
                schema: "ReadModel",
                table: "OrderTravellerDocuments",
                column: "TravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderTravellers_OrderId_Index",
                schema: "ReadModel",
                table: "OrderTravellers",
                columns: new[] { "OrderId", "Index" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderTravellers_Surname",
                schema: "ReadModel",
                table: "OrderTravellers",
                column: "Surname");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderContactPoints",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderContacts",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderItems",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderJourneys",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderPricingAllocations",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderPricingLines",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderRemarks",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "Orders",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderSegments",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderServices",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderTravellerDocuments",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "OrderTravellers",
                schema: "ReadModel");
        }
    }
}
