using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RecordChangeReasonAndValidationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReasonText",
                schema: "Order",
                table: "OrderChanges",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ValidatedOrderServiceIds",
                schema: "Order",
                table: "FulfillmentReservations",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ValidationCommercialVersion",
                schema: "Order",
                table: "FulfillmentReservations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidationValidUntil",
                schema: "Order",
                table: "FulfillmentReservations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidationValidatedAt",
                schema: "Order",
                table: "FulfillmentReservations",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReasonText",
                schema: "Order",
                table: "OrderChanges");

            migrationBuilder.DropColumn(
                name: "ValidatedOrderServiceIds",
                schema: "Order",
                table: "FulfillmentReservations");

            migrationBuilder.DropColumn(
                name: "ValidationCommercialVersion",
                schema: "Order",
                table: "FulfillmentReservations");

            migrationBuilder.DropColumn(
                name: "ValidationValidUntil",
                schema: "Order",
                table: "FulfillmentReservations");

            migrationBuilder.DropColumn(
                name: "ValidationValidatedAt",
                schema: "Order",
                table: "FulfillmentReservations");
        }
    }
}
