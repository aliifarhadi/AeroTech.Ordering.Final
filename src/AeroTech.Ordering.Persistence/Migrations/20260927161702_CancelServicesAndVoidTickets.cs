using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CancelServicesAndVoidTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EndedByChangeId",
                schema: "Order",
                table: "OrderServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EndedByChangeId",
                schema: "Order",
                table: "OrderItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsInvoluntary",
                schema: "Order",
                table: "OrderChanges",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ReasonCode",
                schema: "Order",
                table: "OrderChanges",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSystem",
                schema: "Order",
                table: "OrderChanges",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WaiverCode",
                schema: "Order",
                table: "OrderChanges",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VoidActorId",
                schema: "Order",
                table: "ElectronicTickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VoidFulfillmentTaskId",
                schema: "Order",
                table: "ElectronicTickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidProviderReference",
                schema: "Order",
                table: "ElectronicTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReasonCode",
                schema: "Order",
                table: "ElectronicTickets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReasonText",
                schema: "Order",
                table: "ElectronicTickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoidedAt",
                schema: "Order",
                table: "ElectronicTickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ElectronicTickets_VoidFulfillmentTaskId",
                schema: "Order",
                table: "ElectronicTickets",
                column: "VoidFulfillmentTaskId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ElectronicTickets_VoidFulfillmentTaskId",
                schema: "Order",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "EndedByChangeId",
                schema: "Order",
                table: "OrderServices");

            migrationBuilder.DropColumn(
                name: "EndedByChangeId",
                schema: "Order",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsInvoluntary",
                schema: "Order",
                table: "OrderChanges");

            migrationBuilder.DropColumn(
                name: "ReasonCode",
                schema: "Order",
                table: "OrderChanges");

            migrationBuilder.DropColumn(
                name: "SourceSystem",
                schema: "Order",
                table: "OrderChanges");

            migrationBuilder.DropColumn(
                name: "WaiverCode",
                schema: "Order",
                table: "OrderChanges");

            migrationBuilder.DropColumn(
                name: "VoidActorId",
                schema: "Order",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidFulfillmentTaskId",
                schema: "Order",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidProviderReference",
                schema: "Order",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidReasonCode",
                schema: "Order",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidReasonText",
                schema: "Order",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                schema: "Order",
                table: "ElectronicTickets");
        }
    }
}
