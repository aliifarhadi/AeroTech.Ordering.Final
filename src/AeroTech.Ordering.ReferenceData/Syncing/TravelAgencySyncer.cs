using AeroTech.Ordering.ReferenceData.Core;
using AeroTech.Ordering.ReferenceData.Core.Wire;
using AeroTech.Ordering.ReferenceData.Persistence;
using AeroTech.Ordering.ReferenceData.ReadModels;

namespace AeroTech.Ordering.ReferenceData.Syncing
{
    public sealed class TravelAgencySyncer : ReferenceSyncerBase<TravelAgencyReadModel, TravelAgencyDto, long>
    {
        private readonly ICoreClient _client;

        public TravelAgencySyncer(ReferenceDbContext db, ICoreClient client, TimeProvider timeProvider)
            : base(db, timeProvider) => _client = client;

        protected override string Resource => "TravelAgencies";

        protected override Task<List<TravelAgencyDto>> FetchAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken)
            => _client.GetTravelAgenciesAsync(modifiedAfter, cancellationToken);

        protected override TravelAgencyReadModel CreateNew(TravelAgencyDto dto)
        {
            var model = new TravelAgencyReadModel { Id = dto.Id };
            ApplyChanges(dto, model);
            return model;
        }

        protected override void ApplyChanges(TravelAgencyDto dto, TravelAgencyReadModel model)
        {
            model.Code = dto.Code;
            model.LegalName = dto.LegalName;
            model.TradingName = dto.TradingName;
            model.ParentAgencyId = dto.ParentAgencyId;
            model.ParentAgencyLegalName = dto.ParentAgencyLegalName;
            model.CountryId = dto.CountryId;
            model.PreferredCurrencyId = dto.PreferredCurrencyId;
            model.PreferredLanguageCode = dto.PreferredLanguageCode;
            model.Status = dto.Status;
            model.OnboardedAt = dto.OnboardedAt;
            model.TerminatedAt = dto.TerminatedAt;
            model.TerminationReasonCode = dto.TerminationReasonCode;
            model.PrimaryAccreditationTypeCode = dto.PrimaryAccreditationTypeCode;
            model.PrimaryAccreditationIdentifier = dto.PrimaryAccreditationIdentifier;
            model.LastUpdateTime = dto.LastUpdateTime;
        }
    }
}
