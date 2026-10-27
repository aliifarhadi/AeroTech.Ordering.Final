using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Query.Migrations
{
    /// <inheritdoc />
    public partial class ProjectAncillaryContextFacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AirFareId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CabinClassId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FlightCapacityId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RbdId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AircraftId",
                schema: "ReadModel",
                table: "OrderSegments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DestinationAirportTerminalId",
                schema: "ReadModel",
                table: "OrderSegments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FlightVersion",
                schema: "ReadModel",
                table: "OrderSegments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginAirportTerminalId",
                schema: "ReadModel",
                table: "OrderSegments",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderSegmentLegs",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    OrderSegmentId = table.Column<long>(type: "bigint", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    LegId = table.Column<long>(type: "bigint", nullable: false),
                    OriginAirportId = table.Column<int>(type: "int", nullable: false),
                    DestinationAirportId = table.Column<int>(type: "int", nullable: false),
                    DepartureDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ArrivalDateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderSegmentLegs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegmentLegs_OrderId",
                schema: "ReadModel",
                table: "OrderSegmentLegs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderSegmentLegs_OrderSegmentId",
                schema: "ReadModel",
                table: "OrderSegmentLegs",
                column: "OrderSegmentId");

            migrationBuilder.Sql(
                """
                UPDATE readSegment
                SET [FlightVersion] = commandSegment.[FlightVersion],
                    [OriginAirportTerminalId] = commandSegment.[OriginAirportTerminalId],
                    [DestinationAirportTerminalId] = commandSegment.[DestinationAirportTerminalId],
                    [AircraftId] = commandSegment.[AircraftId]
                FROM [ReadModel].[OrderSegments] AS readSegment
                INNER JOIN [Order].[OrderSegments] AS commandSegment ON commandSegment.[Id] = readSegment.[Id];

                UPDATE readService
                SET [FlightCapacityId] = commandService.[FlightCapacityId],
                    [AirFareId] = commandService.[AirFareId],
                    [RbdId] = commandService.[RbdId],
                    [CabinClassId] = commandService.[CabinClassId]
                FROM [ReadModel].[OrderServices] AS readService
                INNER JOIN [Order].[OrderAirTransportServices] AS commandService ON commandService.[Id] = readService.[Id];

                INSERT INTO [ReadModel].[OrderSegmentLegs]
                    ([Id], [OrderId], [OrderSegmentId], [Sequence], [LegId], [OriginAirportId], [DestinationAirportId], [DepartureDateTime], [ArrivalDateTime])
                SELECT leg.[Id], readSegment.[OrderId], leg.[OrderSegmentId], leg.[Sequence], leg.[LegId], leg.[OriginAirportId], leg.[DestinationAirportId], leg.[DepartureDateTime], leg.[ArrivalDateTime]
                FROM [Order].[OrderSegmentLegs] AS leg
                INNER JOIN [ReadModel].[OrderSegments] AS readSegment ON readSegment.[Id] = leg.[OrderSegmentId];
                """);

            migrationBuilder.AlterColumn<int>(
                name: "FlightVersion",
                schema: "ReadModel",
                table: "OrderSegments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderSegmentLegs",
                schema: "ReadModel");

            migrationBuilder.DropColumn(
                name: "AirFareId",
                schema: "ReadModel",
                table: "OrderServices");

            migrationBuilder.DropColumn(
                name: "CabinClassId",
                schema: "ReadModel",
                table: "OrderServices");

            migrationBuilder.DropColumn(
                name: "FlightCapacityId",
                schema: "ReadModel",
                table: "OrderServices");

            migrationBuilder.DropColumn(
                name: "RbdId",
                schema: "ReadModel",
                table: "OrderServices");

            migrationBuilder.DropColumn(
                name: "AircraftId",
                schema: "ReadModel",
                table: "OrderSegments");

            migrationBuilder.DropColumn(
                name: "DestinationAirportTerminalId",
                schema: "ReadModel",
                table: "OrderSegments");

            migrationBuilder.DropColumn(
                name: "FlightVersion",
                schema: "ReadModel",
                table: "OrderSegments");

            migrationBuilder.DropColumn(
                name: "OriginAirportTerminalId",
                schema: "ReadModel",
                table: "OrderSegments");
        }
    }
}
