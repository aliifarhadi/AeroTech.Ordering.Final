using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Domain.Providers.Reservation
{
    public sealed record ReservationCapability(
        ReservationMode Mode,
        BatchResultMode BatchResultMode,
        ReservationActionScope PreConfirmationReleaseScope,
        ReservationActionScope PostConfirmationCancelScope,
        bool SupportsExtend,
        bool SupportsSplit,
        bool ProvidesUnitReference,
        bool SupportsReadBack,
        bool ExpiresAutomatically);
}
