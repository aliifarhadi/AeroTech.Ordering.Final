using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Query.Migrations
{
    /// <inheritdoc />
    public partial class ProjectAncillaryServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<long>(
                name: "TravellerId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<long>(
                name: "SegmentId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "OrderAncillaryServices",
                schema: "ReadModel",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    VariantCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Profile = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SelectionScope = table.Column<int>(type: "int", nullable: true),
                    CoverageScope = table.Column<int>(type: "int", nullable: true),
                    CoveredAirServiceIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PointOfSaleId = table.Column<long>(type: "bigint", nullable: true),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    SourceReference = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SourceOrderCommercialVersion = table.Column<int>(type: "int", nullable: true),
                    SourceSelectionId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    SourceOfferItemId = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: true),
                    DefinitionVersionId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceDefinitionRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ServiceTypeCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    ServiceSubCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Rfic = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    Rfisc = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    SsrCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    ProvisionId = table.Column<long>(type: "bigint", nullable: false),
                    PurchaseStage = table.Column<int>(type: "int", nullable: false),
                    BookingMethod = table.Column<int>(type: "int", nullable: false),
                    BookingRequired = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmationRequirement = table.Column<int>(type: "int", nullable: false),
                    ProvisionFulfillmentProviderKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    DocumentRouting = table.Column<int>(type: "int", nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    DocumentRequired = table.Column<bool>(type: "bit", nullable: false),
                    RequiresCheckAtReserve = table.Column<bool>(type: "bit", nullable: false),
                    PricingId = table.Column<long>(type: "bigint", nullable: false),
                    PricingRateId = table.Column<long>(type: "bigint", nullable: false),
                    PricingVersion = table.Column<int>(type: "int", nullable: false),
                    PricingUnit = table.Column<int>(type: "int", nullable: false),
                    BaggageChargeKind = table.Column<int>(type: "int", nullable: true),
                    BaggageAllowanceConcept = table.Column<int>(type: "int", nullable: true),
                    BaggagePackageWeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageWeightFromExclusiveKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageWeightToInclusiveKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageEquipmentKind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BaggageBagRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    BaggageMeasuredWeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageLengthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageWidthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageHeightCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BaggageWeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MealKind = table.Column<int>(type: "int", nullable: true),
                    MealCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    MealMenuItemRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MealDietaryCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    MealExclusiveFamilyCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PetTransportMode = table.Column<int>(type: "int", nullable: true),
                    PetAnimalType = table.Column<int>(type: "int", nullable: true),
                    PetOtherAnimalCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PetAnimalAgeWeeks = table.Column<int>(type: "int", nullable: true),
                    PetCombinedWeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PetCarrierLengthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PetCarrierWidthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PetCarrierHeightCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    PetDocumentAcknowledgements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PetSizeBracket = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AssistanceKind = table.Column<int>(type: "int", nullable: true),
                    AssistanceSsrCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    AssistanceInfantTravellerId = table.Column<long>(type: "bigint", nullable: true),
                    AssistanceGuardianTravellerId = table.Column<long>(type: "bigint", nullable: true),
                    AirportServiceKind = table.Column<int>(type: "int", nullable: true),
                    AirportServiceAirportId = table.Column<int>(type: "int", nullable: true),
                    AirportServiceTerminalRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AirportServiceFacilityId = table.Column<long>(type: "bigint", nullable: true),
                    AirportServiceDirection = table.Column<int>(type: "int", nullable: true),
                    AirportServiceTimeWithOffset = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AirportServiceGuestCount = table.Column<int>(type: "int", nullable: true),
                    PriorityKind = table.Column<int>(type: "int", nullable: true),
                    PriorityZoneCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PriorityGroupCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PriorityFareBenefitRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    PriorityOptIn = table.Column<bool>(type: "bit", nullable: true),
                    ConnectivityPlanKind = table.Column<int>(type: "int", nullable: true),
                    ConnectivityDurationMinutes = table.Column<int>(type: "int", nullable: true),
                    ConnectivityIncludedDataMb = table.Column<int>(type: "int", nullable: true),
                    ConnectivityMaxDevices = table.Column<int>(type: "int", nullable: true),
                    ConnectivityDeliveryStage = table.Column<int>(type: "int", nullable: true),
                    ConnectivityDeviceCount = table.Column<int>(type: "int", nullable: true),
                    UpgradeFromCabinId = table.Column<int>(type: "int", nullable: true),
                    UpgradeToCabinId = table.Column<int>(type: "int", nullable: true),
                    UpgradeKind = table.Column<int>(type: "int", nullable: true),
                    UpgradeRequiresTicketExchange = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAncillaryServices", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrderAncillaryServices_OrderId",
                schema: "ReadModel",
                table: "OrderAncillaryServices",
                column: "OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderAncillaryServices",
                schema: "ReadModel");

            migrationBuilder.AlterColumn<long>(
                name: "TravellerId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "SegmentId",
                schema: "ReadModel",
                table: "OrderServices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);
        }
    }
}
