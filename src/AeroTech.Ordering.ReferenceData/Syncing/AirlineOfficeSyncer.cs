using AeroTech.Ordering.ReferenceData.Core;
using AeroTech.Ordering.ReferenceData.Core.Wire;
using AeroTech.Ordering.ReferenceData.Persistence;
using AeroTech.Ordering.ReferenceData.ReadModels;

namespace AeroTech.Ordering.ReferenceData.Syncing
{
    public sealed class AirlineOfficeSyncer : ReferenceSyncerBase<AirlineOfficeReadModel, AirlineOfficeDto, long>
    {
        private readonly ICoreClient _client;

        public AirlineOfficeSyncer(ReferenceDbContext db, ICoreClient client, TimeProvider timeProvider)
            : base(db, timeProvider) => _client = client;

        protected override string Resource => "AirlineOffices";

        protected override Task<List<AirlineOfficeDto>> FetchAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken)
            => _client.GetAirlineOfficesAsync(modifiedAfter, cancellationToken);

        protected override AirlineOfficeReadModel CreateNew(AirlineOfficeDto dto)
        {
            var model = new AirlineOfficeReadModel { Id = dto.Id };
            ApplyChanges(dto, model);
            return model;
        }

        protected override void ApplyChanges(AirlineOfficeDto dto, AirlineOfficeReadModel model)
        {
            model.OrganisationUnitId = dto.OrganisationUnitId;
            model.OrganisationUnitName = dto.OrganisationUnitName;
            model.ParentOfficeId = dto.ParentOfficeId;
            model.ParentOfficeName = dto.ParentOfficeName;
            model.Code = dto.Code;
            model.Name = dto.Name;
            model.LegalEntityId = dto.LegalEntityId;
            model.LegalEntityLegalName = dto.LegalEntityLegalName;
            model.LegalEntityEffectiveFrom = dto.LegalEntityEffectiveFrom;
            model.CountryId = dto.CountryId;
            model.CityId = dto.CityId;
            model.AirportId = dto.AirportId;
            model.PointOfSaleCountryId = dto.PointOfSaleCountryId;
            model.PointOfSaleCityId = dto.PointOfSaleCityId;
            model.TimeZoneId = dto.TimeZoneId;
            model.Status = dto.Status;
            model.ValidFrom = dto.ValidFrom;
            model.ValidTo = dto.ValidTo;
            model.LastUpdateTime = dto.LastUpdateTime;
        }
    }
}
