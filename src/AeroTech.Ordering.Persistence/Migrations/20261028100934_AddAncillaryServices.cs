using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAncillaryServices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SourceFeeApplicationUnit",
                schema: "Order",
                table: "PricingLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceTaxTreatment",
                schema: "Order",
                table: "PricingLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxCountryId",
                schema: "Order",
                table: "PricingLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TaxStationId",
                schema: "Order",
                table: "PricingLines",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "TravellerId",
                schema: "Order",
                table: "OrderServices",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddColumn<long>(
                name: "DefinitionVersionId",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PointOfSaleId",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PricingId",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "PricingRateId",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingUnit",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PricingVersion",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvisionBookingMethod",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProvisionBookingRequired",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvisionConfirmationRequirement",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProvisionDocumentRequired",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvisionDocumentRouting",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvisionDocumentType",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProvisionFulfillmentProviderKey",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProvisionId",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvisionPurchaseStage",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProvisionRequiresCheckAtReserve",
                schema: "Order",
                table: "OrderSeatServices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rfic",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Rfisc",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceDefinitionRef",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceSubCode",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceTypeCode",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceKind",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceOfferItemId",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SourceOrderCommercialVersion",
                schema: "Order",
                table: "OrderSeatServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceReference",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceSelectionId",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SsrCode",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VariantCode",
                schema: "Order",
                table: "OrderSeatServices",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OrderAncillaryServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Profile = table.Column<int>(type: "int", nullable: false),
                    VariantCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    SelectionScope = table.Column<int>(type: "int", nullable: false),
                    CoverageScope = table.Column<int>(type: "int", nullable: false),
                    CoveredAirServiceIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
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
                    ProvisionPurchaseStage = table.Column<int>(type: "int", nullable: false),
                    ProvisionBookingMethod = table.Column<int>(type: "int", nullable: false),
                    ProvisionBookingRequired = table.Column<bool>(type: "bit", nullable: false),
                    ProvisionConfirmationRequirement = table.Column<int>(type: "int", nullable: false),
                    ProvisionFulfillmentProviderKey = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProvisionDocumentRouting = table.Column<int>(type: "int", nullable: false),
                    ProvisionDocumentType = table.Column<int>(type: "int", nullable: false),
                    ProvisionDocumentRequired = table.Column<bool>(type: "bit", nullable: false),
                    ProvisionRequiresCheckAtReserve = table.Column<bool>(type: "bit", nullable: false),
                    PricingId = table.Column<long>(type: "bigint", nullable: false),
                    PricingRateId = table.Column<long>(type: "bigint", nullable: false),
                    PricingVersion = table.Column<int>(type: "int", nullable: false),
                    PricingUnit = table.Column<int>(type: "int", nullable: false),
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
                    PetDocumentAcknowledgements = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_OrderAncillaryServices_OrderServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderBaggageServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ChargeKind = table.Column<int>(type: "int", nullable: false),
                    AllowanceConcept = table.Column<int>(type: "int", nullable: true),
                    PackageWeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WeightFromExclusiveKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WeightToInclusiveKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    EquipmentKind = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BagRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    MeasuredWeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    LengthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WidthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    HeightCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    WeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderBaggageServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderBaggageServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrderBaggageServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderAncillaryServices",
                schema: "Order");

            migrationBuilder.DropColumn(
                name: "SourceFeeApplicationUnit",
                schema: "Order",
                table: "PricingLines");

            migrationBuilder.DropColumn(
                name: "SourceTaxTreatment",
                schema: "Order",
                table: "PricingLines");

            migrationBuilder.DropColumn(
                name: "TaxCountryId",
                schema: "Order",
                table: "PricingLines");

            migrationBuilder.DropColumn(
                name: "TaxStationId",
                schema: "Order",
                table: "PricingLines");

            migrationBuilder.DropColumn(
                name: "DefinitionVersionId",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "PointOfSaleId",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "PricingId",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "PricingRateId",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "PricingUnit",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "PricingVersion",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionBookingMethod",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionBookingRequired",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionConfirmationRequirement",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionDocumentRequired",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionDocumentRouting",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionDocumentType",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionFulfillmentProviderKey",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionId",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionPurchaseStage",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ProvisionRequiresCheckAtReserve",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "Rfic",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "Rfisc",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ServiceDefinitionRef",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ServiceSubCode",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "ServiceTypeCode",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "SourceKind",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "SourceOfferItemId",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "SourceOrderCommercialVersion",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "SourceReference",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "SourceSelectionId",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "SsrCode",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.DropColumn(
                name: "VariantCode",
                schema: "Order",
                table: "OrderSeatServices");

            migrationBuilder.AlterColumn<long>(
                name: "TravellerId",
                schema: "Order",
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
