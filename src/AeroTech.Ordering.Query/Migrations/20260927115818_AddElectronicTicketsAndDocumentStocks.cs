using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Query.Migrations
{
    /// <inheritdoc />
    public partial class AddElectronicTicketsAndDocumentStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DocumentStocks",
                schema: "ReadModel",
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
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentStocks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ElectronicTickets",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    TravellerId = table.Column<long>(type: "bigint", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Authority = table.Column<int>(type: "int", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IssuedTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    StatusSummary = table.Column<int>(type: "int", nullable: false),
                    DocumentVersion = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ElectronicTickets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TicketCoupons",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ElectronicTicketId = table.Column<long>(type: "bigint", nullable: false),
                    CouponNumber = table.Column<int>(type: "int", nullable: false),
                    OriginalOrderServiceId = table.Column<long>(type: "bigint", nullable: false),
                    CurrentOrderServiceId = table.Column<long>(type: "bigint", nullable: false),
                    OrderSegmentId = table.Column<long>(type: "bigint", nullable: false),
                    FareBasisSnapshot = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BookingClassSnapshot = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    RbdIdSnapshot = table.Column<long>(type: "bigint", nullable: true),
                    CabinClassIdSnapshot = table.Column<long>(type: "bigint", nullable: true),
                    IssuanceValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrencyId = table.Column<int>(type: "int", nullable: false),
                    FinancialStatus = table.Column<int>(type: "int", nullable: false),
                    ControlStatus = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketCoupons", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStocks_OwnerAirlineId_DocumentKind",
                schema: "ReadModel",
                table: "DocumentStocks",
                columns: new[] { "OwnerAirlineId", "DocumentKind" });

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_DocumentNumber",
                schema: "ReadModel",
                table: "ElectronicTickets",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_OrderId",
                schema: "ReadModel",
                table: "ElectronicTickets",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCoupons_ElectronicTicketId",
                schema: "ReadModel",
                table: "TicketCoupons",
                column: "ElectronicTicketId");

            migrationBuilder.CreateIndex(
                name: "IX_TicketCoupons_OrderId",
                schema: "ReadModel",
                table: "TicketCoupons",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocumentStocks",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "ElectronicTickets",
                schema: "ReadModel");

            migrationBuilder.DropTable(
                name: "TicketCoupons",
                schema: "ReadModel");
        }
    }
}
