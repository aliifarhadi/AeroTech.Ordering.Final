using AeroTech.Ordering.Domain.OrderAggregate.Contracts;
using AeroTech.Ordering.Query._Shared.DbContexts;
using AeroTech.Ordering.Query.OrderAggregate.Models;
using Microsoft.EntityFrameworkCore;

namespace AeroTech.Ordering.Synchronizer.OrderAggregate
{
    public sealed class OrderQueryDbSynchronizer : IOrderQueryDbSynchronizer
    {
        private readonly OrderQueryDbContext _dbContext;

        public OrderQueryDbSynchronizer(OrderQueryDbContext dbContext) => _dbContext = dbContext;

        public Task ProjectCreatedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        public Task ProjectRemarkedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        public Task ProjectReservationChangedAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken = default)
            => UpsertAsync(snapshot, cancellationToken);

        private async Task UpsertAsync(OrderReadModelSnapshot snapshot, CancellationToken cancellationToken)
        {
            var order = await _dbContext.Orders
                .FirstOrDefaultAsync(row => row.Id == snapshot.OrderId, cancellationToken);

            if (order is null)
                _dbContext.Orders.Add(Apply(snapshot, new OrderReadModel { Id = snapshot.OrderId }));
            else if (snapshot.OccurredAt <= order.LastProjectedAt)
                return;
            else
                Apply(snapshot, order);

            await ReconcileAsync(_dbContext.OrderTravellers, snapshot, snapshot.Travellers, traveller => traveller.TravellerId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderTravellerDocuments, snapshot, Documents(snapshot), document => document.Document.DocumentId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderJourneys, snapshot, snapshot.Journeys, journey => journey.JourneyId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderSegments, snapshot, snapshot.Segments, segment => segment.SegmentId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderItems, snapshot, snapshot.Items, item => item.ItemId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderServices, snapshot, snapshot.Services, service => service.ServiceId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderPricingLines, snapshot, snapshot.PricingLines, line => line.PricingLineId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderPricingAllocations, snapshot, Allocations(snapshot), allocation => allocation.Allocation.AllocationId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderContacts, snapshot, snapshot.Contacts, contact => contact.ContactId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderContactPoints, snapshot, ContactPoints(snapshot), point => point.ContactPoint.ContactPointId, Apply, cancellationToken);
            await ReconcileAsync(_dbContext.OrderRemarks, snapshot, snapshot.Remarks, remark => remark.RemarkId, Apply, cancellationToken);
        }

        private async Task ReconcileAsync<TReadModel, TSource>(
            DbSet<TReadModel> set,
            OrderReadModelSnapshot snapshot,
            IReadOnlyList<TSource> sources,
            Func<TSource, long> identify,
            Action<TSource, TReadModel> apply,
            CancellationToken cancellationToken)
            where TReadModel : class, IOrderOwnedReadModel, new()
        {
            var stored = await set
                .Where(row => row.OrderId == snapshot.OrderId)
                .ToListAsync(cancellationToken);
            var storedById = stored.ToDictionary(row => row.Id);

            foreach (var source in sources)
            {
                var id = identify(source);

                if (storedById.TryGetValue(id, out var row))
                {
                    apply(source, row);
                    continue;
                }

                row = new TReadModel { Id = id, OrderId = snapshot.OrderId };
                apply(source, row);
                set.Add(row);
            }

            var incomingIds = sources.Select(identify).ToHashSet();

            foreach (var row in stored.Where(row => !incomingIds.Contains(row.Id)))
                set.Remove(row);
        }

        private static IReadOnlyList<OwnedDocument> Documents(OrderReadModelSnapshot snapshot)
            => snapshot.Travellers
                .SelectMany(traveller => traveller.Documents.Select(document => new OwnedDocument(traveller.TravellerId, document)))
                .ToList();

        private static IReadOnlyList<OwnedAllocation> Allocations(OrderReadModelSnapshot snapshot)
            => snapshot.PricingLines
                .SelectMany(line => line.Allocations.Select(allocation => new OwnedAllocation(line.PricingLineId, allocation)))
                .ToList();

