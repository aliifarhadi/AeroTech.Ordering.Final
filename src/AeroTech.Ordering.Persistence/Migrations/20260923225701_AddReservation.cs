using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FulfillmentProviderKey",
                schema: "Order",
                table: "OrderServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.Sql("UPDATE [Order].[OrderServices] SET [FulfillmentProviderKey] = N'FlightFlow';");

            migrationBuilder.AlterColumn<string>(
                name: "FulfillmentProviderKey",
                schema: "Order",
                table: "OrderServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecordLocator",
                schema: "Order",
                table: "Orders",
                type: "nvarchar(6)",
                maxLength: 6,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FulfillmentReservations",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentProviderKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderOperationRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RequestedExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastObservedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentReservations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FulfillmentTasks",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentReservationId = table.Column<long>(type: "bigint", nullable: false),
                    TaskType = table.Column<int>(type: "int", nullable: false),
                    FulfillmentProviderKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CorrelationReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LastFailureKind = table.Column<int>(type: "int", nullable: true),
                    LastFailureReason = table.Column<int>(type: "int", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentTasks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReservationUnits",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentReservationId = table.Column<long>(type: "bigint", nullable: false),
                    UnitCorrelationKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ProviderUnitRef = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    RawStatusCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    ObservedSeat = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    OrderServiceIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReservationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReservationUnits_FulfillmentReservations_FulfillmentReservationId",
                        column: x => x.FulfillmentReservationId,
                        principalSchema: "Order",
                        principalTable: "FulfillmentReservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FulfillmentTaskAttempts",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentTaskId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: true),
                    FailureKind = table.Column<int>(type: "int", nullable: true),
                    FailureReason = table.Column<int>(type: "int", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentTaskAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FulfillmentTaskAttempts_FulfillmentTasks_FulfillmentTaskId",
                        column: x => x.FulfillmentTaskId,
                        principalSchema: "Order",
                        principalTable: "FulfillmentTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FulfillmentTaskTargets",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentTaskId = table.Column<long>(type: "bigint", nullable: false),
                    ReservationUnitId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<int>(type: "int", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FulfillmentTaskTargets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FulfillmentTaskTargets_FulfillmentTasks_FulfillmentTaskId",
                        column: x => x.FulfillmentTaskId,
                        principalSchema: "Order",
                        principalTable: "FulfillmentTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProviderInteractions",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FulfillmentTaskId = table.Column<long>(type: "bigint", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false),
                    InteractionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ProviderStatusCode = table.Column<int>(type: "int", nullable: true),
                    Error = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LastUpdatedBy = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProviderInteractions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProviderInteractions_FulfillmentTasks_FulfillmentTaskId",
                        column: x => x.FulfillmentTaskId,
                        principalSchema: "Order",
                        principalTable: "FulfillmentTasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RecordLocator",
                schema: "Order",
                table: "Orders",
                column: "RecordLocator",
                unique: true,
                filter: "[RecordLocator] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentReservations_IdempotencyKey",
                schema: "Order",
                table: "FulfillmentReservations",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentReservations_OrderId",
                schema: "Order",
                table: "FulfillmentReservations",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentTaskAttempts_FulfillmentTaskId_AttemptNumber",
                schema: "Order",
                table: "FulfillmentTaskAttempts",
                columns: new[] { "FulfillmentTaskId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentTasks_FulfillmentReservationId_TaskType",
                schema: "Order",
                table: "FulfillmentTasks",
                columns: new[] { "FulfillmentReservationId", "TaskType" });

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentTasks_OrderId",
                schema: "Order",
                table: "FulfillmentTasks",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentTaskTargets_FulfillmentTaskId",
                schema: "Order",
                table: "FulfillmentTaskTargets",
                column: "FulfillmentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentTaskTargets_ReservationUnitId",
                schema: "Order",
                table: "FulfillmentTaskTargets",
                column: "ReservationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInteractions_FulfillmentTaskId_AttemptNumber",
                schema: "Order",
                table: "ProviderInteractions",
                columns: new[] { "FulfillmentTaskId", "AttemptNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReservationUnits_FulfillmentReservationId_UnitCorrelationKey",
                schema: "Order",
                table: "ReservationUnits",
                columns: new[] { "FulfillmentReservationId", "UnitCorrelationKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FulfillmentTaskAttempts",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "FulfillmentTaskTargets",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "ProviderInteractions",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "ReservationUnits",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "FulfillmentTasks",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "FulfillmentReservations",
                schema: "Order");

            migrationBuilder.DropIndex(
                name: "IX_Orders_RecordLocator",
                schema: "Order",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "FulfillmentProviderKey",
                schema: "Order",
                table: "OrderServices");

            migrationBuilder.DropColumn(
                name: "RecordLocator",
                schema: "Order",
                table: "Orders");
        }
    }
}
