using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnforceDocumentStockAllocationIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentStockAllocations_DocumentStockId_IssueFulfillmentTaskId_DocumentRole",
                schema: "Order",
                table: "DocumentStockAllocations");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStockAllocations_DocumentStockId_IssueFulfillmentTaskId_DocumentRole",
                schema: "Order",
                table: "DocumentStockAllocations",
                columns: new[] { "DocumentStockId", "IssueFulfillmentTaskId", "DocumentRole" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentStockAllocations_DocumentStockId_IssueFulfillmentTaskId_DocumentRole",
                schema: "Order",
                table: "DocumentStockAllocations");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentStockAllocations_DocumentStockId_IssueFulfillmentTaskId_DocumentRole",
                schema: "Order",
                table: "DocumentStockAllocations",
                columns: new[] { "DocumentStockId", "IssueFulfillmentTaskId", "DocumentRole" },
                unique: true,
                filter: "[State] <> 3");
        }
    }
}
