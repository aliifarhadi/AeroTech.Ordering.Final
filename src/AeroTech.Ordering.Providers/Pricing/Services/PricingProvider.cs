using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AeroTech.Ordering.Domain.Providers.Pricing;
using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.Providers.Pricing.Wire;

namespace AeroTech.Ordering.Providers.Pricing.Services
{
    public sealed class PricingProvider : IPricingProvider
    {
        private const string ReservationValidationRoute = "v1/BoundReservationValidation";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        private readonly HttpClient _httpClient;

        public PricingProvider(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<AirFareBoundReservationValidationResult> ReservationValidationAsync(
            AirFareBoundReservationValidationRequest request,
            CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsJsonAsync(ReservationValidationRoute, request, JsonOptions, cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw ExceptionFactory.FareReservationCouldNotBeValidated("The fare reservation validation request timed out.");
            }
            catch (HttpRequestException exception)
            {
                throw ExceptionFactory.FareReservationCouldNotBeValidated($"The fare reservation validation request failed to reach AirPrice. {exception.Message}");
            }

            using (response)
            {
                var envelope = await ReadEnvelopeAsync(response, cancellationToken);

                if (response.IsSuccessStatusCode && envelope?.Data is { } result)
                    return result;

                if (response.StatusCode == HttpStatusCode.BadRequest && envelope?.Errors?.FirstOrDefault(error => error.Code > 0) is { } rejection)
                    throw ExceptionFactory.FareReservationIsNotPermitted(rejection.Detail ?? rejection.Title);

                throw ExceptionFactory.FareReservationCouldNotBeValidated(FirstError(envelope));
            }
        }

        private static async Task<PricingEnvelope<AirFareBoundReservationValidationResult>?> ReadEnvelopeAsync(
            HttpResponseMessage response,
            CancellationToken cancellationToken)
        {
            try
            {
                return await response.Content.ReadFromJsonAsync<PricingEnvelope<AirFareBoundReservationValidationResult>>(JsonOptions, cancellationToken);
            }
            catch (JsonException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        private static string? FirstError(PricingEnvelope<AirFareBoundReservationValidationResult>? envelope)
        {
            var error = envelope?.Errors?.FirstOrDefault();
            return error is null ? null : error.Detail ?? error.Title;
        }
    }
}
