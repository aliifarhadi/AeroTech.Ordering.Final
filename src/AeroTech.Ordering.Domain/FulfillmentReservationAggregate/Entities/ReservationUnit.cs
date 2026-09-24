using AeroTech.Framework.Core.Domain.Entities;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Domain._Shared.Resources;

namespace AeroTech.Ordering.Domain.FulfillmentReservationAggregate.Entities
{
    public sealed class ReservationUnit : Entity<long>
    {
        private readonly List<long> _orderServiceIds = new();

        private ReservationUnit()
        {
        }

        internal ReservationUnit(
            long id,
            long fulfillmentReservationId,
            string unitCorrelationKey,
            IReadOnlyCollection<long> orderServiceIds)
        {
            if (orderServiceIds.Count == 0)
                throw ExceptionFactory.ReservationUnitMustCoverServices();

            Id = id;
            FulfillmentReservationId = fulfillmentReservationId;
            UnitCorrelationKey = unitCorrelationKey;
            Status = ReservationMemberStatus.Pending;
            _orderServiceIds.AddRange(orderServiceIds);
        }

        public long FulfillmentReservationId { get; private set; }

        public string UnitCorrelationKey { get; private set; } = default!;

        public ReservationMemberStatus Status { get; private set; }

        public string? ProviderUnitRef { get; private set; }

        public string? RawStatusCode { get; private set; }

        public string? ObservedSeat { get; private set; }

        public IReadOnlyCollection<long> OrderServiceIds => _orderServiceIds.AsReadOnly();

        internal void Record(
            ReservationMemberStatus status,
            string? providerUnitRef,
            string? rawStatusCode,
            string? observedSeat)
        {
            if (providerUnitRef is not null)
            {
                if (ProviderUnitRef is not null && ProviderUnitRef != providerUnitRef)
                    throw ExceptionFactory.ProviderReferenceCannotChange(nameof(ReservationUnit), Id, ProviderUnitRef, providerUnitRef);

                ProviderUnitRef = providerUnitRef;
            }

            Status = status;
            RawStatusCode = rawStatusCode ?? RawStatusCode;
            ObservedSeat = observedSeat ?? ObservedSeat;
        }

        internal void Mark(ReservationMemberStatus status) => Status = status;
    }
}
