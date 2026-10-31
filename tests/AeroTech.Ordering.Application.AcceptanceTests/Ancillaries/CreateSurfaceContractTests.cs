using System.Text.Json;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Messages.Shared.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Backoffice;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.Ota;
using AeroTech.Ordering.Application.OrderAggregate.Commands.CreateOrderFromOffer.OtaPanel;
using AeroTech.Ordering.Application._Shared.Authorization;
using AeroTech.Ordering.Domain.OrderAggregate.Arguments;
using AeroTech.Ordering.Domain.OrderAggregate.ValueObjects;
using AeroTech.Ordering.Domain._Shared.Contracts;
using AeroTech.Ordering.RestApi.V1.OrderAggregate;
using AeroTech.Ordering.RestApi.V1.OrderAggregate.Requests;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Ancillaries;

public sealed class CreateSurfaceContractTests
{
    private const string AirOnly = """{"offerId":"OFFER-1","customerId":42,"contact":{"addresseeName":"A","emailAddress":"a@b.c","phones":[]},"travelers":[]}""";
    private const string WithSelections = """{"offerId":"OFFER-1","customerId":42,"contact":{"addresseeName":"A","emailAddress":"a@b.c","phones":[]},"travelers":[],"ancillaryPricedSelectionId":"s_general","seatPricedSelectionId":"s_seats"}""";

    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);
    private static readonly CreateOrderFromOfferResult Result = new(1, Guid.Empty, 1, 100m, 1);
    private static readonly OrderContact Contact = new("A", "a@b.c", []);

    private readonly CapturingCreateService _service = new();
    private readonly RecordingMediator _mediator = new(Result);
    private readonly OrderInputMapper _input = new(new NoCountries());
    private readonly FixedSalesContextFactory _salesContext = new(new SalesContext(SalesChannel.BackOffice, CallerContextType.Airline, CallerPrincipalType.Human, 7, 7, null, null, null, null, null, null));

    [Theory]
    [InlineData(WithSelections, "s_general", "s_seats")]
    [InlineData(AirOnly, null, null)]
    public async Task Backoffice_carries_both_selection_ids_from_json_to_the_create_args(string json, string? ancillary, string? seats)
    {
        await new BackofficeController(_mediator).CreateFromOffer(JsonSerializer.Deserialize<BackofficeCreateOrderFromOfferRequest>(json, WebJson)!, CancellationToken.None);

        await new BackofficeCreateOrderFromOfferCommandHandler(_service, _salesContext, _input)
            .Handle((BackofficeCreateOrderFromOfferCommand)_mediator.Sent!, CancellationToken.None);

        Assert.Equal((ancillary, seats), (_service.Args!.AncillaryPricedSelectionId, _service.Args.SeatPricedSelectionId));
    }

    [Theory]
    [InlineData(WithSelections, "s_general", "s_seats")]
    [InlineData(AirOnly, null, null)]
    public async Task Ota_carries_both_selection_ids_from_json_to_the_create_args(string json, string? ancillary, string? seats)
    {
        await new OtaController(_mediator).CreateFromOffer(JsonSerializer.Deserialize<AgencyCreateOrderFromOfferRequest>(json, WebJson)!, CancellationToken.None);

        await new OtaCreateOrderFromOfferCommandHandler(_service, AgencyMapper())
            .Handle((OtaCreateOrderFromOfferCommand)_mediator.Sent!, CancellationToken.None);

        Assert.Equal((ancillary, seats), (_service.Args!.AncillaryPricedSelectionId, _service.Args.SeatPricedSelectionId));
    }

    [Theory]
    [InlineData(WithSelections, "s_general", "s_seats")]
    [InlineData(AirOnly, null, null)]
    public async Task OtaPanel_carries_both_selection_ids_from_json_to_the_create_args(string json, string? ancillary, string? seats)
    {
        await new OtaPanelController(_mediator).CreateFromOffer(JsonSerializer.Deserialize<AgencyCreateOrderFromOfferRequest>(json, WebJson)!, CancellationToken.None);

        await new OtaPanelCreateOrderFromOfferCommandHandler(_service, AgencyMapper())
            .Handle((OtaPanelCreateOrderFromOfferCommand)_mediator.Sent!, CancellationToken.None);

        Assert.Equal((ancillary, seats), (_service.Args!.AncillaryPricedSelectionId, _service.Args.SeatPricedSelectionId));
    }

    [Fact]
    public void An_empty_selection_id_is_invalid_on_every_surface_and_an_absent_one_is_not()
    {
        Assert.Equal(
            [nameof(BackofficeCreateOrderFromOfferCommand.AncillaryPricedSelectionId), nameof(BackofficeCreateOrderFromOfferCommand.SeatPricedSelectionId)],
            SelectionErrors(new BackofficeCreateOrderFromOfferCommandValidator().Validate(Backoffice(string.Empty, " "))));
        Assert.Equal(2, SelectionErrors(new OtaCreateOrderFromOfferCommandValidator().Validate(Ota(string.Empty, " "))).Count);
        Assert.Equal(2, SelectionErrors(new OtaPanelCreateOrderFromOfferCommandValidator().Validate(OtaPanel(string.Empty, " "))).Count);

        Assert.Empty(SelectionErrors(new BackofficeCreateOrderFromOfferCommandValidator().Validate(Backoffice(null, null))));
        Assert.Empty(SelectionErrors(new OtaCreateOrderFromOfferCommandValidator().Validate(Ota(null, null))));
        Assert.Empty(SelectionErrors(new OtaPanelCreateOrderFromOfferCommandValidator().Validate(OtaPanel(null, null))));
    }

    [Fact]
    public void The_public_create_result_keeps_its_shape_and_exposes_no_source_ids()
    {
        Assert.Equal(
            [nameof(CreateOrderFromOfferResult.OrderId), nameof(CreateOrderFromOfferResult.OrderReference), nameof(CreateOrderFromOfferResult.CurrencyId), nameof(CreateOrderFromOfferResult.CustomerTotal), nameof(CreateOrderFromOfferResult.CommercialVersion)],
            typeof(CreateOrderFromOfferResult).GetProperties().Where(property => property.Name != "EqualityContract").Select(property => property.Name));
    }

    private static IReadOnlyList<string> SelectionErrors(FluentValidation.Results.ValidationResult result)
        => result.Errors
            .Select(error => error.PropertyName)
            .Where(name => name.EndsWith("PricedSelectionId", StringComparison.Ordinal))
            .ToList();

    private static BackofficeCreateOrderFromOfferCommand Backoffice(string? ancillary, string? seats)
        => new("OFFER-1", 42, Contact, [], [], ancillary, seats);

    private static OtaCreateOrderFromOfferCommand Ota(string? ancillary, string? seats)
        => new("OFFER-1", Contact, [], [], ancillary, seats);

    private static OtaPanelCreateOrderFromOfferCommand OtaPanel(string? ancillary, string? seats)
        => new("OFFER-1", Contact, [], [], ancillary, seats);

    private AgencyCreateOrderArgsMapper AgencyMapper() => new(_salesContext, new FixedCaller(), _input);

    private sealed class CapturingCreateService : ICreateOrderFromOfferService
    {
        public CreateOrderArgs? Args { get; private set; }

        public Task<CreateOrderFromOfferResult> ExecuteAsync(CreateOrderArgs args, CancellationToken cancellationToken = default)
        {
            Args = args;

            return Task.FromResult(Result);
        }
    }

    private sealed class FixedCaller : ICallerCustomer
    {
        public long RequiredId() => 42;
    }

    private sealed class NoCountries : ICountryCodeResolver
    {
        public Task<int> ResolveAsync(string alpha2Code, CancellationToken cancellationToken = default) => throw new NotSupportedException();

        public Task<int?> ResolveOptionalAsync(string? alpha2Code, CancellationToken cancellationToken = default) => Task.FromResult<int?>(null);
    }
}
