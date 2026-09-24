using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFarePricingUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderFarePricingUnits",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByChangeId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    CoveredJourneyIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFarePricingUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderFarePricingUnits_Orders_OrderId",
                        column: x => x.OrderId,
                        principalSchema: "Order",
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderFareComponents",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderFarePricingUnitId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    AirFareId = table.Column<long>(type: "bigint", nullable: false),
                    BookingClass = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    FareBasis = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FareFamily = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    FareType = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CoveredOrderServiceIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderFareComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderFareComponents_OrderFarePricingUnits_OrderFarePricingUnitId",
                        column: x => x.OrderFarePricingUnitId,
                        principalSchema: "Order",
                        principalTable: "OrderFarePricingUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderFareComponents_OrderFarePricingUnitId_Sequence",
                schema: "Order",
                table: "OrderFareComponents",
                columns: new[] { "OrderFarePricingUnitId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderFarePricingUnits_OrderId_Sequence",
                schema: "Order",
                table: "OrderFarePricingUnits",
                columns: new[] { "OrderId", "Sequence" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderFareComponents",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderFarePricingUnits",
                schema: "Order");
        }
    }
}
