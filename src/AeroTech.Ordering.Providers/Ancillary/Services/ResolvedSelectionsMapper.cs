using AeroTech.Messages.Ancillary.Enums;
using AeroTech.Ordering.Domain.OrderAggregate;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.Providers.Ancillary.Wire;

namespace AeroTech.Ordering.Providers.Ancillary.Services
{
    public static class ResolvedSelectionsMapper
    {
        public static ResolvedAncillarySelection ToDomain(string selectionId, ResolvedSelectionsResponse source)
        {
            if (source.TravelInsurance.FirstOrDefault() is { } insurance)
                throw ExceptionFactory.AncillaryVariantIsNotRecognised(insurance.OfferItemId, insurance.VariantCode);

            if (source.ExtraSeats.FirstOrDefault() is { } extraSeat)
                throw ExceptionFactory.AncillaryVariantSourceIsIncomplete(extraSeat.OfferItemId, extraSeat.VariantCode);

            var lines = source.Baggages
                .Select(line => Line(selectionId, line, AncillaryProfile.Baggage, details => new ResolvedAncillaryDetails { Baggage = Baggage(details, line.Selected.Options?.Baggage) }))
                .Concat(source.Meals.Select(line => Line(selectionId, line, AncillaryProfile.Meal, details => new ResolvedAncillaryDetails { Meal = Meal(details) })))
                .Concat(source.Pets.Select(line => Line(selectionId, line, AncillaryProfile.Pet, details => new ResolvedAncillaryDetails { Pet = Pet(details, line.Selected.Options?.Pet) })))
                .Concat(source.AssistedTravel.Select(line => Line(selectionId, line, AncillaryProfile.AssistedTravel, details => new ResolvedAncillaryDetails { Assistance = Assistance(line, details) })))
                .Concat(source.AirportServices.Select(line => Line(selectionId, line, AncillaryProfile.AirportService, details => new ResolvedAncillaryDetails { AirportService = AirportService(details, line.Selected.Options?.AirportService) })))
                .Concat(source.PriorityServices.Select(line => Line(selectionId, line, AncillaryProfile.Priority, details => new ResolvedAncillaryDetails { Priority = Priority(details, line.Selected.Options?.Priority) })))
                .Concat(source.Connectivity.Select(line => Line(selectionId, line, AncillaryProfile.Connectivity, details => new ResolvedAncillaryDetails { Connectivity = Connectivity(details, line.Selected.Options?.Connectivity) })))
                .Concat(source.Upgrades.Select(line => Line(selectionId, line, AncillaryProfile.Upgrade, details => new ResolvedAncillaryDetails { Upgrade = Upgrade(details) })))
                .ToList();

            return new ResolvedAncillarySelection(
                selectionId,
                source.SourceKind ?? throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionId, nameof(source.SourceKind)),
                source.SourceReference ?? throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionId, nameof(source.SourceReference)),
                source.OrderCommercialVersion,
                source.ExpiresAtUtc ?? throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionId, nameof(source.ExpiresAtUtc)),
                source.SelectedLineCount ?? throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionId, nameof(source.SelectedLineCount)),
                lines);
        }

        private static ResolvedAncillaryLine Line<TDetails>(
            string selectionId,
            ResolvedSelection<TDetails> line,
            AncillaryProfile profile,
            Func<TDetails, ResolvedAncillaryDetails> details)
            => new(
                line.OfferItemId,
                profile,
                line.VariantCode,
                line.SelectionScope,
                new ResolvedAncillaryUnit(line.Unit.TravellerRef, line.Unit.PortionRef, line.Unit.FlightRef),
                new ResolvedAncillaryCoverage(line.Coverage.TravellerRefs, line.Coverage.PortionRefs, line.Coverage.FlightRefs),
                new ResolvedAncillaryDefinition(
                    line.Definition.DefinitionVersionId,
                    line.Definition.ServiceDefinitionRef,
                    line.Definition.VariantCode,
                    line.Definition.ServiceTypeCode,
                    line.Definition.ServiceSubCode,
                    line.Definition.Rfic,
                    line.Definition.Rfisc,
                    line.Definition.SsrCode),
                new ResolvedAncillaryProvision(
                    line.Provision.ProvisionId,
                    line.Provision.CoverageScope,
                    line.Provision.PurchaseStage,
                    line.Provision.BookingMethod,
                    line.Provision.BookingRequired,
                    line.Provision.ConfirmationRequirement,
                    line.Provision.FulfillmentProviderKey,
                    line.Provision.DocumentRouting,
                    line.Provision.DocumentType,
                    line.Provision.DocumentRequired,
                    line.Provision.RequiresCheckAtReserve),
                new ResolvedAncillaryPricing(
                    line.Pricing.PricingId,
                    line.Pricing.PricingRateId,
                    line.Pricing.PricingVersion,
                    line.Pricing.CurrencyId,
                    line.Pricing.Quantity,
                    line.Pricing.UnitAmount,
                    line.Pricing.TotalAmount,
                    line.Pricing.PricingUnit,
                    line.Pricing.BaseAmount,
                    line.Pricing.IsUnitTotalComplete ?? throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionId, nameof(line.Pricing.IsUnitTotalComplete)),
                    line.Pricing.TaxLines.Select(PriceLine).ToList(),
                    line.Pricing.FeeLines.Select(PriceLine).ToList()),
                line.Selected.Quantity,
                details(line.Details ?? throw ExceptionFactory.AncillarySourceContractIsIncomplete(selectionId, nameof(line.Details))));

        private static ResolvedAncillaryPriceLine PriceLine(PriceLine line)
            => new(line.Code, line.Amount, line.CurrencyId, line.CountryId, line.StationAirportId, line.TaxTreatment, line.FeeApplicationUnit);

        private static ResolvedDimensions? Dimensions(Wire.Dimensions? dimensions)
            => dimensions is null ? null : new ResolvedDimensions(dimensions.LengthCm, dimensions.WidthCm, dimensions.HeightCm);

        private static ResolvedBaggageDetails Baggage(BaggageDetails details, BaggageOptions? options)
            => new(
                details.ChargeKind,
                details.AllowanceConcept,
                details.PackageWeightKg,
                details.WeightFromExclusiveKg,
                details.WeightToInclusiveKg,
                details.EquipmentKind,
                options?.BagRef,
                options?.MeasuredWeightKg,
                Dimensions(options?.Dimensions),
                options?.WeightKg);

        private static ResolvedMealDetails Meal(MealDetails details)
            => new(details.MealKind, details.MealCode, details.MenuItemRef, details.DietaryCode, details.ExclusiveMealFamilyCode);

        private static ResolvedPetDetails Pet(PetDetails details, PetOptions? options)
            => new(
                details.TransportMode,
                options?.AnimalType,
                options?.OtherAnimalCode,
                options?.AnimalAgeWeeks,
                options?.CombinedWeightKg,
                Dimensions(options?.CarrierDimensions),
                options?.DocumentAcknowledgements ?? [],
                options?.SizeBracket);

        private static ResolvedAssistanceDetails Assistance(ResolvedSelection<AssistedTravelDetails> line, AssistedTravelDetails details)
        {
            if (line.VariantCode is AncillaryVariant.MedicalEquipment or AncillaryVariant.UnaccompaniedMinor)
                throw ExceptionFactory.AncillaryVariantSourceIsIncomplete(line.OfferItemId, line.VariantCode);

            var options = line.Selected.Options?.AssistedTravel;

            return new ResolvedAssistanceDetails(
                details.AssistanceKind,
                options?.AssistanceSsrCode,
                options?.InfantRef,
                options?.GuardianRef,
                MedicalEquipment: null,
                UnaccompaniedMinor: null);
        }

        private static ResolvedAirportServiceDetails AirportService(AirportServiceDetails details, AirportServiceOptions? options)
            => new(details.Kind, details.AirportId, details.TerminalRef, details.FacilityId, details.Direction, options?.TimeWithOffset, options?.GuestCount);

        private static ResolvedPriorityDetails Priority(PriorityDetails details, PriorityOptions? options)
            => new(details.Kind, details.PriorityZoneCode, details.PriorityGroupCode, details.FareBenefitRef, options?.OptIn);

        private static ResolvedConnectivityDetails Connectivity(ConnectivityDetails details, ConnectivityOptions? options)
            => new(details.PlanKind, details.DurationMinutes, details.IncludedDataMb, details.MaxDevices, details.DeliveryStage, options?.DeviceCount);

        private static ResolvedUpgradeDetails Upgrade(UpgradeDetails details)
            => new(details.FromCabinId, details.ToCabinId, details.AllowedUpgradeKind, details.RequiresTicketExchange);
    }
}
