using AeroTech.Ordering.Domain._Shared.Resources;
using Xunit;
using static AeroTech.Ordering.Domain.ConformanceTests.Stage1TypeSetConformanceTests;

namespace AeroTech.Ordering.Domain.ConformanceTests;

public sealed class Stage5DomainClosureConformanceTests
{
    private const string MasterFileName = "AeroTech-Ordering-Master-Domain-ADR-PRD-v2.0-FINAL-PROJECT-AUTHORITY.md";

    private readonly string _master = File.ReadAllText(Path.Combine(FindRepoRoot(), "docs", MasterFileName));

    [Fact]
    public void Master_v2_0_remains_the_single_domain_authority()
    {
        var masters = new DirectoryInfo(Path.Combine(FindRepoRoot(), "docs"))
            .GetFiles("AeroTech-Ordering-Master-Domain-ADR-PRD-*.md")
            .Select(file => file.Name)
            .ToArray();

        Assert.Equal([MasterFileName], masters);
        Assert.Contains("In-place amendment — Stage 5 R2 Domain Closure", _master);
    }

    [Fact]
    public void Master_defines_order_change_reason_text()
    {
        Assert.Contains("| `ReasonText` | `string?` | Yes | NOW (Stage 5 R2) |", _master);
        Assert.Contains("maximum length 500 characters, after trimming", _master);
        Assert.Contains("`request.ReasonDetail → OrderChange.ReasonText`", _master);
    }

    [Fact]
    public void Master_states_that_the_reservation_root_status_is_summary_only_for_issue()
    {
        Assert.Contains("`FulfillmentReservation.Status` is summary only.", _master);
        Assert.Contains("a `Mixed` root may be issueable for a confirmed target subset", _master);
        Assert.Contains("required issue-scope ReservationUnit(s) must be Confirmed, not the whole FulfillmentReservation root", _master);
    }

    [Fact]
    public void Master_defines_scope_and_version_aware_validation_evidence()
    {
        Assert.Contains("## 13.6 `ReservationValidationEvidence`", _master);
        Assert.Contains("Evidence.CommercialVersion == Order.CommercialVersion", _master);
        Assert.Contains("S ⊆ Evidence.ValidatedOrderServiceIds", _master);
        Assert.Contains("Issue**: refresh is **validation-only**", _master);
        Assert.Contains("evidence is never backfilled with a guessed scope or version", _master);
    }

    [Fact]
    public void Master_defines_the_partial_cancellation_pricing_atom_rule()
    {
        Assert.Contains("## 7.4 `FarePricingAtom`", _master);
        Assert.Contains("It is **not a persisted entity**", _master);
        Assert.Contains("| `RoundTrip`, `OpenJaw`, `CircleTrip` | The whole pricing unit is one atom", _master);
        Assert.Contains("| `Unspecified`, `Other` | The whole pricing unit is one conservative atom.", _master);
        Assert.Contains("`REPRICING_REQUIRED_AFTER_PARTIAL_CANCELLATION` (Ordering code 2820, HTTP 409)", _master);
    }

    [Fact]
    public void Source_materializes_the_stage5_closure_decisions()
    {
        Assert.Equal(typeof(string), FindDomainType("OrderChange")!.GetProperty("ReasonText")!.PropertyType);

        var evidence = FindDomainType("ReservationValidationEvidence")!;
        Assert.Equal(evidence, FindDomainType("FulfillmentReservation")!.GetProperty("ValidationEvidence")!.PropertyType);
        Assert.Equal(
            [("CommercialVersion", typeof(int)), ("ValidUntil", typeof(DateTimeOffset)), ("ValidatedAt", typeof(DateTimeOffset)), ("ValidatedOrderServiceIds", typeof(IReadOnlyCollection<long>))],
            new[] { "CommercialVersion", "ValidUntil", "ValidatedAt", "ValidatedOrderServiceIds" }.Select(name => (name, evidence.GetProperty(name)!.PropertyType)));

        var atom = FindDomainType("FarePricingAtom")!;
        Assert.DoesNotContain(atom, DomainModelTypes());
        Assert.Equal(typeof(IReadOnlyList<>).MakeGenericType(atom), FindDomainType("OrderFarePricingUnit")!.GetMethod("PricingAtoms")!.ReturnType);

        var fracture = ExceptionFactory.RepricingRequiredAfterPartialCancellation(1L, 2L);
        Assert.Equal((2820, 409), (fracture.Code, fracture.HttpStatus));
    }
}
