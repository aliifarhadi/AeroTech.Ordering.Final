using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AeroTech.Ordering.Domain._Shared;
using AeroTech.Ordering.Domain.Providers.FlightFlow;
using AeroTech.Ordering.Providers.FlightFlow.Wire;
using AeroTech.Messages.Ordering.Enums;

namespace AeroTech.Ordering.Providers.FlightFlow.Services
{
    public sealed class FlightFlowProvider : IFlightFlowProvider
    {
        private const string SeatHoldsRoute = "v1/Flights/Seat-Holds";
        private const string SeatConfirmationsRoute = "v1/Flights/Seat-Confirmations";
        private const int CannotConfirmExpiredSeatHoldErrorCode = 1176;
        private const int CannotConfirmReleasedSeatHoldErrorCode = 1177;
        private const int CannotConfirmCancelledSeatHoldErrorCode = 1178;
        private const int CannotConfirmInconsistentSeatHoldErrorCode = 1179;
        private const int SeatHoldNotFoundForConfirmationErrorCode = 1180;
        private const int SeatHoldNotFoundForReleaseErrorCode = 1181;
        private const int CannotReleaseConfirmedSeatHoldErrorCode = 1182;
        private const int CannotReleaseExpiredSeatHoldErrorCode = 1183;
        private const int CannotReleaseCancelledSeatHoldErrorCode = 1184;
        private const int CannotReleaseInconsistentSeatHoldErrorCode = 1185;
        private const int SeatHoldNotFoundForCancellationErrorCode = 1186;
        private const int CannotCancelHeldSeatHoldErrorCode = 1187;
        private const int CannotCancelReleasedSeatHoldErrorCode = 1188;
        private const int CannotCancelExpiredSeatHoldErrorCode = 1189;
        private const int CannotCancelInconsistentSeatHoldErrorCode = 1190;

        private readonly HttpClient _httpClient;

        public FlightFlowProvider(HttpClient httpClient) => _httpClient = httpClient;

        public async Task<FlightFlowReply<FlightHeldSeatsResult>> CreateHoldAsync(HoldSeatsRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;
            string body;

            try
            {
                response = await _httpClient.PostAsJsonAsync(SeatHoldsRoute, request, FlightFlowJson.Options, cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Indeterminate,
                    FulfillmentFailureReason.UnknownOutcome,
                    "The seat hold request timed out.");
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Retriable,
                    FulfillmentFailureReason.TechnicalFailed,
                    $"The seat hold request failed to reach FlightFlow. {exception.Message}");
            }

            using (response)
            {
                var envelope = Deserialize<FlightHeldSeatsResult>(body);

                if (!response.IsSuccessStatusCode)
                {
                    var (kind, reason) = Classify((int)response.StatusCode);
                    throw new ProviderRequestException(
                        kind,
                        reason,
                        FirstError(envelope) ?? $"The seat hold could not be created (HTTP {(int)response.StatusCode}): {Truncate(body)}",
                        (int)response.StatusCode,
                        body);
                }

                if (envelope?.Data is null)
                    throw new ProviderRequestException(
                        FulfillmentFailureKind.Indeterminate,
                        FulfillmentFailureReason.UnknownOutcome,
                        "The seat hold response contained no hold data.",
                        (int)response.StatusCode,
                        body);

                return new FlightFlowReply<FlightHeldSeatsResult>(envelope.Data, (int)response.StatusCode, body);
            }
        }

        public async Task ExtendHeldAsync(ExtendHeldSeatsRequest request, CancellationToken cancellationToken = default)
        {
            var route = $"{SeatHoldsRoute}/{request.HoldBatchId}";
            var payload = new { expiresAt = request.ExpiresAt };

            HttpResponseMessage response;
            string body;

            try
            {
                response = await _httpClient.PatchAsJsonAsync(route, payload, FlightFlowJson.Options, cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Indeterminate,
                    FulfillmentFailureReason.UnknownOutcome,
                    "The seat-hold extension timed out; the hold may or may not have been extended.");
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Retriable,
                    FulfillmentFailureReason.TechnicalFailed,
                    $"The seat-hold extension failed to reach FlightFlow. {exception.Message}");
            }

