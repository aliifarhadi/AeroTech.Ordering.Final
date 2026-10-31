using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AeroTech.Ordering.Domain.Providers.Ancillary;
using AeroTech.Ordering.Domain._Shared.Resources;
using AeroTech.Ordering.Providers.Ancillary.Wire;

namespace AeroTech.Ordering.Providers.Ancillary.Services
{
    public sealed class AncillaryProvider : IAncillaryProvider
    {
        private const string ResolveRoute = "v1/AncillaryOffers/Resolve";
        private const string ResolveSeatsRoute = "v1/Ancillaries/SeatMaps/Resolve";
        private const int RetiredSeatRateCode = 16811;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly HttpClient _httpClient;

        public AncillaryProvider(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<ResolvedAncillarySelection> ResolveSelectionAsync(string selectionId, CancellationToken cancellationToken = default)
            => ResolvedSelectionsMapper.ToDomain(
                selectionId,
                await ResolveAsync<ResolvedSelectionsResponse>(ResolveRoute, selectionId, cancellationToken));

        public async Task<ResolvedSeatQuote> ResolveSeatSelectionAsync(string selectionId, CancellationToken cancellationToken = default)
            => ResolvedSeatsMapper.ToDomain(
                selectionId,
                await ResolveAsync<ResolvedSeatsResponse>(ResolveSeatsRoute, selectionId, cancellationToken));

        private async Task<TResponse> ResolveAsync<TResponse>(string route, string selectionId, CancellationToken cancellationToken)
            where TResponse : class
        {
            using var response = await _httpClient.PostAsJsonAsync(
                route,
                new { SelectionId = selectionId },
                JsonOptions,
                cancellationToken);

            var envelope = await response.Content
                .ReadFromJsonAsync<AncillaryEnvelope<TResponse>>(JsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode && envelope?.Data is { } data)
                return data;

            var error = envelope?.Errors?.FirstOrDefault();

            throw response.StatusCode switch
            {
                HttpStatusCode.Gone => ExceptionFactory.AncillarySelectionExpiredAtSource(selectionId),
                HttpStatusCode.Conflict => ExceptionFactory.AncillarySelectionIsNoLongerSellable(selectionId),
                HttpStatusCode.NotFound when error?.Code == RetiredSeatRateCode => ExceptionFactory.AncillarySelectionIsNoLongerSellable(selectionId),
                HttpStatusCode.NotFound => ExceptionFactory.AncillarySelectionIsNotFound(selectionId),
                _ => ExceptionFactory.AncillarySelectionCouldNotBeResolved(error?.Detail ?? error?.Title, selectionId)
            };
        }
    }
}
