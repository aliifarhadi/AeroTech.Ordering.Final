namespace AeroTech.Ordering.Application.OrderAggregate.Commands.AddAncillaries
{
    public sealed record SelectedAncillariesInput(string SelectionId);

    public sealed record AncillaryServiceReference(string Type, string Id)
    {
        public const string AncillarySelection = "ancillary-selection";
    }

    public interface IAddAncillariesCommand
    {
        long OrderId { get; }

        int ExpectedCommercialVersion { get; }
    }

    public interface IAgencyAddAncillariesCommand : IAddAncillariesCommand
    {
        IReadOnlyList<AncillaryServiceReference> Services { get; }
    }
}
