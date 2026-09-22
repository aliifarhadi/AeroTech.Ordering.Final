using AeroTech.Ordering.ReferenceData.Core;
using AeroTech.Ordering.ReferenceData.Core.Wire;
using AeroTech.Ordering.ReferenceData.Persistence;
using AeroTech.Ordering.ReferenceData.ReadModels;

namespace AeroTech.Ordering.ReferenceData.Syncing
{
    public sealed class CustomerSyncer : ReferenceSyncerBase<CustomerReadModel, CustomerDto, long>
    {
        private readonly ICoreClient _client;

        public CustomerSyncer(ReferenceDbContext db, ICoreClient client, TimeProvider timeProvider)
            : base(db, timeProvider) => _client = client;

        protected override string Resource => "Customers";

        protected override Task<List<CustomerDto>> FetchAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken)
            => _client.GetCustomersAsync(modifiedAfter, cancellationToken);

        protected override CustomerReadModel CreateNew(CustomerDto dto)
        {
            var model = new CustomerReadModel { Id = dto.Id };
            ApplyChanges(dto, model);
            return model;
        }

        protected override void ApplyChanges(CustomerDto dto, CustomerReadModel model)
        {
            model.CustomerNumber = dto.CustomerNumber;
            model.Type = dto.CustomerType;
            model.IndividualId = dto.IndividualId;
            model.TravelAgencyId = dto.TravelAgencyId;
            model.OrganizationId = dto.OrganizationId;
            model.SubjectId = dto.SubjectId;
            model.SubjectName = dto.SubjectName;
            model.Status = dto.Status;
            model.RelationshipStartedOn = dto.RelationshipStartedOn;
            model.RelationshipEndedOn = dto.RelationshipEndedOn;
            model.PreferredCurrencyId = dto.PreferredCurrencyId;
            model.PreferredLanguageCode = dto.PreferredLanguageCode;
            model.SuspensionReasonCode = dto.SuspensionReasonCode;
            model.ClosureReasonCode = dto.ClosureReasonCode;
            model.LastUpdateTime = dto.LastUpdateTime;
        }
    }
}
