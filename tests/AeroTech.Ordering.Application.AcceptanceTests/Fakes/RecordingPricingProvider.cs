using AeroTech.Ordering.Domain.Providers.Pricing;

namespace AeroTech.Ordering.Application.AcceptanceTests.Fakes;

public sealed class RecordingPricingProvider : IPricingProvider
{
    public List<AirFareBoundReservationValidationRequest> Requests { get; } = [];

    public DateTimeOffset TimeLimit { get; set; } = new(2026, 10, 2, 10, 0, 0, TimeSpan.Zero);

    public Task<AirFareBoundReservationValidationResult> ReservationValidationAsync(
        AirFareBoundReservationValidationRequest request,
        CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(new AirFareBoundReservationValidationResult(TimeLimit));
    }
}
