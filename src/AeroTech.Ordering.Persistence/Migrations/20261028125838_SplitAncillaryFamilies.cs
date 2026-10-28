using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AeroTech.Ordering.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SplitAncillaryFamilies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrderAirportServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    AirportId = table.Column<int>(type: "int", nullable: false),
                    TerminalRef = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FacilityId = table.Column<long>(type: "bigint", nullable: true),
                    Direction = table.Column<int>(type: "int", nullable: false),
                    TimeWithOffset = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    GuestCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAirportServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAirportServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderAssistedTravelServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AssistanceKind = table.Column<int>(type: "int", nullable: false),
                    AssistanceSsrCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    InfantTravellerId = table.Column<long>(type: "bigint", nullable: true),
                    GuardianTravellerId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAssistedTravelServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAssistedTravelServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderConnectivityServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PlanKind = table.Column<int>(type: "int", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: true),
                    IncludedDataMb = table.Column<int>(type: "int", nullable: true),
                    MaxDevices = table.Column<int>(type: "int", nullable: true),
                    DeliveryStage = table.Column<int>(type: "int", nullable: false),
                    DeviceCount = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderConnectivityServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderConnectivityServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderMealServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    MealKind = table.Column<int>(type: "int", nullable: false),
                    MealCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    MenuItemRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DietaryCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ExclusiveMealFamilyCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderMealServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderMealServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderPetServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    TransportMode = table.Column<int>(type: "int", nullable: false),
                    AnimalType = table.Column<int>(type: "int", nullable: false),
                    OtherAnimalCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AnimalAgeWeeks = table.Column<int>(type: "int", nullable: true),
                    CombinedWeightKg = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarrierLengthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarrierWidthCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CarrierHeightCm = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DocumentAcknowledgements = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SizeBracket = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPetServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPetServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderPriorityServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    PriorityZoneCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PriorityGroupCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FareBenefitRef = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    OptIn = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderPriorityServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderPriorityServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderUpgradeServices",
                schema: "Order",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    FromCabinId = table.Column<int>(type: "int", nullable: false),
                    ToCabinId = table.Column<int>(type: "int", nullable: false),
                    UpgradeKind = table.Column<int>(type: "int", nullable: false),
                    RequiresTicketExchange = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderUpgradeServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderUpgradeServices_OrderAncillaryServices_Id",
                        column: x => x.Id,
                        principalSchema: "Order",
                        principalTable: "OrderAncillaryServices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO [Order].[OrderMealServices] ([Id], [MealKind], [MealCode], [MenuItemRef], [DietaryCode], [ExclusiveMealFamilyCode])
                SELECT [Id], [MealKind], [MealCode], [MealMenuItemRef], [MealDietaryCode], [MealExclusiveFamilyCode]
                FROM [Order].[OrderAncillaryServices]
                WHERE [MealKind] IS NOT NULL;

                INSERT INTO [Order].[OrderPetServices] ([Id], [TransportMode], [AnimalType], [OtherAnimalCode], [AnimalAgeWeeks], [CombinedWeightKg], [CarrierLengthCm], [CarrierWidthCm], [CarrierHeightCm], [DocumentAcknowledgements], [SizeBracket])
                SELECT [Id], [PetTransportMode], [PetAnimalType], [PetOtherAnimalCode], [PetAnimalAgeWeeks], [PetCombinedWeightKg], [PetCarrierLengthCm], [PetCarrierWidthCm], [PetCarrierHeightCm], [PetDocumentAcknowledgements], [PetSizeBracket]
                FROM [Order].[OrderAncillaryServices]
                WHERE [PetTransportMode] IS NOT NULL;

                INSERT INTO [Order].[OrderAssistedTravelServices] ([Id], [AssistanceKind], [AssistanceSsrCode], [InfantTravellerId], [GuardianTravellerId])
                SELECT [Id], [AssistanceKind], [AssistanceSsrCode], [AssistanceInfantTravellerId], [AssistanceGuardianTravellerId]
                FROM [Order].[OrderAncillaryServices]
                WHERE [AssistanceKind] IS NOT NULL;

                INSERT INTO [Order].[OrderAirportServices] ([Id], [Kind], [AirportId], [TerminalRef], [FacilityId], [Direction], [TimeWithOffset], [GuestCount])
                SELECT [Id], [AirportServiceKind], [AirportServiceAirportId], [AirportServiceTerminalRef], [AirportServiceFacilityId], [AirportServiceDirection], [AirportServiceTimeWithOffset], [AirportServiceGuestCount]
                FROM [Order].[OrderAncillaryServices]
                WHERE [AirportServiceKind] IS NOT NULL;

                INSERT INTO [Order].[OrderPriorityServices] ([Id], [Kind], [PriorityZoneCode], [PriorityGroupCode], [FareBenefitRef], [OptIn])
                SELECT [Id], [PriorityKind], [PriorityZoneCode], [PriorityGroupCode], [PriorityFareBenefitRef], [PriorityOptIn]
                FROM [Order].[OrderAncillaryServices]
                WHERE [PriorityKind] IS NOT NULL;

                INSERT INTO [Order].[OrderConnectivityServices] ([Id], [PlanKind], [DurationMinutes], [IncludedDataMb], [MaxDevices], [DeliveryStage], [DeviceCount])
                SELECT [Id], [ConnectivityPlanKind], [ConnectivityDurationMinutes], [ConnectivityIncludedDataMb], [ConnectivityMaxDevices], [ConnectivityDeliveryStage], [ConnectivityDeviceCount]
                FROM [Order].[OrderAncillaryServices]
                WHERE [ConnectivityPlanKind] IS NOT NULL;

                INSERT INTO [Order].[OrderUpgradeServices] ([Id], [FromCabinId], [ToCabinId], [UpgradeKind], [RequiresTicketExchange])
                SELECT [Id], [UpgradeFromCabinId], [UpgradeToCabinId], [UpgradeKind], [UpgradeRequiresTicketExchange]
                FROM [Order].[OrderAncillaryServices]
                WHERE [UpgradeFromCabinId] IS NOT NULL;
                """);

            migrationBuilder.Sql(
                """
                DECLARE @unmatched int;

                WITH ExpectedFamilies AS
                (
                    SELECT [Id],
                        CASE
                            WHEN [VariantCode] IN (N'A01', N'A02', N'A03', N'A04', N'A05', N'A06') THEN N'OrderBaggageServices'
                            WHEN [VariantCode] = N'A10' THEN N'OrderUpgradeServices'
                            WHEN [VariantCode] IN (N'A11', N'A12') THEN N'OrderMealServices'
                            WHEN [VariantCode] IN (N'A13', N'A14') THEN N'OrderPetServices'
                            WHEN [VariantCode] IN (N'A15', N'A16', N'A17', N'A18', N'A19') THEN N'OrderAssistedTravelServices'
                            WHEN [VariantCode] IN (N'A20', N'A21', N'A22') THEN N'OrderAirportServices'
                            WHEN [VariantCode] = N'A23' THEN N'OrderPriorityServices'
                            WHEN [VariantCode] = N'A24' THEN N'OrderConnectivityServices'
                        END AS [FamilyTable]
                    FROM [Order].[OrderAncillaryServices]
                ),
                FamilyRows AS
                (
                    SELECT [Id], N'OrderBaggageServices' AS [FamilyTable] FROM [Order].[OrderBaggageServices]
                    UNION ALL SELECT [Id], N'OrderUpgradeServices' FROM [Order].[OrderUpgradeServices]
                    UNION ALL SELECT [Id], N'OrderMealServices' FROM [Order].[OrderMealServices]
                    UNION ALL SELECT [Id], N'OrderPetServices' FROM [Order].[OrderPetServices]
                    UNION ALL SELECT [Id], N'OrderAssistedTravelServices' FROM [Order].[OrderAssistedTravelServices]
                    UNION ALL SELECT [Id], N'OrderAirportServices' FROM [Order].[OrderAirportServices]
                    UNION ALL SELECT [Id], N'OrderPriorityServices' FROM [Order].[OrderPriorityServices]
                    UNION ALL SELECT [Id], N'OrderConnectivityServices' FROM [Order].[OrderConnectivityServices]
                )
                SELECT @unmatched = COUNT(*)
                FROM ExpectedFamilies AS expected
                WHERE expected.[FamilyTable] IS NULL
                    OR (SELECT COUNT(*) FROM FamilyRows AS family WHERE family.[Id] = expected.[Id]) <> 1
                    OR NOT EXISTS (SELECT 1 FROM FamilyRows AS family WHERE family.[Id] = expected.[Id] AND family.[FamilyTable] = expected.[FamilyTable]);

                IF @unmatched > 0
                BEGIN
                    DECLARE @message nvarchar(2048) = CONCAT(N'SplitAncillaryFamilies stopped before dropping any column: ', @unmatched, N' ancillary service row(s) do not map to exactly one family table matching their VariantCode.');
                    THROW 50000, @message, 1;
                END;
                """);

            migrationBuilder.DropColumn(
                name: "AirportServiceAirportId",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AirportServiceDirection",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AirportServiceFacilityId",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AirportServiceGuestCount",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AirportServiceKind",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AirportServiceTerminalRef",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AirportServiceTimeWithOffset",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AssistanceGuardianTravellerId",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AssistanceInfantTravellerId",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AssistanceKind",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "AssistanceSsrCode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "ConnectivityDeliveryStage",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "ConnectivityDeviceCount",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "ConnectivityDurationMinutes",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "ConnectivityIncludedDataMb",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "ConnectivityMaxDevices",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "ConnectivityPlanKind",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "MealCode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "MealDietaryCode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "MealExclusiveFamilyCode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "MealKind",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "MealMenuItemRef",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetAnimalAgeWeeks",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetAnimalType",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetCarrierHeightCm",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetCarrierLengthCm",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetCarrierWidthCm",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetCombinedWeightKg",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetDocumentAcknowledgements",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetOtherAnimalCode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetSizeBracket",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PetTransportMode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PriorityFareBenefitRef",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PriorityGroupCode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PriorityKind",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PriorityOptIn",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "PriorityZoneCode",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "UpgradeFromCabinId",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "UpgradeKind",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "UpgradeRequiresTicketExchange",
                schema: "Order",
                table: "OrderAncillaryServices");

            migrationBuilder.DropColumn(
                name: "UpgradeToCabinId",
                schema: "Order",
                table: "OrderAncillaryServices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AirportServiceAirportId",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AirportServiceDirection",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AirportServiceFacilityId",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AirportServiceGuestCount",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AirportServiceKind",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AirportServiceTerminalRef",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AirportServiceTimeWithOffset",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssistanceGuardianTravellerId",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "AssistanceInfantTravellerId",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssistanceKind",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AssistanceSsrCode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectivityDeliveryStage",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectivityDeviceCount",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectivityDurationMinutes",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectivityIncludedDataMb",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectivityMaxDevices",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ConnectivityPlanKind",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealCode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealDietaryCode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealExclusiveFamilyCode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MealKind",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MealMenuItemRef",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PetAnimalAgeWeeks",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PetAnimalType",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PetCarrierHeightCm",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PetCarrierLengthCm",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PetCarrierWidthCm",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PetCombinedWeightKg",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PetDocumentAcknowledgements",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PetOtherAnimalCode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PetSizeBracket",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PetTransportMode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriorityFareBenefitRef",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriorityGroupCode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PriorityKind",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PriorityOptIn",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PriorityZoneCode",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpgradeFromCabinId",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpgradeKind",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "UpgradeRequiresTicketExchange",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpgradeToCabinId",
                schema: "Order",
                table: "OrderAncillaryServices",
                type: "int",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE ancillary
                SET [MealKind] = family.[MealKind],
                    [MealCode] = family.[MealCode],
                    [MealMenuItemRef] = family.[MenuItemRef],
                    [MealDietaryCode] = family.[DietaryCode],
                    [MealExclusiveFamilyCode] = family.[ExclusiveMealFamilyCode]
                FROM [Order].[OrderAncillaryServices] AS ancillary
                INNER JOIN [Order].[OrderMealServices] AS family ON family.[Id] = ancillary.[Id];

                UPDATE ancillary
                SET [PetTransportMode] = family.[TransportMode],
                    [PetAnimalType] = family.[AnimalType],
                    [PetOtherAnimalCode] = family.[OtherAnimalCode],
                    [PetAnimalAgeWeeks] = family.[AnimalAgeWeeks],
                    [PetCombinedWeightKg] = family.[CombinedWeightKg],
                    [PetCarrierLengthCm] = family.[CarrierLengthCm],
                    [PetCarrierWidthCm] = family.[CarrierWidthCm],
                    [PetCarrierHeightCm] = family.[CarrierHeightCm],
                    [PetDocumentAcknowledgements] = family.[DocumentAcknowledgements],
                    [PetSizeBracket] = family.[SizeBracket]
                FROM [Order].[OrderAncillaryServices] AS ancillary
                INNER JOIN [Order].[OrderPetServices] AS family ON family.[Id] = ancillary.[Id];

                UPDATE ancillary
                SET [AssistanceKind] = family.[AssistanceKind],
                    [AssistanceSsrCode] = family.[AssistanceSsrCode],
                    [AssistanceInfantTravellerId] = family.[InfantTravellerId],
                    [AssistanceGuardianTravellerId] = family.[GuardianTravellerId]
                FROM [Order].[OrderAncillaryServices] AS ancillary
                INNER JOIN [Order].[OrderAssistedTravelServices] AS family ON family.[Id] = ancillary.[Id];

                UPDATE ancillary
                SET [AirportServiceKind] = family.[Kind],
                    [AirportServiceAirportId] = family.[AirportId],
                    [AirportServiceTerminalRef] = family.[TerminalRef],
                    [AirportServiceFacilityId] = family.[FacilityId],
                    [AirportServiceDirection] = family.[Direction],
                    [AirportServiceTimeWithOffset] = family.[TimeWithOffset],
                    [AirportServiceGuestCount] = family.[GuestCount]
                FROM [Order].[OrderAncillaryServices] AS ancillary
                INNER JOIN [Order].[OrderAirportServices] AS family ON family.[Id] = ancillary.[Id];

                UPDATE ancillary
                SET [PriorityKind] = family.[Kind],
                    [PriorityZoneCode] = family.[PriorityZoneCode],
                    [PriorityGroupCode] = family.[PriorityGroupCode],
                    [PriorityFareBenefitRef] = family.[FareBenefitRef],
                    [PriorityOptIn] = family.[OptIn]
                FROM [Order].[OrderAncillaryServices] AS ancillary
                INNER JOIN [Order].[OrderPriorityServices] AS family ON family.[Id] = ancillary.[Id];

                UPDATE ancillary
                SET [ConnectivityPlanKind] = family.[PlanKind],
                    [ConnectivityDurationMinutes] = family.[DurationMinutes],
                    [ConnectivityIncludedDataMb] = family.[IncludedDataMb],
                    [ConnectivityMaxDevices] = family.[MaxDevices],
                    [ConnectivityDeliveryStage] = family.[DeliveryStage],
                    [ConnectivityDeviceCount] = family.[DeviceCount]
                FROM [Order].[OrderAncillaryServices] AS ancillary
                INNER JOIN [Order].[OrderConnectivityServices] AS family ON family.[Id] = ancillary.[Id];

                UPDATE ancillary
                SET [UpgradeFromCabinId] = family.[FromCabinId],
                    [UpgradeToCabinId] = family.[ToCabinId],
                    [UpgradeKind] = family.[UpgradeKind],
                    [UpgradeRequiresTicketExchange] = family.[RequiresTicketExchange]
                FROM [Order].[OrderAncillaryServices] AS ancillary
                INNER JOIN [Order].[OrderUpgradeServices] AS family ON family.[Id] = ancillary.[Id];
                """);

            migrationBuilder.DropTable(
                name: "OrderAirportServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderAssistedTravelServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderConnectivityServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderMealServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderPetServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderPriorityServices",
                schema: "Order");

            migrationBuilder.DropTable(
                name: "OrderUpgradeServices",
                schema: "Order");
        }
    }
}
