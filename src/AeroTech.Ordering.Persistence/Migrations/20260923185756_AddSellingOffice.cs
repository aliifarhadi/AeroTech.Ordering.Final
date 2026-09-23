using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSellingOffice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SalesOfficeId",
                schema: "Order",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalesOfficeKind",
                schema: "Order",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ActorOfficeId",
                schema: "Order",
                table: "OrderChanges",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ActorOfficeKind",
                schema: "Order",
                table: "OrderChanges",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SalesOfficeId",
                schema: "Order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SalesOfficeKind",
                schema: "Order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ActorOfficeId",
                schema: "Order",
                table: "OrderChanges");

            migrationBuilder.DropColumn(
                name: "ActorOfficeKind",
                schema: "Order",
                table: "OrderChanges");
        }
    }
}
