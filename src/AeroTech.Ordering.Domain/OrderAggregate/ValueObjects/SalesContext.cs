using AeroTech.Framework.Core.Domain.ValueObjects;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.OrderAggregate.ValueObjects
{
    public sealed class SalesContext : ValueObject
    {
        private SalesContext()
        {
        }

        public SalesContext(
            SalesChannel channel,
            CallerContextType contextType,
            CallerPrincipalType principalType,
            long actorId,
            long? airlineUserId,
            long? travelAgencyId,
            long? travelAgencyUserId,
            long? individualId,
            long? partnerApiAccessProfileId,
            SellingOfficeKind? officeKind,
            long? officeId)
        {
            if (actorId <= 0)
                throw ExceptionFactory.SalesContextActorIsRequired();

            if (officeKind.HasValue != officeId.HasValue)
                throw ExceptionFactory.SellingOfficeIsIncomplete();

            Channel = channel;
            ContextType = contextType;
            PrincipalType = principalType;
            ActorId = actorId;
            AirlineUserId = airlineUserId;
            TravelAgencyId = travelAgencyId;
            TravelAgencyUserId = travelAgencyUserId;
            IndividualId = individualId;
            PartnerApiAccessProfileId = partnerApiAccessProfileId;
            OfficeKind = officeKind;
            OfficeId = officeId;
        }

        public SalesChannel Channel { get; private set; }

        public CallerContextType ContextType { get; private set; }

        public CallerPrincipalType PrincipalType { get; private set; }

        public long ActorId { get; private set; }

        public long? AirlineUserId { get; private set; }

        public long? TravelAgencyId { get; private set; }

        public long? TravelAgencyUserId { get; private set; }

        public long? IndividualId { get; private set; }

        public long? PartnerApiAccessProfileId { get; private set; }

        public SellingOfficeKind? OfficeKind { get; private set; }

        public long? OfficeId { get; private set; }

        public SalesContext Copy()
            => new(
                Channel,
                ContextType,
                PrincipalType,
                ActorId,
                AirlineUserId,
                TravelAgencyId,
                TravelAgencyUserId,
                IndividualId,
                PartnerApiAccessProfileId,
                OfficeKind,
                OfficeId);

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Channel;
            yield return ContextType;
            yield return PrincipalType;
            yield return ActorId;
            yield return AirlineUserId;
            yield return TravelAgencyId;
            yield return TravelAgencyUserId;
            yield return IndividualId;
            yield return PartnerApiAccessProfileId;
            yield return OfficeKind;
            yield return OfficeId;
        }
    }
}
