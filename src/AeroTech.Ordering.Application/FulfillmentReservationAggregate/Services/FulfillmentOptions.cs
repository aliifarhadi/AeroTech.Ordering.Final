namespace AeroTech.Ordering.Application.FulfillmentReservationAggregate.Services
{
    public sealed class FulfillmentOptions
    {
        public const string SectionName = "Fulfillment";

        public int LockExpirySeconds { get; set; }
    }
}
