INSERT INTO [Order].[Orders] ([Id], [OrderReference], [CustomerId], [SalesChannel], [SalesContextType], [SalesPrincipalType], [SalesActorId], [SalesAirlineUserId], [SalesTravelAgencyId], [SalesTravelAgencyUserId], [SalesIndividualId], [SalesPartnerApiAccessProfileId], [CurrencyId], [SourceOfferId], [LastTicketingDate], [Status], [CommercialVersion], [CustomerTotal], [CreatedAt], [LastUpdateTime], [LastUpdatedBy], [SalesOfficeId], [SalesOfficeKind], [RecordLocator])
VALUES
    (1001, N'6051A9B4-1112-41F8-8CF4-A658A5E3DD21', 42, 1, 1, 1, 7, 7, NULL, NULL, NULL, NULL, 1, N'OFFER-1', NULL, 1, 3, 875.0, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL);

INSERT INTO [Order].[OrderChanges] ([Id], [OrderId], [ChangeType], [CommercialVersion], [ActorChannel], [ActorContextType], [ActorPrincipalType], [ActorId], [ActorAirlineUserId], [ActorTravelAgencyId], [ActorTravelAgencyUserId], [ActorIndividualId], [ActorPartnerApiAccessProfileId], [SourceReference], [CommittedAt], [LastUpdateTime], [LastUpdatedBy], [ActorOfficeId], [ActorOfficeKind], [IsInvoluntary], [ReasonCode], [SourceSystem], [WaiverCode], [ReasonText])
VALUES
    (1002, 1001, 1, 1, 1, 1, 1, 7, 7, NULL, NULL, NULL, NULL, N'OFFER-1', N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, 0, NULL, NULL, NULL, NULL),
    (1041, 1001, 2, 2, 1, 1, 1, 88, 88, NULL, NULL, NULL, NULL, N's_1001_1', N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, 5, 1, 0, NULL, NULL, NULL, NULL),
    (1126, 1001, 2, 3, 1, 1, 1, 88, 88, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, 5, 1, 0, NULL, NULL, NULL, NULL);

