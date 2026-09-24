namespace AeroTech.Ordering.Domain._Shared.Resources
{
    public static class ExceptionMessages
    {
        // Order lifecycle
        public const string OrderCannotTransition = "Order cannot transition from {0} to {1}.";
        public const string OrderCannotBeReserved = "Order {0} cannot be reserved from status {1}.";
        public const string OrderCannotStartPayment = "Order {0} cannot start payment from status {1}.";
        public const string OrderCannotBeIssued = "Order {0} cannot be issued from status {1}. It must be paid first.";
        public const string OrderCannotBeCancelled = "Order {0} cannot be cancelled from status {1}.";
        public const string OrderCannotExpire = "Order {0} cannot expire from status {1}.";
        public const string OrderHasNotReachedTimeToLive = "Order {0} has not reached its time-to-live and cannot expire.";

        // Order time-to-live
        public const string TimeToLiveOnlyUpdatableWhileConfirmed = "Order {0} time-to-live can only be updated while Confirmed (current status {1}).";
        public const string TimeToLiveMustBeInTheFuture = "The new time-to-live must be in the future.";

        // Order composition
        public const string OrderMustIncludeAtLeastOneAdult = "An order must include at least one adult traveller.";
        public const string OrderCannotHaveMoreInfantsThanAdults = "An order cannot have more infants than adults.";

        // Order split
        public const string OrderCannotBeSplit = "Order {0} cannot be split from status {1}.";
        public const string AtLeastOneTravellerMustBeSelectedToSplit = "At least one traveller must be selected to split.";
        public const string SelectedTravellersDoNotBelongToOrder = "One or more selected travellers do not belong to this order.";
        public const string CannotSplitOffAllTravellers = "Cannot split off all travellers; the source order must retain at least one.";
        public const string InfantAndParentMustBeSplitTogether = "An infant and its parent must be split together.";

        // Order remarks
        public const string RemarkNotFound = "Remark not found on this order.";
        public const string RemarksCannotBeChangedOnClosedOrder = "Remarks cannot be changed on a closed order.";
        public const string OnlyActiveRemarkCanBeModified = "Only an active remark can be modified.";
        public const string RemarkTextIsRequired = "Remark text is required.";
        public const string RemarkTextTooLong = "Remark text cannot exceed {0} characters.";
        public const string TravellerScopedRemarkRequiresTraveller = "A traveller-scoped remark requires a traveller reference.";
        public const string SegmentScopedRemarkRequiresSegment = "A segment-scoped remark requires a segment reference.";
        public const string OrderItemScopedRemarkRequiresOrderItem = "An order-item-scoped remark requires an order-item reference.";
        public const string OrderServiceScopedRemarkRequiresOrderService = "An order-service-scoped remark requires an order-service reference.";
        public const string DocumentScopedRemarkRequiresDocument = "A document-scoped remark requires a document reference.";
        public const string RemarkTravellerDoesNotBelongToOrder = "The remark traveller reference does not belong to this order.";
        public const string RemarkSegmentDoesNotBelongToOrder = "The remark segment reference does not belong to this order.";
        public const string RemarkOrderItemDoesNotBelongToOrder = "The remark order-item reference does not belong to this order.";
        public const string RemarkOrderServiceDoesNotBelongToOrder = "The remark order-service reference does not belong to this order.";

        // Traffic document
        public const string OnlyIssuedDocumentCanBeVoided = "Only an issued document can be voided.";
        public const string OnlyIssuedDocumentCanBeCancelled = "Only an issued document can be cancelled.";
        public const string OnlyIssuedDocumentCanBeMarkedVoidUnconfirmed = "Only an issued document can be marked void-unconfirmed.";
        public const string OnlyIssuedDocumentCanBeMarkedCancelUnconfirmed = "Only an issued document can be marked cancel-unconfirmed.";
        public const string TerminationWindowExpired = "The termination window for this document has expired.";
        public const string DocumentWithUsedCouponCannotBeTerminated = "A document with a used or flown coupon cannot be terminated.";
        public const string DocumentWithUsedCouponCannotBeMoved = "A document with a used or flown coupon cannot be moved to another order.";
        public const string CouponReferencesServiceNotInSplit = "Coupon {0} references service {1} which was not part of the split.";

        // Payment
        public const string OnlyCapturedPaymentCanBeVoided = "Only a captured payment can be voided.";
        public const string VoidAmountMustBeGreaterThanZero = "The void amount must be greater than zero.";
        public const string VoidAmountExceedsCapturedAmount = "The void amount exceeds the remaining captured amount.";

        // Traveller name
        public const string FirstNameIsRequired = "First name is required.";
        public const string FirstNameIsInvalid = "First name is invalid.";
        public const string SurnameIsRequired = "Surname is required.";
        public const string SurnameIsInvalid = "Surname is invalid.";
        public const string NameIsTooLong = "Name is too long.";

        // Offer
        public const string OfferHasNoTravellerWithIndex = "Offer has no traveller with index {0}.";
        public const string CouldNotResolveAirFareForBound = "Could not resolve an air fare id for bound {0}.";
        public const string OfferHasNoTicketForTraveller = "Offer has no ticket for traveller {0}.";
        public const string OfferHasNoCouponForFlight = "Offer has no coupon for traveller {0} on flight {1}.";

        // Application: lookups and orchestration
        public const string OrderNotFound = "Order '{0}' was not found.";
        public const string TrafficDocumentNotFound = "Traffic document not found.";
        public const string DocumentDoesNotBelongToOrder = "The document does not belong to this order.";
        public const string FulfillmentTaskNotFound = "Fulfillment task '{0}' was not found.";
        public const string OrderForFulfillmentTaskNotFound = "Order '{0}' for fulfillment task '{1}' was not found.";
        public const string NoFulfillmentAdapterRegistered = "No fulfillment adapter is registered for {0}/{1}.";
        public const string PaymentAlreadyInProgress = "A payment for order '{0}' is already in progress.";
        public const string PaymentNotFound = "Payment '{0}' for order '{1}' was not found.";
        public const string PaymentUnconfirmedWithoutPayment = "Order '{0}' is payment-unconfirmed but has no payment to reconcile.";
        public const string OrderHasNoReservedHoldsToIssue = "Order '{0}' has no reserved holds to issue.";
        public const string CouldNotGenerateRecordLocator = "Could not generate a unique record locator.";
        public const string CouldNotGenerateTicketNumber = "Could not generate a unique ticket number.";
        public const string OrderOperationInProgress = "Another operation is already in progress for order '{0}'. Try again shortly.";

        // Provider rejections
        public const string ProviderRequestFailed = "The provider request failed.";
        public const string ReservationRejectedByProvider = "The reservation was rejected by the provider.";
        public const string TicketVoidRejectedByProvider = "The ticket void was rejected by the provider.";
        public const string TicketCancellationRejectedByProvider = "The ticket cancellation was rejected by the provider.";
        public const string HoldCouldNotBeReleased = "A reservation hold could not be released.";
        public const string SeatHoldCouldNotBeReleased = "The seat hold '{0}' could not be released. {1}";
        public const string OfferCouldNotBeRetrieved = "The offer '{0}' could not be retrieved.";
        public const string FareReservationCouldNotBeValidated = "The fare reservation could not be validated.";
        public const string FareReservationIsNotPermitted = "The fare reservation is not permitted.";

        // Initial sale
        public const string SalesContextActorIsRequired = "Sales context requires an actor id.";
        public const string BaggagePiecesCannotBeNegative = "Baggage pieces cannot be negative.";
        public const string BaggageWeightCannotBeNegative = "Baggage weight cannot be negative.";
        public const string BaggageUnitIsNotRecognised = "Baggage unit {0} is not recognised.";
        public const string ExchangeRateMustBePositive = "Exchange rate must be greater than zero.";
        public const string ExchangeRateDecimalPlacesCannotBeNegative = "Exchange rate decimal places cannot be negative.";
        public const string TravellerIndexIsDuplicated = "Traveller indexes must be unique in the request.";
        public const string OfferTravellerIndexIsDuplicated = "Traveller indexes must be unique in the offer.";
        public const string TravellerSetDoesNotMatchOffer = "The requested traveller set does not match the offer traveller set.";
        public const string OfferPassengerCodeIsNotRecognised = "Offer passenger code {0} is not recognised.";
        public const string TravellerPassengerTypeDoesNotMatchOffer = "Traveller {0} passenger type does not match the offer.";
        public const string InfantRequiresParentTraveller = "Infant traveller {0} requires a parent traveller.";
        public const string InfantParentMustBeAnAdult = "The parent of infant traveller {0} must be an adult.";
        public const string OrderHasNoTravellerWithIndex = "Order has no traveller with index {0}.";
        public const string OrderHasNoSegmentForFlight = "Order has no segment for flight {0}.";
        public const string OrderHasNoAirServiceForFlight = "Order has no air transport service for flight {0}.";
        public const string SeatSelectionBoundIsNotInOrder = "Seat selection references bound {0} which is not in the order.";
        public const string SeatSelectionHasNoMatchingAirService = "Seat selection for traveller {0} on bound {1} has no matching air transport service.";
        public const string OfferPriceCategoryIsNotRecognised = "Offer price category {0} is not recognised.";
        public const string PricingAllocationsDoNotReconcile = "Allocations of pricing line {0} do not reconcile with the line amount.";
        public const string CustomerPriceLineCurrencyDoesNotMatchOrder = "Every customer-price line must be expressed in the order currency.";
        public const string DocumentScopedRemarkIsNotSupportedYet = "Document-scoped remarks are not supported in this stage.";
        public const string OrderDoesNotBelongToCaller = "Order {0} does not belong to the calling customer.";
        public const string CallerHasNoCustomerContext = "The caller has no customer context.";
        public const string CountryCodeIsNotRecognised = "Country code {0} is not recognised.";
        public const string CallerPrincipalTypeIsNotRecognised = "Caller principal type {0} is not recognised.";
        public const string CallerContextTypeIsNotRecognised = "Caller context type {0} is not recognised.";
        public const string SellingOfficeIsIncomplete = "A selling office requires both its kind and its identifier.";
        public const string SellingOfficeIsAmbiguous = "The caller holds {0} travel agency offices; the selling office cannot be resolved.";
        public const string FulfillmentProviderIsRequired = "A fulfillable order service requires a fulfillment provider.";
        public const string RecordLocatorIsRequired = "A record locator cannot be empty.";
        public const string OrderIsNotReservable = "Order '{0}' in status {1} cannot be reserved.";
        public const string OrderServiceIsNotInOrder = "Order service '{0}' does not belong to order '{1}'.";
        public const string OrderServiceIsNotActive = "Order service '{0}' is not active.";
        public const string OrderServiceDoesNotRequireReservation = "Order service '{0}' has no reservation step.";
        public const string ReservationUnitIncludesCoveredService = "Reservation unit '{0}' would add order service '{1}' to an existing provider operation; adding to an existing hold is not supported.";
        public const string InfantReservationMappingIsBlocked = "Reserving lap infant '{0}' with {1} is blocked until the provider's infant request mapping is confirmed.";
        public const string AirServiceValidationFactsAreMissing = "Air service '{0}' lacks the accepted facts needed to validate its fare for reservation.";
        public const string ReservationNotFound = "Reservation '{0}' was not found for order '{1}'.";
        public const string ReservationIsNotReleasable = "Reservation '{0}' in status {1} cannot be released.";
        public const string ReservationOutcomeCannotBeRecorded = "Reservation '{0}' in status {1} cannot record a provider outcome.";
        public const string ReservationOutcomeDoesNotMatchUnits = "The provider outcome for reservation '{0}' does not match its units.";
        public const string ProviderReferenceCannotChange = "Provider reference of {0} '{1}' cannot change from '{2}' to '{3}'.";
        public const string ReservationUnitMustCoverServices = "A reservation unit must cover at least one order service.";
        public const string ReservationUnitCoversDuplicateService = "Order service '{0}' is covered more than once in one reservation.";
        public const string ReservationRequiresUnits = "A reservation must contain at least one unit.";
        public const string FulfillmentTaskCannotStartAttempt = "Fulfillment task '{0}' in status {1} cannot start a new attempt.";
        public const string FulfillmentTaskHasNoAttemptInProgress = "Fulfillment task '{0}' has no attempt in progress.";
        public const string ReservationPlanDiffersFromPersistedUnits = "Re-planned units of reservation '{0}' differ from its persisted units.";
        public const string PassengerTypeIsNotSupportedByProvider = "Passenger type {0} has no counterpart in the {1} passenger type contract.";
    }
}
