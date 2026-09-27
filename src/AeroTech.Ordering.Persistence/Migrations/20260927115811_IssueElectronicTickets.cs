using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class IssueElectronicTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FulfillmentTaskTargets_ReservationUnitId",
                schema: "Order",
                table: "FulfillmentTaskTargets");

            migrationBuilder.RenameColumn(
                name: "ReservationUnitId",
                schema: "Order",
                table: "FulfillmentTaskTargets",
                newName: "TargetId");

            migrationBuilder.AddColumn<int>(
                name: "TargetKind",
                schema: "Order",
                table: "FulfillmentTaskTargets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("UPDATE [Order].[FulfillmentTaskTargets] SET [TargetKind] = 1;");

            migrationBuilder.AlterColumn<long>(
                name: "FulfillmentReservationId",
                schema: "Order",
                table: "FulfillmentTasks",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "DocumentStocks",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OwnerAirlineId = table.Column<int>(type: "int", nullable: false),
                    OfficeId = table.Column<long>(type: "bigint", nullable: true),
                    DocumentKind = table.Column<int>(type: "int", nullable: false),
                    Prefix = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SerialWidth = table.Column<int>(type: "int", nullable: false),
                    CheckDigitProfile = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RangeFrom = table.Column<long>(type: "bigint", nullable: false),
                    RangeTo = table.Column<long>(type: "bigint", nullable: false),
                    NextNumber = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectronicTickets",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OriginalOrderId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentServicingOrderId = table.Column<long>(type: "bigint", nullable: false),
                    TravellerId = table.Column<long>(type: "bigint", nullable: false),
                    TravellerProfileRevisionId = table.Column<long>(type: "bigint", nullable: false),
                    IssueFulfillmentTaskId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IssuerCarrierId = table.Column<int>(type: "int", nullable: false),
                    ValidatingCarrierId = table.Column<int>(type: "int", nullable: true),
                    IssuingOfficeId = table.Column<long>(type: "bigint", nullable: true),
                    IssuedByActorId = table.Column<long>(type: "bigint", nullable: true),
                    TravelAgencyId = table.Column<long>(type: "bigint", nullable: true),
                    AgencyIataNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Pcc = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    SalesChannel = table.Column<int>(type: "int", nullable: true),
                    SourceFormOfPaymentCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Authority = table.Column<int>(type: "int", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    VoidDeadline = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IssuedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    ProviderReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    StatusSummary = table.Column<int>(type: "int", nullable: false),
                    DocumentVersion = table.Column<int>(type: "int", nullable: false),
                    PredecessorElectronicTicketId = table.Column<long>(type: "bigint", nullable: true),
                    PredecessorExchangeChangeId = table.Column<long>(type: "bigint", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DocumentStockAllocations",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DocumentStockId = table.Column<long>(type: "bigint", nullable: false),
                    IssueFulfillmentTaskId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentRole = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Serial = table.Column<long>(type: "bigint", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    AllocatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SettledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStockAllocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DocumentStockAllocations_DocumentStocks_DocumentStockId",
                        column: x => x.DocumentStockId,
                        principalSchema: "Order",
                        principalTable: "DocumentStocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketCoupons",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TicketId = table.Column<long>(type: "bigint", nullable: false),
                    CouponNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalOrderServiceId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentOrderServiceId = table.Column<long>(type: "bigint", nullable: false),
                    OrderSegmentId = table.Column<long>(type: "bigint", nullable: false),
                    OrderFareComponentId = table.Column<long>(type: "bigint", nullable: true),
                    PredecessorTicketCouponId = table.Column<long>(type: "bigint", nullable: true),
                    IssuedMarketingAirlineId = table.Column<int>(type: "int", nullable: false),
                    IssuedOperatingAirlineId = table.Column<int>(type: "int", nullable: false),
                    IssuedFlightNumber = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    IssuedOriginAirportId = table.Column<int>(type: "int", nullable: false),
                    IssuedDestinationAirportId = table.Column<int>(type: "int", nullable: false),
                    IssuedDepartureDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IssuedArrivalDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IssuedBookingClass = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    IssuedRbdId = table.Column<long>(type: "bigint", nullable: true),
                    IssuedCabinClassId = table.Column<long>(type: "bigint", nullable: true),
                    IssuedSourceSegmentReference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FareBasisSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BookingClassSnapshot = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    RbdIdSnapshot = table.Column<long>(type: "bigint", nullable: true),
                    CabinClassIdSnapshot = table.Column<long>(type: "bigint", nullable: true),
                    BaggageAllowancePieces = table.Column<int>(type: "int", nullable: true),
                    BaggageAllowanceWeight = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageAllowanceUnit = table.Column<int>(type: "int", nullable: true),
                    IssuanceValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    FinancialStatus = table.Column<int>(type: "int", nullable: false),
                    ControlStatus = table.Column<int>(type: "int", nullable: false),
                    ProviderCouponStatusCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    NotValidBefore = table.Column<DateOnly>(type: "date", nullable: true),
                    NotValidAfter = table.Column<DateOnly>(type: "date", nullable: true),
                    UsedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UsageReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCoupons", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketCoupons_ElectronicTickets_TicketId",
                        column: x => x.TicketId,
                        principalSchema: "Order",
                        principalTable: "ElectronicTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TicketPriceLinks",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ElectronicTicketId = table.Column<long>(type: "bigint", nullable: false),
                    TicketCouponId = table.Column<long>(type: "bigint", nullable: true),
                    PricingLineId = table.Column<long>(type: "bigint", nullable: false),
                    PricingAllocationId = table.Column<long>(type: "bigint", nullable: true),
                    AttributedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketPriceLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketPriceLinks_ElectronicTickets_ElectronicTicketId",
                        column: x => x.ElectronicTicketId,
                        principalSchema: "Order",
                        principalTable: "ElectronicTickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentTaskTargets_TargetKind_TargetId",
                schema: "Order",
                table: "FulfillmentTaskTargets",
                columns: new[] { "TargetKind", "TargetId" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStockAllocations_DocumentNumber",
                schema: "Order",
                table: "DocumentStockAllocations",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStockAllocations_DocumentStockId_IssueFulfillmentTaskId_DocumentRole",
                schema: "Order",
                table: "DocumentStockAllocations",
                columns: new[] { "DocumentStockId", "IssueFulfillmentTaskId", "DocumentRole" },
                unique: true,
                filter: "[State] <> 3");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStockAllocations_IssueFulfillmentTaskId",
                schema: "Order",
                table: "DocumentStockAllocations",
                column: "IssueFulfillmentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStocks_OwnerAirlineId_DocumentKind_Prefix",
                schema: "Order",
                table: "DocumentStocks",
                columns: new[] { "OwnerAirlineId", "DocumentKind", "Prefix" });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_CurrentServicingOrderId",
                schema: "Order",
                table: "ElectronicTickets",
                column: "CurrentServicingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_DocumentNumber",
                schema: "Order",
                table: "ElectronicTickets",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_IssueFulfillmentTaskId",
                schema: "Order",
                table: "ElectronicTickets",
                column: "IssueFulfillmentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_OriginalOrderId",
                schema: "Order",
                table: "ElectronicTickets",
                column: "OriginalOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_TravellerId",
                schema: "Order",
                table: "ElectronicTickets",
                column: "TravellerId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCoupons_CurrentOrderServiceId",
                schema: "Order",
                table: "TicketCoupons",
                column: "CurrentOrderServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCoupons_OrderSegmentId",
                schema: "Order",
                table: "TicketCoupons",
                column: "OrderSegmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCoupons_TicketId_CouponNumber",
                schema: "Order",
                table: "TicketCoupons",
                columns: new[] { "TicketId", "CouponNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriceLinks_ElectronicTicketId",
                schema: "Order",
                table: "TicketPriceLinks",
                column: "ElectronicTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriceLinks_PricingAllocationId",
                schema: "Order",
                table: "TicketPriceLinks",
                column: "PricingAllocationId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriceLinks_PricingLineId",
                schema: "Order",
                table: "TicketPriceLinks",
                column: "PricingLineId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketPriceLinks_TicketCouponId",
                schema: "Order",
                table: "TicketPriceLinks",
                column: "TicketCouponId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentStockAllocations",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "TicketCoupons",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "TicketPriceLinks",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "DocumentStocks",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "ElectronicTickets",
                schema: "Order");

            migrationBuilder.DropIndex(
                name: "IX_FulfillmentTaskTargets_TargetKind_TargetId",
                schema: "Order",
                table: "FulfillmentTaskTargets");

            migrationBuilder.DropColumn(
                name: "TargetKind",
                schema: "Order",
                table: "FulfillmentTaskTargets");

            migrationBuilder.RenameColumn(
                name: "TargetId",
                schema: "Order",
                table: "FulfillmentTaskTargets",
                newName: "ReservationUnitId");

            migrationBuilder.AlterColumn<long>(
                name: "FulfillmentReservationId",
                schema: "Order",
                table: "FulfillmentTasks",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentTaskTargets_ReservationUnitId",
                schema: "Order",
                table: "FulfillmentTaskTargets",
                column: "ReservationUnitId");
        }
    }
}