        private static IReadOnlyList<OwnedContactPoint> ContactPoints(OrderReadModelSnapshot snapshot)
            => snapshot.Contacts
                .SelectMany(contact => contact.ContactPoints.Select(point => new OwnedContactPoint(contact.ContactId, point)))
                .ToList();

        private static OrderReadModel Apply(OrderReadModelSnapshot snapshot, OrderReadModel target)
        {
            target.OrderReference = snapshot.OrderReference;
            target.RecordLocator = snapshot.RecordLocator;
            target.SourceOfferId = snapshot.SourceOfferId;
            target.Status = snapshot.Status;
            target.Channel = snapshot.Channel;
            target.CustomerId = snapshot.CustomerId;
            target.ActorId = snapshot.ActorId;
            target.TravelAgencyId = snapshot.TravelAgencyId;
            target.OfficeKind = snapshot.OfficeKind;
            target.OfficeId = snapshot.OfficeId;
            target.CurrencyId = snapshot.CurrencyId;
            target.CustomerTotal = snapshot.CustomerTotal;
            target.CommercialVersion = snapshot.CommercialVersion;
            target.LastTicketingDate = snapshot.LastTicketingDate;
            target.CreatedAt = snapshot.CreatedAt;
            target.LastProjectedAt = snapshot.OccurredAt;
            return target;
        }

        private static void Apply(OrderTravellerSnapshot source, OrderTravellerReadModel target)
        {
            target.Index = source.Index;
            target.SourceTravellerRef = source.SourceTravellerRef;
            target.PassengerType = source.PassengerType;
            target.AgeRange = source.AgeRange;
            target.InfantParentTravellerId = source.InfantParentTravellerId;
            target.Status = source.Status;
            target.GivenName = source.GivenName;
            target.Surname = source.Surname;
            target.DateOfBirth = source.DateOfBirth;
            target.Gender = source.Gender;
            target.NationalityId = source.NationalityId;
            target.CountryOfResidenceId = source.CountryOfResidenceId;
        }

        private static void Apply(OwnedDocument source, OrderTravellerDocumentReadModel target)
        {
            target.TravellerId = source.TravellerId;
            target.Type = source.Document.Type;
            target.Number = source.Document.Number;
            target.ExpiryDate = source.Document.ExpiryDate;
            target.IssuanceCountryId = source.Document.IssuanceCountryId;
            target.Holder = source.Document.Holder;
        }

        private static void Apply(OrderJourneySnapshot source, OrderJourneyReadModel target)
        {
            target.Sequence = source.Sequence;
            target.BoundId = source.BoundId;
            target.OriginAirportId = source.OriginAirportId;
            target.DestinationAirportId = source.DestinationAirportId;
        }

        private static void Apply(OrderSegmentSnapshot source, OrderSegmentReadModel target)
        {
            target.OrderJourneyId = source.JourneyId;
            target.Sequence = source.Sequence;
            target.FlightId = source.FlightId;
            target.FlightNumber = source.FlightNumber;
            target.MarketingAirlineId = source.MarketingAirlineId;
            target.OperatingAirlineId = source.OperatingAirlineId;
            target.OriginAirportId = source.OriginAirportId;
            target.DestinationAirportId = source.DestinationAirportId;
            target.SoldDeparture = source.SoldDeparture;
            target.SoldArrival = source.SoldArrival;
            target.Duration = source.Duration;
        }

        private static void Apply(OrderItemSnapshot source, OrderItemReadModel target)
        {
            target.Kind = source.Kind;
            target.AcceptedTotal = source.AcceptedTotal;
            target.CommercialStatus = source.CommercialStatus;
        }

