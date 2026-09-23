using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Application._Shared.Caller
{
    public sealed class SalesContextFactory : ISalesContextFactory
    {
        private readonly ICallerContext _callerContext;

        public SalesContextFactory(ICallerContext callerContext) => _callerContext = callerContext;

        public SalesContext Create(SalesChannel channel)
        {
            var (officeKind, officeId) = ResolveSellingOffice();

            return new SalesContext(
                channel,
                _callerContext.ContextType,
                _callerContext.PrincipalType,
                _callerContext.ActorId,
                _callerContext.AirlineUserId,
                _callerContext.TravelAgencyId,
                _callerContext.TravelAgencyUserId,
                _callerContext.IndividualId,
                _callerContext.PartnerApiAccessProfileId,
                officeKind,
                officeId);
        }

        private (SellingOfficeKind? Kind, long? Id) ResolveSellingOffice()
        {
            if (_callerContext.AirlineOfficeId is { } airlineOfficeId)
                return (SellingOfficeKind.AirlineOffice, airlineOfficeId);

            var travelAgencyOfficeIds = _callerContext.TravelAgencyOfficeIds;

            if (travelAgencyOfficeIds.Count > 1)
                throw ExceptionFactory.SellingOfficeIsAmbiguous(travelAgencyOfficeIds.Count);

            return travelAgencyOfficeIds.Count == 1
                ? (SellingOfficeKind.TravelAgencyOffice, travelAgencyOfficeIds.Single())
                : (null, null);
        }
    }
}