            using (response)
            {
                if (!response.IsSuccessStatusCode)
                {
                    var (kind, reason) = Classify((int)response.StatusCode);
                    throw new ProviderRequestException(
                        kind,
                        reason,
                        $"The hold batch '{request.HoldBatchId}' could not be extended (HTTP {(int)response.StatusCode}): {Truncate(body)}",
                        (int)response.StatusCode,
                        body);
                }
            }
        }

        public async Task<SplitHeldSeatsResult> SplitHeldAsync(SplitHeldSeatsRequest request, CancellationToken cancellationToken = default)
        {
            var route = $"{SeatHoldsRoute}/{request.HoldBatchId}/Splits";
            var payload = new
            {
                idempotencyKey = request.IdempotencyKey,
                reference = request.Reference,
                expiresAt = request.ExpiresAt,
                moves = request.SeatHoldReferences.Select(seatHoldReference => new { seatHoldReference }).ToArray()
            };

            HttpResponseMessage response;
            string body;

            try
            {
                response = await _httpClient.PostAsJsonAsync(route, payload, FlightFlowJson.Options, cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Indeterminate,
                    FulfillmentFailureReason.UnknownOutcome,
                    "The seat-hold split timed out; the seats may or may not have been split.");
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Retriable,
                    FulfillmentFailureReason.TechnicalFailed,
                    $"The seat-hold split failed to reach FlightFlow. {exception.Message}");
            }

            using (response)
            {
                var envelope = Deserialize<FlightSplitSeatsResponse>(body);

                if (!response.IsSuccessStatusCode)
                {
                    var (kind, reason) = Classify((int)response.StatusCode);
                    throw new ProviderRequestException(
                        kind,
                        reason,
                        FirstError(envelope) ?? $"The hold batch '{request.HoldBatchId}' could not be split (HTTP {(int)response.StatusCode}): {Truncate(body)}",
                        (int)response.StatusCode,
                        body);
                }

                if (envelope?.Data is null)
                    throw new ProviderRequestException(
                        FulfillmentFailureKind.Indeterminate,
                        FulfillmentFailureReason.UnknownOutcome,
                        "The seat-hold split response contained no split data.",
                        (int)response.StatusCode,
                        body);

                return new SplitHeldSeatsResult(
                    envelope.Data.HoldId,
                    envelope.Data.ExpiresAt,
                    envelope.Data.Seats.Select(seat => seat.SeatHoldReference).ToList());
            }
        }

        public async Task<FlightFlowReply<ConfirmHoldResult>> ConfirmHoldAsync(ConfirmHoldRequest request, CancellationToken cancellationToken = default)
        {
            var route = $"{SeatHoldsRoute}/{request.HoldId}/Confirmations";

            HttpResponseMessage response;
            string body;

            try
            {
                response = await _httpClient.PostAsync(route, null, cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Indeterminate,
                    FulfillmentFailureReason.UnknownOutcome,
                    "The seat-hold confirmation timed out.");
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Retriable,
                    FulfillmentFailureReason.TechnicalFailed,
                    $"The seat-hold confirmation failed to reach FlightFlow. {exception.Message}");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return new FlightFlowReply<ConfirmHoldResult>(new ConfirmHoldResult(FulfillmentReservationStatus.Confirmed, null), (int)response.StatusCode, body);

                if (response.IsSuccessStatusCode)
                    throw UndocumentedSuccess("seat-hold confirmation", response, body);

                var envelope = Deserialize<object>(body);

                if (ConfirmationRefusalOf(ErrorCodeOf(envelope)) is { } holdStatus)
                    return new FlightFlowReply<ConfirmHoldResult>(new ConfirmHoldResult(holdStatus, FirstError(envelope)), (int)response.StatusCode, body);

                var (kind, reason) = response.StatusCode == HttpStatusCode.NotFound
                    ? (FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome)
                    : Classify((int)response.StatusCode);
                throw new ProviderRequestException(
                    kind,
                    reason,
                    FirstError(envelope) ?? $"The seat hold '{request.HoldId}' could not be confirmed (HTTP {(int)response.StatusCode}): {Truncate(body)}",
                    (int)response.StatusCode,
                    body);
            }
        }

        private static FulfillmentReservationStatus? ConfirmationRefusalOf(int? errorCode) => errorCode switch
        {
            CannotConfirmExpiredSeatHoldErrorCode => FulfillmentReservationStatus.Expired,
            CannotConfirmReleasedSeatHoldErrorCode => FulfillmentReservationStatus.Released,
            CannotConfirmCancelledSeatHoldErrorCode => FulfillmentReservationStatus.Cancelled,
            CannotConfirmInconsistentSeatHoldErrorCode => FulfillmentReservationStatus.Mixed,
            SeatHoldNotFoundForConfirmationErrorCode => FulfillmentReservationStatus.Unknown,
            _ => null
        };

        private static FulfillmentReservationStatus? ReleaseRefusalOf(int? errorCode) => errorCode switch
        {
            SeatHoldNotFoundForReleaseErrorCode => FulfillmentReservationStatus.Unknown,
            CannotReleaseConfirmedSeatHoldErrorCode => FulfillmentReservationStatus.Confirmed,
            CannotReleaseExpiredSeatHoldErrorCode => FulfillmentReservationStatus.Expired,
            CannotReleaseCancelledSeatHoldErrorCode => FulfillmentReservationStatus.Cancelled,
            CannotReleaseInconsistentSeatHoldErrorCode => FulfillmentReservationStatus.Mixed,
            _ => null
        };

        private static FulfillmentReservationStatus? CancellationRefusalOf(int? errorCode) => errorCode switch
        {
            SeatHoldNotFoundForCancellationErrorCode => FulfillmentReservationStatus.Unknown,
            CannotCancelHeldSeatHoldErrorCode => FulfillmentReservationStatus.Held,
            CannotCancelReleasedSeatHoldErrorCode => FulfillmentReservationStatus.Released,
            CannotCancelExpiredSeatHoldErrorCode => FulfillmentReservationStatus.Expired,
            CannotCancelInconsistentSeatHoldErrorCode => FulfillmentReservationStatus.Mixed,
            _ => null
        };

        private static ProviderRequestException UndocumentedSuccess(string operation, HttpResponseMessage response, string body)
            => new(
                FulfillmentFailureKind.Indeterminate,
                FulfillmentFailureReason.UnknownOutcome,
                $"The {operation} answered HTTP {(int)response.StatusCode}, which its contract does not define as success.",
                (int)response.StatusCode,
                body);

        private static (FulfillmentFailureKind Kind, FulfillmentFailureReason Reason) Classify(int statusCode)
        {
            if (statusCode >= 500 || statusCode == 429)
                return (FulfillmentFailureKind.Retriable, FulfillmentFailureReason.TechnicalFailed);

            if (statusCode == 400)
                return (FulfillmentFailureKind.Permanent, FulfillmentFailureReason.BusinessRejected);

            return (FulfillmentFailureKind.Permanent, FulfillmentFailureReason.ProviderRejected);
        }

        public async Task<FlightFlowReply<ReleaseHeldSeatsResult>> ReleaseHeldAsync(ReleaseHeldSeatsRequest request, CancellationToken cancellationToken = default)
        {
            HttpResponseMessage response;
            string body;

            try
            {
                response = await _httpClient.DeleteAsync($"{SeatHoldsRoute}/{request.HoldId}", cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Indeterminate,
                    FulfillmentFailureReason.UnknownOutcome,
                    "The seat-hold release timed out; the hold may or may not have been released.");
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Retriable,
                    FulfillmentFailureReason.TechnicalFailed,
                    $"The seat-hold release failed to reach FlightFlow. {exception.Message}");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.NoContent)
                    return new FlightFlowReply<ReleaseHeldSeatsResult>(new ReleaseHeldSeatsResult(FulfillmentReservationStatus.Released, null), (int)response.StatusCode, body);

                if (response.IsSuccessStatusCode)
                    throw UndocumentedSuccess("seat-hold release", response, body);

                var envelope = Deserialize<object>(body);

                if (ReleaseRefusalOf(ErrorCodeOf(envelope)) is { } holdStatus)
                    return new FlightFlowReply<ReleaseHeldSeatsResult>(new ReleaseHeldSeatsResult(holdStatus, FirstError(envelope)), (int)response.StatusCode, body);

                var (kind, reason) = Classify((int)response.StatusCode);
                throw new ProviderRequestException(
                    kind,
                    reason,
                    FirstError(envelope) ?? $"The seat hold '{request.HoldId}' could not be released (HTTP {(int)response.StatusCode}): {Truncate(body)}",
                    (int)response.StatusCode,
                    body);
            }
        }

        public async Task<FlightFlowReply<CancelConfirmedSeatsResult>> CancelConfirmedAsync(CancelConfirmedSeatsRequest request, CancellationToken cancellationToken = default)
        {
            var route = $"{SeatConfirmationsRoute}/{request.HoldBatchId}/Cancellation";
            var payload = new { reasonCode = request.ReasonCode, seatHoldReferences = request.SeatHoldReferences };

            HttpResponseMessage response;
            string body;

            try
            {
                response = await _httpClient.PostAsJsonAsync(route, payload, FlightFlowJson.Options, cancellationToken);
                body = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Indeterminate,
                    FulfillmentFailureReason.UnknownOutcome,
                    "The confirmed-seat cancellation timed out; the seats may or may not have been cancelled.");
            }
            catch (HttpRequestException exception)
            {
                throw new ProviderRequestException(
                    FulfillmentFailureKind.Retriable,
                    FulfillmentFailureReason.TechnicalFailed,
                    $"The confirmed-seat cancellation failed to reach FlightFlow. {exception.Message}");
            }

            using (response)
            {
                if (response.StatusCode == HttpStatusCode.OK)
                    return Deserialize<CancelConfirmedSeatsResult>(body)?.Data is { } cancelled
                        ? new FlightFlowReply<CancelConfirmedSeatsResult>(cancelled, (int)response.StatusCode, body)
                        : throw UndocumentedSuccess("confirmed-seat cancellation", response, body);

                if (response.IsSuccessStatusCode)
                    throw UndocumentedSuccess("confirmed-seat cancellation", response, body);

                var envelope = Deserialize<object>(body);

                if (CancellationRefusalOf(ErrorCodeOf(envelope)) is { } holdStatus)
                    return new FlightFlowReply<CancelConfirmedSeatsResult>(new CancelConfirmedSeatsResult(null, null, holdStatus, FirstError(envelope)), (int)response.StatusCode, body);

                var (kind, reason) = response.StatusCode == HttpStatusCode.NotFound
                    ? (FulfillmentFailureKind.Indeterminate, FulfillmentFailureReason.UnknownOutcome)
                    : Classify((int)response.StatusCode);
                throw new ProviderRequestException(
                    kind,
                    reason,
                    FirstError(envelope) ?? $"The confirmed seats of hold batch '{request.HoldBatchId}' could not be cancelled (HTTP {(int)response.StatusCode}): {Truncate(body)}",
                    (int)response.StatusCode,
                    body);
            }
        }

        private static FlightFlowEnvelope<T>? Deserialize<T>(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return null;

            try
            {
                return JsonSerializer.Deserialize<FlightFlowEnvelope<T>>(body, FlightFlowJson.Options);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static int? ErrorCodeOf<T>(FlightFlowEnvelope<T>? envelope) => envelope?.Errors?.FirstOrDefault()?.Code;

        private static string? FirstError<T>(FlightFlowEnvelope<T>? envelope)
        {
            var error = envelope?.Errors?.FirstOrDefault();
            return error is null ? null : error.Detail ?? error.Title;
        }

        private static string Truncate(string value)
            => string.IsNullOrEmpty(value) ? "<empty>" : value.Length <= 1000 ? value : value[..1000];
    }
}
