using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AeroTech.Ordering.ReferenceData.Core.Wire;

namespace AeroTech.Ordering.ReferenceData.Core
{
    public sealed class CoreClient : ICoreClient
    {
        private const string CustomersRoute = "v1/Customers";
        private const string OperatorSettingsRoute = "v1/OperatorSettings";
        private const string AirlineOfficesRoute = "v1/AirlineOffices";
        private const string TravelAgenciesRoute = "v1/TravelAgencies";
        private const string TravelAgencyOfficesRoute = "v1/TravelAgencyOffices";

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
            Converters = { new JsonStringEnumConverter() }
        };

        private readonly HttpClient _httpClient;

        public CoreClient(HttpClient httpClient) => _httpClient = httpClient;

        public Task<List<CustomerDto>> GetCustomersAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken = default)
            => GetAsync<CustomerDto>(CustomersRoute, modifiedAfter, cancellationToken);

        public Task<List<OperatorSettingsDto>> GetOperatorSettingsAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken = default)
            => GetAsync<OperatorSettingsDto>(OperatorSettingsRoute, modifiedAfter, cancellationToken);

        public Task<List<AirlineOfficeDto>> GetAirlineOfficesAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken = default)
            => GetAsync<AirlineOfficeDto>(AirlineOfficesRoute, modifiedAfter, cancellationToken);

        public Task<List<TravelAgencyDto>> GetTravelAgenciesAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken = default)
            => GetAsync<TravelAgencyDto>(TravelAgenciesRoute, modifiedAfter, cancellationToken);

        public Task<List<TravelAgencyOfficeDto>> GetTravelAgencyOfficesAsync(DateTimeOffset? modifiedAfter, CancellationToken cancellationToken = default)
            => GetAsync<TravelAgencyOfficeDto>(TravelAgencyOfficesRoute, modifiedAfter, cancellationToken);

        private async Task<List<T>> GetAsync<T>(string route, DateTimeOffset? modifiedAfter, CancellationToken cancellationToken)
        {
            var url = modifiedAfter is { } value
                ? $"{route}?modifiedAfter={Uri.EscapeDataString(value.ToString("o"))}"
                : route;

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            var envelope = await response.Content.ReadFromJsonAsync<CoreEnvelope<List<T>>>(JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode || envelope?.Data is null)
                throw new CoreRequestException(FirstError(envelope) ?? $"Core request '{route}' failed with status {(int)response.StatusCode}.");

            return envelope.Data;
        }

        private static string? FirstError<T>(CoreEnvelope<T>? envelope)
        {
            var error = envelope?.Errors?.FirstOrDefault();
            return error is null ? null : error.Detail ?? error.Title;
        }
    }
}
