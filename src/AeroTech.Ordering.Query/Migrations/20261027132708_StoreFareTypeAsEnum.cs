using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Query.Migrations
{
    /// <inheritdoc />
    public partial class StoreFareTypeAsEnum : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(FareTypeNamesToValues("[ReadModel].[OrderServices]"));

            migrationBuilder.AlterColumn<int>(
                name: "FareType",
                schema: "ReadModel",
                table: "OrderServices",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FareType",
                schema: "ReadModel",
                table: "OrderServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.Sql(FareTypeValuesToNames("[ReadModel].[OrderServices]"));
        }

        private static string FareTypeNamesToValues(string table) =>
            $"""
            IF EXISTS (SELECT 1 FROM {table} WHERE LTRIM(RTRIM([FareType])) <> N'' AND [FareType] NOT IN (N'Public', N'Private', N'Negotiated', N'Charter'))
                THROW 50000, N'{table}.[FareType] holds a value that is not an AirFareType name.', 1;

            UPDATE {table}
            SET [FareType] = CASE [FareType]
                    WHEN N'Public' THEN N'1'
                    WHEN N'Private' THEN N'2'
                    WHEN N'Negotiated' THEN N'3'
                    WHEN N'Charter' THEN N'4'
                END;
            """;

        private static string FareTypeValuesToNames(string table) =>
            $"""
            UPDATE {table}
            SET [FareType] = CASE [FareType]
                    WHEN N'1' THEN N'Public'
                    WHEN N'2' THEN N'Private'
                    WHEN N'3' THEN N'Negotiated'
                    WHEN N'4' THEN N'Charter'
                END;
            """;
    }
}
