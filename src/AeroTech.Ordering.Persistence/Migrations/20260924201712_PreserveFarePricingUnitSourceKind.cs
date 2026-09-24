using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PreserveFarePricingUnitSourceKind : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SourceKind",
                schema: "Order",
                table: "OrderFarePricingUnits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [Order].[OrderFarePricingUnits]
                SET [SourceKind] = CASE [Kind]
                        WHEN 1 THEN N'OneWay'
                        WHEN 2 THEN N'RoundTripFare'
                        WHEN 3 THEN N'ThroughOneWay'
                        WHEN 4 THEN N'SectorSum'
                    END,
                    [Kind] = CASE [Kind]
                        WHEN 1 THEN 1
                        WHEN 2 THEN 2
                        WHEN 3 THEN 1
                        ELSE 0
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "SourceKind",
                schema: "Order",
                table: "OrderFarePricingUnits",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "Kind",
                schema: "Order",
                table: "OrderFarePricingUnits",
                newName: "SemanticType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE [Order].[OrderFarePricingUnits]
                SET [SemanticType] = CASE [SourceKind]
                        WHEN N'OneWay' THEN 1
                        WHEN N'RoundTripFare' THEN 2
                        WHEN N'ThroughOneWay' THEN 3
                        WHEN N'SectorSum' THEN 4
                        ELSE 0
                    END;
                """);

            migrationBuilder.DropColumn(
                name: "SourceKind",
                schema: "Order",
                table: "OrderFarePricingUnits");

            migrationBuilder.RenameColumn(
                name: "SemanticType",
                schema: "Order",
                table: "OrderFarePricingUnits",
                newName: "Kind");
        }
    }
}
