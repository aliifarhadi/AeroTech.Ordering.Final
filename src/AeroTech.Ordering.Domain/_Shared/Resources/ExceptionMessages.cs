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
        public const string OfferPricingUnitKindIsMissing = "Offer pricing unit {0} carries no kind.";
        public const string OfferCoveredBoundIsNotRecognised = "Offer pricing unit covers bound offer '{0}', which is not one of the offer's bounds.";
        public const string FarePricingUnitBoundIsNotInOrder = "Fare pricing unit {0} covers bound '{1}', which is not a journey of this order.";
        public const string FareComponentCoverageIsAmbiguous = "Fare components of pricing unit {0} share AirFareId '{1}' on bound '{2}', so the offer contract does not identify which air services each one covers.";
        public const string FareComponentCoversNoAirService = "Fare component {1} of pricing unit {0} covers no air service of this order.";
        public const string FareComponentContradictsAirService = "Fare component {1} of pricing unit {0} contradicts the fare facts of air service '{2}'.";
        public const string FarePricingUnitCoversNoJourney = "Fare pricing unit {0} covers no journey.";
        public const string FareComponentBoundIsNotInPricingUnit = "Fare component {1} of pricing unit {0} covers bound '{2}', which the pricing unit does not cover.";
        public const string OfferBoundOfferIdIsMissing = "Offer bound '{0}' carries no bound offer id.";
        public const string OfferFareComponentBoundIsMissing = "Offer fare component '{0}' carries no bound id.";
        public const string FarePricingUnitContradictsOffer = "Fare pricing unit {0} covers different journeys through its fare components than the offer lists.";
        public const string LastTicketingDatePassed = "Order '{0}' passed its last ticketing date {1}, so a new reservation cannot start.";
        public const string ProviderReadBackIsNotAvailable = "Provider '{0}' offers no read-back of reservation state.";
        public const string ProviderInteractionIsNotInFlight = "Fulfillment task '{0}' has no provider interaction in flight.";
        public const string ProviderInteractionIsAlreadyInFlight = "Fulfillment task '{0}' already has a provider interaction in flight.";
        public const string FarePricingUnitValidationIsUnsupported = "Fare pricing unit {0} of source kind '{1}' has no semantic type that maps to an AirPrice journey type, so it cannot be validated.";
        public const string ReservationIsNotConfirmable = "Reservation '{0}' in status {1} cannot be confirmed.";
        public const string ReservationCannotBeConfirmedAfterLastTicketingDate = "Reservation '{0}' cannot be confirmed because its order passed its last ticketing date {1}.";
        public const string ReservationHoldHasLapsed = "The provider hold of reservation '{0}' lapsed at {1}, so it cannot be confirmed.";
        public const string ReservationValidationIsStale = "The fare validation of reservation '{0}' does not cover its services at the current commercial version (valid until {1}), so it cannot be confirmed.";
        public const string ConfirmationRecoveryRequestIsMissing = "Fulfillment task '{0}' of reservation '{1}' has no persisted confirmation request, so its unresolved confirmation cannot be replayed.";
        public const string FulfillmentTaskTargetIsDuplicated = "Fulfillment task '{0}' already targets {1} '{2}' for {3}.";
        public const string OrderCannotBeIssuedAfterLastTicketingDate = "Order '{0}' passed its last ticketing date {1}, so it cannot be issued.";
        public const string OrderHasNoTicketableServices = "Order '{0}' has no active air service to ticket.";
        public const string AirServiceCapacityIsNotConfirmed = "Air service '{0}' cannot be ticketed because its reserved capacity is {1}, not Confirmed.";
        public const string OrderHasUnresolvedFulfillmentEffect = "Order '{0}' has a fulfillment effect whose outcome is not resolved, so it cannot be issued.";
        public const string AirServiceFareComponentIsMissing = "Air service '{0}' is not covered by any accepted fare component.";
        public const string AirServiceFareComponentIsAmbiguous = "Air service '{0}' is covered by {1} accepted fare components.";
        public const string AirServiceTicketPricingIsMissing = "Air service '{0}' has no accepted customer price allocation to attribute to its ticket coupon.";
        public const string AirServiceTicketPricingIsAmbiguous = "Price allocation '{1}' of air service '{0}' names another traveller or segment.";
        public const string TicketPricingCurrencyMismatch = "Air service '{0}' is priced in currency {1}, not in the order currency {2}.";
        public const string TicketCouponIssuanceValueIsNegative = "Air service '{0}' attributes {1} to its ticket coupon, which cannot be negative.";
        public const string DocumentStockNotFound = "Document stock '{0}' was not found.";
        public const string DocumentStockKindMismatch = "Document stock '{0}' holds {1} numbers, not {2}.";
        public const string DocumentStockIsNotActive = "Document stock '{0}' is {1}, not Active.";
        public const string DocumentStockIsExhausted = "Document stock '{0}' has no number left.";
        public const string DocumentStockHasInsufficientNumbers = "Document stock '{0}' has {1} numbers left but {2} are needed.";
        public const string DocumentStockCheckDigitProfileIsUnsupported = "Check-digit profile '{0}' is not supported; only 'None' is.";
        public const string DocumentStockRangeIsInvalid = "Document stock range {0}-{1} with serial width {2} and prefix '{3}' is invalid.";
        public const string DocumentStockRangeOverlaps = "The range overlaps document stock '{0}' of the same owner, document kind and prefix.";
        public const string DocumentNumberIsAlreadyIssued = "Document number '{0}' is already issued.";
        public const string DocumentStockOperationInProgress = "Another operation holds document stock '{0}'. Retry shortly.";
        public const string ElectronicTicketRequiresCoupons = "The electronic ticket of traveller '{0}' requires at least one coupon.";
        public const string TicketCouponBelongsToAnotherTraveller = "Air service '{0}' belongs to traveller '{1}', not to the ticket traveller.";
        public const string ElectronicTicketCurrencyIsInconsistent = "Ticket value in currency {0} differs from the ticket currency {1}.";
        public const string DocumentNumberIsRequired = "An accountable document requires a document number.";
        public const string OrderIsNotFullyTicketed = "Order '{0}' cannot be Ticketed while air service '{1}' has no ticket coupon.";
        public const string AirServiceIsNotReserved = "Air service '{0}' requires reserved capacity but has none, so it cannot be ticketed.";
        public const string ReservationValidationIsStaleForIssue = "The fare validation of reservation '{0}' does not cover the services being ticketed at the current commercial version (valid until {1}), so they cannot be ticketed.";
        public const string DocumentStockAllocationIsRetired = "Document role '{2}' of fulfillment task '{1}' holds a retired number on document stock '{0}' and cannot be numbered again.";
        public const string OrderServiceRequiresDocumentServicing = "Service '{0}' is covered by a ticket coupon that is not void; void or refund the document before cancelling the service.";
        public const string SeatCancellationRequiresAncillaryStage = "Seat service '{0}' cannot be cancelled on its own until seat unassignment is supported; cancel its air service instead.";
        public const string PartialHeldCancellationIsNotSupportedByProvider = "Reservation '{0}' holds retained services under one provider operation that can only be released as a whole, so part of it cannot be cancelled.";
        public const string CancellationResourceIsUnresolved = "Reservation '{0}' is {1} for the services being cancelled; its provider state must be resolved before the cancellation can be committed.";
        public const string ConfirmedCancellationRecoveryRequestIsMissing = "Confirmed-capacity cancellation task '{0}' of reservation '{1}' has no recorded provider request to replay.";
        public const string ReservationUnitIsNotCancellable = "Reservation unit '{0}' of reservation '{1}' is {2} or has no provider reference, so its confirmed capacity cannot be cancelled.";
        public const string ElectronicTicketNotFoundInOrder = "Electronic ticket '{0}' was not found in order '{1}'.";
        public const string ElectronicTicketVersionConflict = "Electronic ticket '{0}' is at document version {1}, not the expected version {2}.";
        public const string ElectronicTicketVoidDeadlineIsMissing = "Electronic ticket '{0}' has no void deadline under the local void policy; refund it instead.";
        public const string ElectronicTicketVoidWindowIsClosed = "The void window of electronic ticket '{0}' closed at {1}; refund it instead.";
        public const string ElectronicTicketIssuingOfficeMismatch = "Electronic ticket '{0}' was issued by office '{1}', so office '{2}' has no local authority to void it; refund it instead.";
        public const string ElectronicTicketIsNotVoidable = "Electronic ticket '{0}' is {1}, so it cannot be voided; refund it instead.";
        public const string TicketCouponIsNotVoidable = "Coupon {1} of electronic ticket '{0}' is {2}, so the ticket cannot be voided; refund it instead.";
        public const string TicketCouponControlIsNotLocal = "Coupon {1} of electronic ticket '{0}' is under {2} control, so the ticket cannot be voided locally; refund it instead.";
        public const string ConfirmedCancellationTaskTargetsAreInvalid = "Confirmed-capacity cancellation task '{0}' does not target reservation units only, so its provider effect scope is unknown.";
        public const string CancellationRequiresServices = "A cancellation of order '{0}' must end at least one active service.";
        public const string ExternalElectronicTicketVoidIsNotSupported = "Electronic ticket '{0}' is under external document authority, which has no void provider; refund it instead.";
        public const string RepricingRequiredAfterPartialCancellation = "Fare pricing unit '{1}' of order '{0}' was partially cancelled, so its accepted pricing no longer authorizes issuing its remaining services; a source-authoritative repricing or servicing decision is required.";
        public const string OrderChangeReasonTextIsTooLong = "The reason text of a change to order '{0}' has {1} characters; at most {2} are allowed.";
        public const string ReservationValidationEvidenceIsInvalid = "Reservation validation evidence at commercial version {0} must name at least one distinct validated service.";
        public const string OfferFareTypeIsNotRecognised = "Offer fare type {0} is not recognised.";
        public const string OrderDoesNotAcceptAncillariesInStatus = "Order '{0}' is {1}, so it does not accept new ancillary services.";
        public const string OrderCommercialVersionIsNotExpected = "Order '{0}' is at commercial version {1}, not the expected version {2}; read the order again before changing it.";
        public const string OrderHasFulfillmentHistory = "Order '{0}' already has reservation, fulfillment or document history, so it no longer accepts new ancillary services.";
        public const string OrderDoesNotAcceptAncillariesAfterLastTicketingDate = "Order '{0}' passed its last ticketing date {1}, so it does not accept new ancillary services.";
        public const string AncillarySelectionHasExpired = "Ancillary selection '{0}' expired at {1}; price the selection again.";
        public const string AncillarySelectionVersionIsStale = "Ancillary selection '{0}' was priced at commercial version {1} of order '{2}', which is now at version {3}; price the selection again.";
        public const string AncillarySelectionBelongsToAnotherSource = "Ancillary selection '{0}' was priced for {1} '{2}', not for this sale.";
        public const string AncillarySelectionLineCountMismatch = "Ancillary selection '{0}' holds {1} selected lines, but {2} were resolved; the whole selection is refused.";
        public const string AncillarySelectionIsEmpty = "Ancillary selection '{0}' resolved no selected line.";
        public const string AncillaryOfferItemIsDuplicated = "Ancillary selection '{0}' holds offer item '{1}' more than once.";
        public const string AncillaryVariantIsNotRecognised = "Ancillary offer item '{0}' has variant {1}, which is not recognised.";
        public const string AncillaryVariantDoesNotMatchProfile = "Ancillary offer item '{0}' has variant {1}, which cannot be accepted as a {2} service here.";
        public const string AncillaryChoiceIsMissing = "Ancillary offer item '{0}' ({1}) is missing the required choice '{2}'.";
        public const string AncillaryChoiceIsInvalid = "Ancillary offer item '{0}' ({1}) has an invalid choice '{2}'.";
        public const string AncillaryVariantSourceIsIncomplete = "Ancillary offer item '{0}' has variant {1}, whose selected data the ancillary source does not supply yet.";
        public const string AncillaryUpgradeIsNotFixedPriced = "Ancillary offer item '{0}' is a {1} upgrade; only a fixed-price upgrade can be accepted.";
        public const string AncillaryQuantityIsInvalid = "Ancillary offer item '{0}' selects quantity {1}, but its price covers quantity {2}; both must be the same positive whole number.";
        public const string AncillaryScopeIsInconsistent = "Ancillary offer item '{0}' has selection scope {1} with coverage scope {2}, which do not agree.";
        public const string AncillaryTravellerIsNotInOrder = "Ancillary offer item '{0}' names traveller '{1}', who is not an active traveller of the order.";
        public const string AncillaryFlightIsNotInOrder = "Ancillary offer item '{0}' names flight '{1}', which is not covered by an active air service of its traveller.";
        public const string AncillaryPortionIsNotInOrder = "Ancillary offer item '{0}' names portion '{1}', which is not covered by an active air service of its traveller.";
        public const string AncillaryCoverageIsNotSupported = "Ancillary offer item '{0}' has {1} coverage without the flights it covers, so its coverage cannot be accepted.";
        public const string AncillaryCoverageDoesNotMatchUnit = "Ancillary offer item '{0}' has coverage that does not match its selected unit.";
        public const string AncillaryPriceIsIncomplete = "The price of ancillary offer item '{0}' is not complete: the source does not prove that every tax and fee is in its total.";
        public const string AncillaryPriceCurrencyDoesNotMatchOrder = "The price of ancillary offer item '{0}' is in currency {1}, not in the order currency {2}.";
        public const string AncillaryTaxTreatmentIsNotAccepted = "Tax '{1}' of ancillary offer item '{0}' has treatment {2}, which cannot be accepted.";
        public const string AncillaryFeeIsNotAppliedPerItem = "Fee '{1}' of ancillary offer item '{0}' applies per {2}, so it is not part of the quoted item total.";
        public const string AncillaryPriceDoesNotReconcile = "The price components of ancillary offer item '{0}' add up to {1}, not to the quoted {2}.";
        public const string AncillaryPriceAmountIsNegative = "The price of ancillary offer item '{0}' has a negative amount.";
        public const string AncillaryPricingReferenceIsInvalid = "The price of ancillary offer item '{0}' has no valid pricing, rate or version reference.";
        public const string AncillaryServiceIsNotFulfillable = "Order '{0}' holds accepted ancillary service '{1}', which cannot be reserved, confirmed or issued until its fulfillment is supported.";
        public const string AncillaryPartialCancellationRequiresRequote = "The cancellation ends only part of the coverage of ancillary service '{0}'; a source requote or servicing decision is required.";
        public const string AncillaryCancellationRequiresAirService = "Ancillary service '{0}' cannot be cancelled on its own; cancel the air service it covers or the whole order.";
        public const string AncillarySelectionCouldNotBeResolved = "The ancillary source could not resolve selection '{0}'.";
        public const string AncillarySeatIsAlreadyAssigned = "Seat selection '{0}' assigns a second seat to traveller '{1}' on segment '{2}'; a traveller holds one seat per flight.";
        public const string AncillarySeatIsTaken = "Seat selection '{0}' assigns seat {1} on segment '{2}', which another traveller of the order already holds.";
        public const string OrderServiceHasNoTraveller = "Order service '{0}' belongs to the whole order, so it has no traveller for a passenger-specific operation.";
        public const string AncillarySelectionIsNoLongerSellable = "Ancillary selection '{0}' is no longer sellable at its priced terms; price the selection again.";
        public const string AncillarySelectionIsNotFound = "Ancillary selection '{0}' is not known to the ancillary source.";
        public const string AncillarySelectionExpiredAtSource = "Ancillary selection '{0}' has expired at the ancillary source; price the selection again.";
        public const string AncillaryBasketIsEmpty = "No ancillary selection or seat was supplied for order '{0}'.";
        public const string AncillarySourceContractIsIncomplete = "The ancillary source returned selection '{0}' without '{1}', so the selection cannot be accepted.";
    }
}
