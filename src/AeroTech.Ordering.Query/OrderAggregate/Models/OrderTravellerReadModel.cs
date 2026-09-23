using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderTravellerReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public int Index { get; set; }

        public string? SourceTravellerRef { get; set; }

        public PassengerTypeCode PassengerType { get; set; }

        public AgeRange AgeRange { get; set; }

        public long? InfantParentTravellerId { get; set; }

        public OrderTravellerStatus Status { get; set; }

        public string GivenName { get; set; } = default!;

        public string? Surname { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        public Gender? Gender { get; set; }

        public int? NationalityId { get; set; }

        public int? CountryOfResidenceId { get; set; }
    }
}
