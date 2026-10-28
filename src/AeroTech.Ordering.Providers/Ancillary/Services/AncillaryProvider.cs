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

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly HttpClient _httpClient;

        public AncillaryProvider(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<ResolvedAncillarySelection> ResolveSelectionAsync(string selectionId, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                ResolveRoute,
                new { SelectionId = selectionId },
                JsonOptions,
                cancellationToken);

            var envelope = await response.Content
                .ReadFromJsonAsync<AncillaryEnvelope<ResolvedSelectionsResponse>>(JsonOptions, cancellationToken);

            if (response.IsSuccessStatusCode && envelope?.Data is { } data)
                return ResolvedSelectionsMapper.ToDomain(selectionId, data);

            throw response.StatusCode switch
            {
                HttpStatusCode.Gone => ExceptionFactory.AncillarySelectionExpiredAtSource(selectionId),
                HttpStatusCode.Conflict => ExceptionFactory.AncillarySelectionIsNoLongerSellable(selectionId),
                HttpStatusCode.NotFound => ExceptionFactory.AncillarySelectionIsNotFound(selectionId),
                _ => ExceptionFactory.AncillarySelectionCouldNotBeResolved(FirstError(envelope), selectionId)
            };
        }

        private static string? FirstError(AncillaryEnvelope<ResolvedSelectionsResponse>? envelope)
        {
            var error = envelope?.Errors?.FirstOrDefault();
            return error is null ? null : error.Detail ?? error.Title;
        }
    }
}
