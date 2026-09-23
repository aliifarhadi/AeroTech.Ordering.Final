using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Query.Migrations
{
    /// <inheritdoc />
    public partial class AddSellingOffice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "OfficeId",
                schema: "ReadModel",
                table: "Orders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfficeKind",
                schema: "ReadModel",
                table: "Orders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OfficeKind_OfficeId",
                schema: "ReadModel",
                table: "Orders",
                columns: new[] { "OfficeKind", "OfficeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_OfficeKind_OfficeId",
                schema: "ReadModel",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OfficeId",
                schema: "ReadModel",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "OfficeKind",
                schema: "ReadModel",
                table: "Orders");
        }
    }
}
