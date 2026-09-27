using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Query.Migrations
{
    /// <inheritdoc />
    public partial class AddCancellationAndVoidHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EndedByChangeId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "EndedByChangeId",
                schema: "ReadModel",
                table: "OrderItems",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VoidActorId",
                schema: "ReadModel",
                table: "ElectronicTickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoidDeadline",
                schema: "ReadModel",
                table: "ElectronicTickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "VoidFulfillmentTaskId",
                schema: "ReadModel",
                table: "ElectronicTickets",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidProviderReference",
                schema: "ReadModel",
                table: "ElectronicTickets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReasonCode",
                schema: "ReadModel",
                table: "ElectronicTickets",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VoidReasonText",
                schema: "ReadModel",
                table: "ElectronicTickets",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "VoidedAt",
                schema: "ReadModel",
                table: "ElectronicTickets",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderChanges",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    ChangeType = table.Column<int>(type: "int", nullable: false),
                    CommercialVersion = table.Column<int>(type: "int", nullable: false),
                    ActorId = table.Column<long>(type: "bigint", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    ContextType = table.Column<int>(type: "int", nullable: false),
                    PrincipalType = table.Column<int>(type: "int", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SourceSystem = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ReasonCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IsInvoluntary = table.Column<bool>(type: "bit", nullable: false),
                    WaiverCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CommittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderChanges", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderChanges_OrderId_CommercialVersion",
                schema: "ReadModel",
                table: "OrderChanges",
                columns: new[] { "OrderId", "CommercialVersion" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderChanges",
                schema: "ReadModel");

            migrationBuilder.DropColumn(
                name: "EndedByChangeId",
                schema: "ReadModel",
                table: "OrderServices");

            migrationBuilder.DropColumn(
                name: "EndedByChangeId",
                schema: "ReadModel",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "VoidActorId",
                schema: "ReadModel",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidDeadline",
                schema: "ReadModel",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidFulfillmentTaskId",
                schema: "ReadModel",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidProviderReference",
                schema: "ReadModel",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidReasonCode",
                schema: "ReadModel",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidReasonText",
                schema: "ReadModel",
                table: "ElectronicTickets");

            migrationBuilder.DropColumn(
                name: "VoidedAt",
                schema: "ReadModel",
                table: "ElectronicTickets");
        }
    }
}
