using AeroTech.Ordering.ReferenceData.Core;
using AeroTech.Ordering.ReferenceData.Core.Wire;
using AeroTech.Ordering.ReferenceData.Persistence;
using AeroTech.Ordering.ReferenceData.ReadModels;

namespace AeroTech.Ordering.ReferenceData.Syncing
{
    public sealed class TravelAgencyOfficeSyncer : ReferenceSyncerBase<TravelAgencyOfficeReadModel, TravelAgencyOfficeDto, long>
    {
        private readonly ICoreClient _client;

        public TravelAgencyOfficeSyncer(ReferenceDbContext db, ICoreClient client, TimeProvider timeProvider)
            : base(db, timeProvider) => _client = client;

        protected override string Resource => "TravelAgencyOffices";

        protected override Task<List<TravelAgencyOfficeDto>> FetchAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken)
            => _client.GetTravelAgencyOfficesAsync(modifiedAfter, cancellationToken);

        protected override TravelAgencyOfficeReadModel CreateNew(TravelAgencyOfficeDto dto)
        {
            var model = new TravelAgencyOfficeReadModel { Id = dto.Id };
            ApplyChanges(dto, model);
            return model;
        }

        protected override void ApplyChanges(TravelAgencyOfficeDto dto, TravelAgencyOfficeReadModel model)
        {
            model.TravelAgencyId = dto.TravelAgencyId;
            model.TravelAgencyLegalName = dto.TravelAgencyLegalName;
            model.ParentOfficeId = dto.ParentOfficeId;
            model.ParentOfficeName = dto.ParentOfficeName;
            model.Code = dto.Code;
            model.Name = dto.Name;
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
