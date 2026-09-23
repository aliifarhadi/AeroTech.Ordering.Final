using AeroTech.Ordering.ReferenceData.AirInfo;
using AeroTech.Ordering.ReferenceData.AirInfo.Wire;
using AeroTech.Ordering.ReferenceData.Configuration;
using AeroTech.Ordering.ReferenceData.Persistence;
using AeroTech.Ordering.ReferenceData.ReadModels;
using Microsoft.Extensions.Options;

namespace AeroTech.Ordering.ReferenceData.Syncing
{
    public sealed class CountrySyncer : ReferenceSyncerBase<CountryReadModel, CountryDto, int>
    {
        private readonly IAirInfoClient _client;
        private readonly string _primaryLanguage;

        public CountrySyncer(ReferenceDbContext db, IAirInfoClient client, IOptions<ReferenceDataOptions> options, TimeProvider timeProvider)
            : base(db, timeProvider)
        {
            _client = client;
            _primaryLanguage = options.Value.PrimaryLanguage;
        }

        protected override string Resource => "Countries";

        protected override Task<List<CountryDto>> FetchAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken)
            => _client.GetCountriesAsync(modifiedAfter, cancellationToken);

        protected override CountryReadModel CreateNew(CountryDto dto)
        {
            var model = new CountryReadModel { Id = dto.Id };
            ApplyChanges(dto, model);
            return model;
        }

        protected override void ApplyChanges(CountryDto dto, CountryReadModel model)
        {
            model.Alpha2Code = dto.Alpha2Code;
            model.Alpha3Code = dto.Alpha3Code;
            model.NumericCode = dto.NumericCode;
            model.DisplayName = DisplayNameSelector.Pick(dto.DisplayNames, _primaryLanguage);
            model.PhoneCode = dto.PhoneCode;
            model.LastUpdateTime = dto.LastUpdateTime;
        }
    }
}
