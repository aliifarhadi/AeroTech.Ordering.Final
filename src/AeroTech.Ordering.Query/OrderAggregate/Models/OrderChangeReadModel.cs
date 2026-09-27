using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Domain._Shared.Contracts;

namespace AeroTech.Ordering.Query.OrderAggregate.Models
{
    public sealed class OrderChangeReadModel : IOrderOwnedReadModel
    {
        public long Id { get; set; }

        public long OrderId { get; set; }

        public OrderChangeType ChangeType { get; set; }

        public int CommercialVersion { get; set; }

        public long ActorId { get; set; }

        public SalesChannel Channel { get; set; }

        public CallerContextType ContextType { get; set; }

        public CallerPrincipalType PrincipalType { get; set; }

        public string? SourceReference { get; set; }

        public string? SourceSystem { get; set; }

        public string? ReasonCode { get; set; }

        public bool IsInvoluntary { get; set; }

        public string? WaiverCode { get; set; }

        public DateTimeOffset CommittedAt { get; set; }
    }
}
