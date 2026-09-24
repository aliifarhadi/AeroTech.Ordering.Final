using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderEvidenceAndValidationTimeLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderInteractions_FulfillmentTaskId_AttemptNumber",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.AddColumn<long>(
                name: "FulfillmentTaskAttemptId",
                schema: "Order",
                table: "ProviderInteractions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sequence",
                schema: "Order",
                table: "ProviderInteractions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FulfillmentProviderKey",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CorrelationReference",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestPayload",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestHash",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsePayload",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponseHash",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderOperationRef",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE [interaction]
                SET [FulfillmentTaskAttemptId] = [attempt].[Id],
                    [Sequence] = 1,
                    [FulfillmentProviderKey] = [task].[FulfillmentProviderKey],
                    [IdempotencyKey] = [task].[IdempotencyKey],
                    [CorrelationReference] = CASE WHEN [interaction].[InteractionType] = 1 THEN [task].[CorrelationReference] END,
                    [RequestPayload] = N'',
                    [RequestHash] = CONVERT(nvarchar(64), HASHBYTES('SHA2_256', CONVERT(varbinary(max), N'')), 2)
                FROM [Order].[ProviderInteractions] AS [interaction]
                INNER JOIN [Order].[FulfillmentTasks] AS [task]
                    ON [task].[Id] = [interaction].[FulfillmentTaskId]
                INNER JOIN [Order].[FulfillmentTaskAttempts] AS [attempt]
                    ON [attempt].[FulfillmentTaskId] = [interaction].[FulfillmentTaskId]
                    AND [attempt].[AttemptNumber] = [interaction].[AttemptNumber];
                """);

            migrationBuilder.AlterColumn<long>(
                name: "FulfillmentTaskAttemptId",
                schema: "Order",
                table: "ProviderInteractions",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Sequence",
                schema: "Order",
                table: "ProviderInteractions",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FulfillmentProviderKey",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestPayload",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequestHash",
                schema: "Order",
                table: "ProviderInteractions",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReservationValidationTimeLimit",
                schema: "Order",
                table: "FulfillmentReservations",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInteractions_FulfillmentTaskAttemptId_Sequence",
                schema: "Order",
                table: "ProviderInteractions",
                columns: new[] { "FulfillmentTaskAttemptId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInteractions_FulfillmentTaskId",
                schema: "Order",
                table: "ProviderInteractions",
                column: "FulfillmentTaskId");

            migrationBuilder.CreateIndex(
                name: "IX_FulfillmentReservations_Status",
                schema: "Order",
                table: "FulfillmentReservations",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProviderInteractions_FulfillmentTaskAttemptId_Sequence",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropIndex(
                name: "IX_ProviderInteractions_FulfillmentTaskId",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropIndex(
                name: "IX_FulfillmentReservations_Status",
                schema: "Order",
                table: "FulfillmentReservations");

            migrationBuilder.DropColumn(
                name: "CorrelationReference",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "FulfillmentProviderKey",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "FulfillmentTaskAttemptId",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "ProviderOperationRef",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "RequestHash",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "RequestPayload",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "ResponseHash",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "ResponsePayload",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "Sequence",
                schema: "Order",
                table: "ProviderInteractions");

            migrationBuilder.DropColumn(
                name: "ReservationValidationTimeLimit",
                schema: "Order",
                table: "FulfillmentReservations");

            migrationBuilder.CreateIndex(
                name: "IX_ProviderInteractions_FulfillmentTaskId_AttemptNumber",
                schema: "Order",
                table: "ProviderInteractions",
                columns: new[] { "FulfillmentTaskId", "AttemptNumber" },
                unique: true);
        }
    }
}