        private static void Apply(OrderServiceSnapshot source, OrderServiceReadModel target)
        {
            target.OrderItemId = source.ItemId;
            target.TravellerId = source.TravellerId;
            target.SegmentId = source.SegmentId;
            target.ServiceType = source.ServiceType;
            target.CommercialStatus = source.CommercialStatus;
            target.BookingClass = source.BookingClass;
            target.FareBasis = source.FareBasis;
            target.FareFamily = source.FareFamily;
            target.FareType = source.FareType;
            target.CheckedBaggagePieces = source.CheckedBaggage?.Pieces;
            target.CheckedBaggageWeight = source.CheckedBaggage?.Weight;
            target.CheckedBaggageUnit = source.CheckedBaggage?.Unit;
            target.CabinBaggagePieces = source.CabinBaggage?.Pieces;
            target.CabinBaggageWeight = source.CabinBaggage?.Weight;
            target.CabinBaggageUnit = source.CabinBaggage?.Unit;
            target.IsRefundable = source.IsRefundable;
            target.IsChangeable = source.IsChangeable;
            target.IsUpgradable = source.IsUpgradable;
            target.SeatNumber = source.SeatNumber;
        }

        private static void Apply(OrderPricingLineSnapshot source, OrderPricingLineReadModel target)
        {
            target.Reason = source.Reason;
            target.Scope = source.Scope;
            target.Category = source.Category;
            target.SubCategory = source.SubCategory;
            target.Direction = source.Direction;
            target.Treatment = source.Treatment;
            target.Code = source.Code;
            target.Description = source.Description;
            target.Reference = source.Reference;
            target.Amount = source.Amount;
            target.CurrencyId = source.CurrencyId;
            target.EquivalentAmount = source.EquivalentAmount;
            target.EquivalentCurrencyId = source.EquivalentCurrencyId;
            target.Refundability = source.Refundability;
        }

        private static void Apply(OwnedAllocation source, OrderPricingAllocationReadModel target)
        {
            target.PricingLineId = source.PricingLineId;
            target.OrderItemId = source.Allocation.ItemId;
            target.OrderServiceId = source.Allocation.ServiceId;
            target.OrderJourneyId = source.Allocation.JourneyId;
            target.OrderSegmentId = source.Allocation.SegmentId;
            target.TravellerId = source.Allocation.TravellerId;
            target.Amount = source.Allocation.Amount;
            target.CurrencyId = source.Allocation.CurrencyId;
            target.EquivalentAmount = source.Allocation.EquivalentAmount;
            target.EquivalentCurrencyId = source.Allocation.EquivalentCurrencyId;
        }

        private static void Apply(OrderContactSnapshot source, OrderContactReadModel target)
        {
            target.Sequence = source.Sequence;
            target.Role = source.Role;
            target.ContactName = source.ContactName;
        }

        private static void Apply(OwnedContactPoint source, OrderContactPointReadModel target)
        {
            target.OrderContactId = source.ContactId;
            target.Type = source.ContactPoint.Type;
            target.Value = source.ContactPoint.Value;
            target.CountryCode = source.ContactPoint.CountryCode;
            target.IsPrimary = source.ContactPoint.IsPrimary;
        }

        private static void Apply(OrderRemarkSnapshot source, OrderRemarkReadModel target)
        {
            target.Type = source.Type;
            target.Visibility = source.Visibility;
            target.Scope = source.Scope;
            target.TravellerId = source.TravellerId;
            target.SegmentId = source.SegmentId;
            target.OrderItemId = source.ItemId;
            target.OrderServiceId = source.ServiceId;
            target.Text = source.Text;
            target.CategoryCode = source.CategoryCode;
            target.IsPrintedOnItinerary = source.IsPrintedOnItinerary;
            target.IsPrintedOnInvoice = source.IsPrintedOnInvoice;
            target.Status = source.Status;
            target.SupersedesRemarkId = source.SupersedesRemarkId;
            target.CreatedBy = source.CreatedBy;
            target.CreatedAt = source.CreatedAt;
        }

        private sealed record OwnedDocument(long TravellerId, OrderTravellerDocumentSnapshot Document);

        private sealed record OwnedAllocation(long PricingLineId, OrderPricingAllocationSnapshot Allocation);

        private sealed record OwnedContactPoint(long ContactId, OrderContactPointSnapshot ContactPoint);
    }
}