INSERT INTO [Order].[OrderContacts] ([Id], [OrderId], [Sequence], [Role], [ContactName], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1003, 1001, 1, 1, N'Contact', N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[OrderFarePricingUnits] ([Id], [OrderId], [CreatedByChangeId], [Sequence], [SemanticType], [CoveredJourneyIds], [LastUpdateTime], [LastUpdatedBy], [SourceKind])
VALUES
    (1024, 1001, 1002, 1, 1, N'[1009]', N'2026-10-01T10:00:00Z', NULL, N'ThroughOneWay'),
    (1026, 1001, 1002, 2, 1, N'[1012]', N'2026-10-01T10:00:00Z', NULL, N'OneWay');

INSERT INTO [Order].[OrderItems] ([Id], [OrderId], [Kind], [AcceptedTotal], [CommercialStatus], [CreatedByChangeId], [CreatedAt], [LastUpdateTime], [LastUpdatedBy], [EndedByChangeId])
VALUES
    (1014, 1001, 1, 200.0, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1017, 1001, 1, 100.0, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1019, 1001, 1, 200.0, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1022, 1001, 1, 100.0, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1042, 1001, 3, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1046, 1001, 3, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1050, 1001, 3, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1054, 1001, 3, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1058, 1001, 3, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1062, 1001, 3, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1066, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1070, 1001, 4, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1074, 1001, 4, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1078, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1082, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1086, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1090, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1094, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1098, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1102, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1106, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1110, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1114, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1118, 1001, 3, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1122, 1001, 17, 10.0, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1127, 1001, 2, 25.0, 1, 1126, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL),
    (1131, 1001, 2, 40.0, 1, 1126, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL);

INSERT INTO [Order].[OrderJourneys] ([Id], [OrderId], [Sequence], [BoundId], [OriginAirportId], [DestinationAirportId], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1009, 1001, 1, N'OUT', 10, 12, N'2026-10-01T10:00:00Z', NULL),
    (1012, 1001, 2, N'RET', 20, 21, N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[OrderSegments] ([Id], [OrderId], [OrderJourneyId], [Sequence], [FlightId], [FlightVersion], [FlightNumber], [OriginAirportId], [OriginAirportTerminalId], [DestinationAirportId], [DestinationAirportTerminalId], [OperatingAirlineId], [MarketingAirlineId], [SoldDeparture], [SoldArrival], [Duration], [AircraftId], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1010, 1001, 1009, 1, 101, 1, N'DA101', 10, NULL, 11, NULL, 10, 10, N'2026-10-12T10:00:00Z', N'2026-10-12T11:00:00Z', 60, 1, N'2026-10-01T10:00:00Z', NULL),
    (1011, 1001, 1009, 2, 102, 1, N'DA102', 11, NULL, 12, NULL, 10, 10, N'2026-10-12T13:00:00Z', N'2026-10-12T14:00:00Z', 60, 1, N'2026-10-01T10:00:00Z', NULL),
    (1013, 1001, 1012, 1, 201, 1, N'DA201', 20, NULL, 21, NULL, 10, 10, N'2026-10-13T10:00:00Z', N'2026-10-13T11:00:00Z', 60, 1, N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[OrderServices] ([Id], [OrderId], [OrderItemId], [TravellerId], [ServiceType], [CommercialStatus], [CreatedByChangeId], [CreatedAt], [LastUpdateTime], [LastUpdatedBy], [FulfillmentProviderKey], [EndedByChangeId])
VALUES
    (1015, 1001, 1014, 1005, 1, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL),
    (1016, 1001, 1014, 1005, 1, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL),
    (1018, 1001, 1017, 1005, 1, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL),
    (1020, 1001, 1019, 1007, 1, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL),
    (1021, 1001, 1019, 1007, 1, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL),
    (1023, 1001, 1022, 1007, 1, 1, 1002, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL),
    (1043, 1001, 1042, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1047, 1001, 1046, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1051, 1001, 1050, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1055, 1001, 1054, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1059, 1001, 1058, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1063, 1001, 1062, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1067, 1001, 1066, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1071, 1001, 1070, 1005, 4, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1075, 1001, 1074, 1005, 4, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1079, 1001, 1078, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1083, 1001, 1082, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1087, 1001, 1086, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1091, 1001, 1090, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1095, 1001, 1094, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1099, 1001, 1098, 1005, 5, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1103, 1001, 1102, 1005, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1107, 1001, 1106, 1005, 6, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1111, 1001, 1110, 1005, 20, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1115, 1001, 1114, 1005, 21, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1119, 1001, 1118, 1007, 23, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1123, 1001, 1122, NULL, 21, 1, 1041, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'Ancillary', NULL),
    (1128, 1001, 1127, 1005, 2, 1, 1126, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL),
    (1132, 1001, 1131, 1005, 2, 1, 1126, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, N'FlightFlow', NULL);

INSERT INTO [Order].[OrderTravellers] ([Id], [OrderId], [Index], [SourceTravellerRef], [PassengerType], [AgeRange], [InfantParentTravellerId], [CurrentProfileRevisionId], [Status], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1005, 1001, 1, N'T1', 1, 1, NULL, 1006, 1, N'2026-10-01T10:00:00Z', NULL),
    (1007, 1001, 2, N'T2', 1, 1, NULL, 1008, 1, N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[PricingLines] ([Id], [OrderId], [CreatedByChangeId], [Reason], [Scope], [Category], [SubCategory], [Direction], [Treatment], [Code], [Description], [Reference], [Amount], [CurrencyId], [EquivalentAmount], [EquivalentCurrencyId], [ExchangeFromCurrencyId], [ExchangeToCurrencyId], [ExchangeRate], [ExchangeDecimalPlaces], [ExchangePeriodId], [Refundability], [CreatedAt], [LastUpdateTime], [LastUpdatedBy], [SourceFeeApplicationUnit], [SourceTaxTreatment], [TaxCountryId], [TaxStationId])
VALUES
    (1028, 1001, 1002, 1, 3, 1, 1, 2, 1, N'YOW', N'Fare', N'7001', 100.0, 1, 100.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1030, 1001, 1002, 1, 3, 1, 1, 2, 1, N'YOW', N'Fare', N'7001', 100.0, 1, 100.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1032, 1001, 1002, 1, 3, 1, 1, 2, 1, N'YOW', N'Fare', N'7002', 100.0, 1, 100.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1034, 1001, 1002, 1, 3, 1, 1, 2, 1, N'YOW', N'Fare', N'7001', 100.0, 1, 100.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1036, 1001, 1002, 1, 3, 1, 1, 2, 1, N'YOW', N'Fare', N'7001', 100.0, 1, 100.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1038, 1001, 1002, 1, 3, 1, 1, 2, 1, N'YOW', N'Fare', N'7002', 100.0, 1, 100.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1044, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1048, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1052, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1056, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1060, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1064, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1068, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1072, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1076, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1080, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1084, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1088, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1092, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1096, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1100, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1104, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1108, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1112, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1116, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1120, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1124, 1001, 1041, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 10.0, 1, 10.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1129, 1001, 1126, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 25.0, 1, 25.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL),
    (1133, 1001, 1126, 9, 3, 8, 6, 2, 1, NULL, NULL, NULL, 40.0, 1, 40.0, 1, NULL, NULL, NULL, NULL, NULL, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL, NULL, NULL, NULL, NULL);

INSERT INTO [Order].[OrderAirTransportServices] ([Id], [SegmentId], [FlightCapacityId], [AirFareId], [BookingClass], [FareBasis], [FareFamily], [FareType], [CheckedBaggagePieces], [CheckedBaggageWeight], [CheckedBaggageUnit], [CabinBaggagePieces], [CabinBaggageWeight], [CabinBaggageUnit], [IsRefundable], [IsChangeable], [IsUpgradable], [RbdId], [CabinClassId])
VALUES
    (1015, 1010, 90101, 7001, N'Y', N'YOW', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, 0, 25, 4),
    (1016, 1011, 90102, 7001, N'Y', N'YOW', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, 0, 25, 4),
    (1018, 1013, 90201, 7002, N'Y', N'YOW', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, 0, 25, 4),
    (1020, 1010, 90101, 7001, N'Y', N'YOW', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, 0, 25, 4),
    (1021, 1011, 90102, 7001, N'Y', N'YOW', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, 0, 25, 4),
    (1023, 1013, 90201, 7002, N'Y', N'YOW', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1, 0, 25, 4);

INSERT INTO [Order].[OrderAncillaryServices] ([Id], [Profile], [VariantCode], [Quantity], [SelectionScope], [CoverageScope], [CoveredAirServiceIds], [SourceKind], [SourceReference], [SourceOrderCommercialVersion], [SourceSelectionId], [SourceOfferItemId], [DefinitionVersionId], [ServiceDefinitionRef], [ServiceTypeCode], [ServiceSubCode], [Rfic], [Rfisc], [SsrCode], [ProvisionId], [ProvisionPurchaseStage], [ProvisionBookingMethod], [ProvisionBookingRequired], [ProvisionConfirmationRequirement], [ProvisionFulfillmentProviderKey], [ProvisionDocumentRouting], [ProvisionDocumentType], [ProvisionDocumentRequired], [ProvisionRequiresCheckAtReserve], [PricingId], [PricingRateId], [PricingVersion], [PricingUnit], [MealKind], [MealCode], [MealMenuItemRef], [MealDietaryCode], [MealExclusiveFamilyCode], [PetTransportMode], [PetAnimalType], [PetOtherAnimalCode], [PetAnimalAgeWeeks], [PetCombinedWeightKg], [PetCarrierLengthCm], [PetCarrierWidthCm], [PetCarrierHeightCm], [PetDocumentAcknowledgements], [PetSizeBracket], [AssistanceKind], [AssistanceSsrCode], [AssistanceInfantTravellerId], [AssistanceGuardianTravellerId], [AirportServiceKind], [AirportServiceAirportId], [AirportServiceTerminalRef], [AirportServiceFacilityId], [AirportServiceDirection], [AirportServiceTimeWithOffset], [AirportServiceGuestCount], [PriorityKind], [PriorityZoneCode], [PriorityGroupCode], [PriorityFareBenefitRef], [PriorityOptIn], [ConnectivityPlanKind], [ConnectivityDurationMinutes], [ConnectivityIncludedDataMb], [ConnectivityMaxDevices], [ConnectivityDeliveryStage], [ConnectivityDeviceCount], [UpgradeFromCabinId], [UpgradeToCabinId], [UpgradeKind], [UpgradeRequiresTicketExchange])
VALUES
    (1043, 1, N'A01', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A01_1005_OUT:F1', 101, N'DEF_A01', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1047, 1, N'A02', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A02_1005_OUT:F1', 102, N'DEF_A02', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1051, 1, N'A03', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A03_1005_OUT:F1', 103, N'DEF_A03', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1055, 1, N'A04', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A04_1005_OUT:F1', 104, N'DEF_A04', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1059, 1, N'A05', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A05_1005_OUT:F1', 105, N'DEF_A05', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1063, 1, N'A06', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A06_1005_OUT:F1', 106, N'DEF_A06', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1067, 3, N'A10', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A10_1005_OUT:F1', 110, N'DEF_A10', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 2, 1, 0),
    (1071, 4, N'A11', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A11_1005_OUT:F1', 111, N'DEF_A11', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, 1, N'VGML', NULL, N'VG', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1075, 4, N'A12', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A12_1005_OUT:F1', 112, N'DEF_A12', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, 2, NULL, N'MENU_7', NULL, N'MAIN', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1079, 5, N'A13', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A13_1005_OUT:F1', 113, N'DEF_A13', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, 1, 1, NULL, 20, 6.5, 45.0, 30.0, 25.0, N'["RABIES"]', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1083, 5, N'A14', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A14_1005_OUT:F1', 114, N'DEF_A14', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, 2, 1, NULL, 20, 6.5, 45.0, 30.0, 25.0, N'["RABIES"]', N'M', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1087, 6, N'A15', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A15_1005_OUT:F1', 115, N'DEF_A15', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, N'WCHR', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1091, 6, N'A16', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A16_1005_OUT:F1', 116, N'DEF_A16', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 2, N'BLND', NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1095, 6, N'A18', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A18_1005_OUT:F1', 118, N'DEF_A18', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 4, NULL, 1007, 1005, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1099, 7, N'A20', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A20_1005_OUT:F1', 120, N'DEF_A20', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 2, N'T1', 55, 1, N'2026-12-27T10:00:00+03:30', 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1103, 7, N'A21', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A21_1005_OUT:F1', 121, N'DEF_A21', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 2, 2, N'T1', 55, 1, N'2026-12-27T10:00:00+03:30', 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1107, 7, N'A22', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A22_1005_OUT:F1', 122, N'DEF_A22', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 3, 2, N'T1', 55, 1, N'2026-12-27T10:00:00+03:30', 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1111, 8, N'A23', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A23_1005_OUT:F1', 123, N'DEF_A23', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, N'Z1', N'G1', NULL, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1115, 9, N'A24', 1, 4, 1, N'[1015]', 2, N'1001', 1, N's_1001_1', N'd_A24_1005_OUT:F1', 124, N'DEF_A24', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 4, 600, 500, 2, 1, 1, NULL, NULL, NULL, NULL),
    (1119, 1, N'A01', 1, 3, 2, N'[1020,1021]', 2, N'1001', 1, N's_1001_1', N'd_A01_1007_OUT', 101, N'DEF_A01', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1123, 9, N'A24', 1, 1, 4, N'[]', 2, N'1001', 1, N's_1001_1', N'd_A24__ORDER', 124, N'DEF_A24', N'F', N'0CC', N'C', N'0CC', NULL, 200, 1, 1, 1, 1, NULL, 2, 2, 1, 0, 11, 12, 1, 3, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 4, 600, 500, 2, 1, 1, NULL, NULL, NULL, NULL);

INSERT INTO [Order].[OrderContactPoints] ([Id], [OrderContactId], [Type], [Value], [CountryCode], [IsPrimary], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1004, 1003, 1, N'contact@example.com', NULL, 1, N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[OrderFareComponents] ([Id], [OrderFarePricingUnitId], [Sequence], [AirFareId], [BookingClass], [FareBasis], [FareFamily], [FareType], [CoveredOrderServiceIds], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1025, 1024, 1, 7001, N'Y', N'YOW', NULL, NULL, N'[1015,1016,1020,1021]', N'2026-10-01T10:00:00Z', NULL),
    (1027, 1026, 1, 7002, N'Y', N'YOW', NULL, NULL, N'[1018,1023]', N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[OrderSeatServices] ([Id], [SegmentId], [AssociatedAirServiceId], [SeatNumber], [DefinitionVersionId], [PointOfSaleId], [PricingId], [PricingRateId], [PricingUnit], [PricingVersion], [ProvisionBookingMethod], [ProvisionBookingRequired], [ProvisionConfirmationRequirement], [ProvisionDocumentRequired], [ProvisionDocumentRouting], [ProvisionDocumentType], [ProvisionFulfillmentProviderKey], [ProvisionId], [ProvisionPurchaseStage], [ProvisionRequiresCheckAtReserve], [Rfic], [Rfisc], [ServiceDefinitionRef], [ServiceSubCode], [ServiceTypeCode], [SourceKind], [SourceOfferItemId], [SourceOrderCommercialVersion], [SourceReference], [SourceSelectionId], [SsrCode], [VariantCode])
VALUES
    (1128, 1010, 1015, N'12A', 107, 7, 11, 12, 3, 1, 1, 1, 1, 1, 2, 2, NULL, 200, 1, 0, N'C', N'0CC', N'DEF_A07', N'0CC', N'F', 2, NULL, 2, N'1001', N'seat_1005_101_12A', NULL, N'A07'),
    (1132, 1011, 1016, N'18C', 108, 7, 11, 12, 3, 1, 1, 1, 1, 1, 2, 2, NULL, 200, 1, 0, N'C', N'0CC', N'DEF_A08', N'0CC', N'F', 2, NULL, 2, N'1001', N'seat_1005_102_18C', NULL, N'A08');

INSERT INTO [Order].[PricingAllocations] ([Id], [PricingLineId], [OrderItemId], [OrderServiceId], [OrderJourneyId], [OrderSegmentId], [TravellerId], [Amount], [CurrencyId], [EquivalentAmount], [EquivalentCurrencyId], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1029, 1028, 1014, 1015, 1009, 1010, 1005, 100.0, 1, 100.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1031, 1030, 1014, 1016, 1009, 1011, 1005, 100.0, 1, 100.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1033, 1032, 1017, 1018, 1012, 1013, 1005, 100.0, 1, 100.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1035, 1034, 1019, 1020, 1009, 1010, 1007, 100.0, 1, 100.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1037, 1036, 1019, 1021, 1009, 1011, 1007, 100.0, 1, 100.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1039, 1038, 1022, 1023, 1012, 1013, 1007, 100.0, 1, 100.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1045, 1044, 1042, 1043, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1049, 1048, 1046, 1047, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1053, 1052, 1050, 1051, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1057, 1056, 1054, 1055, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1061, 1060, 1058, 1059, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1065, 1064, 1062, 1063, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1069, 1068, 1066, 1067, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1073, 1072, 1070, 1071, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1077, 1076, 1074, 1075, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1081, 1080, 1078, 1079, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1085, 1084, 1082, 1083, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1089, 1088, 1086, 1087, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1093, 1092, 1090, 1091, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1097, 1096, 1094, 1095, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1101, 1100, 1098, 1099, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1105, 1104, 1102, 1103, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1109, 1108, 1106, 1107, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1113, 1112, 1110, 1111, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1117, 1116, 1114, 1115, 1009, 1010, 1005, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1121, 1120, 1118, 1119, NULL, NULL, 1007, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1125, 1124, 1122, 1123, NULL, NULL, NULL, 10.0, 1, 10.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1130, 1129, 1127, 1128, 1009, 1010, 1005, 25.0, 1, 25.0, 1, N'2026-10-01T10:00:00Z', NULL),
    (1134, 1133, 1131, 1132, 1009, 1011, 1005, 40.0, 1, 40.0, 1, N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[TravellerProfileRevisions] ([Id], [TravellerId], [GivenName], [Surname], [NoSurname], [DateOfBirth], [Gender], [NationalityId], [CountryOfResidenceId], [CreatedByChangeId], [SupersededByChangeId], [CreatedAt], [LastUpdateTime], [LastUpdatedBy])
VALUES
    (1006, 1005, N'GIVEN1', N'SURNAME1', 0, N'1990-01-01', 2, NULL, NULL, 1002, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL),
    (1008, 1007, N'GIVEN2', N'SURNAME2', 0, N'1990-01-01', 2, NULL, NULL, 1002, NULL, N'2026-10-01T10:00:00Z', N'2026-10-01T10:00:00Z', NULL);

INSERT INTO [Order].[OrderBaggageServices] ([Id], [ChargeKind], [AllowanceConcept], [PackageWeightKg], [WeightFromExclusiveKg], [WeightToInclusiveKg], [EquipmentKind], [BagRef], [MeasuredWeightKg], [LengthCm], [WidthCm], [HeightCm], [WeightKg])
VALUES
    (1043, 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1047, 2, 1, 10.0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1051, 3, 1, NULL, 23.0, 32.0, NULL, N'BAG1', 27.5, NULL, NULL, NULL, NULL),
    (1055, 4, 1, NULL, NULL, NULL, NULL, NULL, NULL, 90.0, 60.0, 40.0, NULL),
    (1059, 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL),
    (1063, 5, 1, NULL, NULL, NULL, N'GOLF', NULL, NULL, NULL, NULL, NULL, 15.0),
    (1119, 1, 1, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL);
