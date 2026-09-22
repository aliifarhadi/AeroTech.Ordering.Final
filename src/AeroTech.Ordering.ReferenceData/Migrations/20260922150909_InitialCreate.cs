using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.ReferenceData.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ReferenceData");

            migrationBuilder.CreateTable(
                name: "AirlineOffices",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrganisationUnitId = table.Column<long>(type: "bigint", nullable: true),
                    OrganisationUnitName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ParentOfficeId = table.Column<long>(type: "bigint", nullable: true),
                    ParentOfficeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    LegalEntityId = table.Column<long>(type: "bigint", nullable: false),
                    LegalEntityLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LegalEntityEffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    AirportId = table.Column<int>(type: "int", nullable: true),
                    PointOfSaleCountryId = table.Column<int>(type: "int", nullable: true),
                    PointOfSaleCityId = table.Column<int>(type: "int", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirlineOffices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Airlines",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IataCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airlines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Airports",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IataCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    IkaoCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Airports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AirportTerminals",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    AirportId = table.Column<int>(type: "int", nullable: false),
                    Number = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AirportTerminals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cities",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    IataCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Currencies",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    DecimalPlaces = table.Column<int>(type: "int", nullable: false),
                    RoundingFactor = table.Column<double>(type: "float", nullable: false),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Currencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    CustomerNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IndividualId = table.Column<long>(type: "bigint", nullable: true),
                    TravelAgencyId = table.Column<long>(type: "bigint", nullable: true),
                    OrganizationId = table.Column<long>(type: "bigint", nullable: true),
                    SubjectId = table.Column<long>(type: "bigint", nullable: false),
                    SubjectName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RelationshipStartedOn = table.Column<DateOnly>(type: "date", nullable: false),
                    RelationshipEndedOn = table.Column<DateOnly>(type: "date", nullable: true),
                    PreferredCurrencyId = table.Column<int>(type: "int", nullable: true),
                    PreferredLanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    SuspensionReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ClosureReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperatorSettings",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ScopeKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HomeAirlineId = table.Column<long>(type: "bigint", nullable: false),
                    DefaultCurrencyId = table.Column<int>(type: "int", nullable: true),
                    DefaultLanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DefaultTimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperatorSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReferenceDataSyncStates",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LastSync = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReferenceDataSyncStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelAgencies",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradingName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ParentAgencyId = table.Column<long>(type: "bigint", nullable: true),
                    ParentAgencyLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    PreferredCurrencyId = table.Column<int>(type: "int", nullable: true),
                    PreferredLanguageCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    OnboardedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TerminatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    TerminationReasonCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrimaryAccreditationTypeCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PrimaryAccreditationIdentifier = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelAgencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TravelAgencyOffices",
                schema: "ReferenceData",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TravelAgencyId = table.Column<long>(type: "bigint", nullable: false),
                    TravelAgencyLegalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParentOfficeId = table.Column<long>(type: "bigint", nullable: true),
                    ParentOfficeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CountryId = table.Column<int>(type: "int", nullable: false),
                    CityId = table.Column<int>(type: "int", nullable: true),
                    AirportId = table.Column<int>(type: "int", nullable: true),
                    PointOfSaleCountryId = table.Column<int>(type: "int", nullable: true),
                    PointOfSaleCityId = table.Column<int>(type: "int", nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LastUpdateTime = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TravelAgencyOffices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AirlineOffices_Code",
                schema: "ReferenceData",
                table: "AirlineOffices",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_AirlineOffices_ParentOfficeId",
                schema: "ReferenceData",
                table: "AirlineOffices",
                column: "ParentOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_Airlines_IataCode",
                schema: "ReferenceData",
                table: "Airlines",
                column: "IataCode");

            migrationBuilder.CreateIndex(
                name: "IX_Airports_IataCode",
                schema: "ReferenceData",
                table: "Airports",
                column: "IataCode");

            migrationBuilder.CreateIndex(
                name: "IX_AirportTerminals_AirportId",
                schema: "ReferenceData",
                table: "AirportTerminals",
                column: "AirportId");

            migrationBuilder.CreateIndex(
                name: "IX_Cities_IataCode",
                schema: "ReferenceData",
                table: "Cities",
                column: "IataCode");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_CustomerNumber",
                schema: "ReferenceData",
                table: "Customers",
                column: "CustomerNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_TravelAgencyId",
                schema: "ReferenceData",
                table: "Customers",
                column: "TravelAgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_OperatorSettings_ScopeKey",
                schema: "ReferenceData",
                table: "OperatorSettings",
                column: "ScopeKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TravelAgencies_Code",
                schema: "ReferenceData",
                table: "TravelAgencies",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_TravelAgencies_ParentAgencyId",
                schema: "ReferenceData",
                table: "TravelAgencies",
                column: "ParentAgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelAgencyOffices_Code",
                schema: "ReferenceData",
                table: "TravelAgencyOffices",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_TravelAgencyOffices_ParentOfficeId",
                schema: "ReferenceData",
                table: "TravelAgencyOffices",
                column: "ParentOfficeId");

            migrationBuilder.CreateIndex(
                name: "IX_TravelAgencyOffices_TravelAgencyId",
                schema: "ReferenceData",
                table: "TravelAgencyOffices",
                column: "TravelAgencyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AirlineOffices",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "Airlines",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "Airports",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "AirportTerminals",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "Cities",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "Currencies",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "Customers",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "OperatorSettings",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "ReferenceDataSyncStates",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "TravelAgencies",
                schema: "ReferenceData");

            migrationBuilder.DropTable(
                name: "TravelAgencyOffices",
                schema: "ReferenceData");
        }
    }
}
