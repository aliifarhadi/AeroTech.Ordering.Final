namespace AeroTech.Ordering.Query._Shared.Authorization
{
    public readonly record struct OrderQueryScope(long? CustomerId)
    {
        public static readonly OrderQueryScope Unrestricted = new((long?)null);

        public static OrderQueryScope OwnedBy(long customerId) => new(customerId);
    }
}
