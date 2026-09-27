using AeroTech.Framework.Core.Domain.Exceptions;
using AeroTech.Messages.Ordering.Enums;
using AeroTech.Ordering.Application.AcceptanceTests.Fakes;
using AeroTech.Ordering.Domain.DocumentStockAggregate;
using Xunit;

namespace AeroTech.Ordering.Application.AcceptanceTests.Issuance;

public sealed class DocumentStockDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 1, 10, 0, 0, TimeSpan.Zero);

    private readonly SequentialIdGenerator _ids = new();

    [Fact]
    public void Valid_definition_is_active_from_the_start_of_its_range()
    {
        var stock = Define(rangeFrom: 500, rangeTo: 599);

        Assert.Equal((DocumentStockStatus.Active, 500L, 100L), (stock.Status, stock.NextNumber, stock.RemainingNumbers));
        Assert.Empty(stock.Allocations);
    }

    [Theory]
    [InlineData(0L, 10L, 10, "0991")]
    [InlineData(20L, 10L, 10, "0991")]
    [InlineData(1L, 10L, 0, "0991")]
    [InlineData(1L, 10L, 10, " ")]
    [InlineData(1L, 1000L, 3, "0991")]
    public void Invalid_range_is_rejected(long rangeFrom, long rangeTo, int serialWidth, string prefix)
    {
        var exception = Assert.Throws<BusinessException>(() => Define(rangeFrom, rangeTo, serialWidth, prefix));

        Assert.Equal(2791, exception.Code);
    }

    [Theory]
    [InlineData("Mod7")]
    [InlineData("IATA")]
    public void Unsupported_check_digit_profile_fails_explicitly(string checkDigitProfile)
    {
        var exception = Assert.Throws<BusinessException>(() => DocumentStock.Define(_ids.NewId(), 10, 5, AccountableDocumentKind.ElectronicTicket, "0991", 10, checkDigitProfile, 1, 10));

        Assert.Equal(2790, exception.Code);
    }

    [Fact]
    public void First_allocation_uses_the_start_of_the_range_left_padded_after_the_prefix()
    {
        var allocation = Define(rangeFrom: 42, rangeTo: 99).Allocate(7, "ETKT:1:1", _ids, Now);

        Assert.Equal((42L, "09910000000042", StockNumberState.Reserved), (allocation.Serial, allocation.DocumentNumber, allocation.State));
    }

    [Fact]
    public void Next_role_takes_the_next_serial()
    {
        var stock = Define();

        var first = stock.Allocate(7, "ETKT:1:1", _ids, Now);
        var second = stock.Allocate(7, "ETKT:2:1", _ids, Now);

        Assert.Equal((1L, 2L, 3L), (first.Serial, second.Serial, stock.NextNumber));
    }

    [Fact]
    public void Same_task_and_role_return_the_same_allocation()
    {
        var stock = Define();

        var first = stock.Allocate(7, "ETKT:1:1", _ids, Now);
        var again = stock.Allocate(7, "ETKT:1:1", _ids, Now);

        Assert.Same(first, again);
        Assert.Equal((2L, 1), (stock.NextNumber, stock.Allocations.Count));
    }

    [Fact]
    public void Last_number_exhausts_the_stock_and_no_further_number_is_given()
    {
        var stock = Define(rangeFrom: 1, rangeTo: 2);

        stock.Allocate(7, "ETKT:1:1", _ids, Now);
        stock.Allocate(7, "ETKT:2:1", _ids, Now);
        var exception = Assert.Throws<BusinessException>(() => stock.Allocate(7, "ETKT:3:1", _ids, Now));

        Assert.Equal((DocumentStockStatus.Exhausted, 2788), (stock.Status, exception.Code));
    }

    [Fact]
    public void Issued_numbers_are_never_recycled()
    {
        var stock = Define();
        var issued = stock.Allocate(7, "ETKT:1:1", _ids, Now);

        stock.MarkIssued(issued.Id, Now);
        var next = stock.Allocate(8, "ETKT:1:1", _ids, Now);

        Assert.Equal((StockNumberState.Issued, Now), (issued.State, issued.SettledAt!.Value));
        Assert.NotEqual(issued.Serial, next.Serial);
        Assert.Equal(3L, stock.NextNumber);
    }

    [Fact]
    public void Retired_numbers_are_never_recycled()
    {
        var stock = Define();
        var retired = stock.Allocate(7, "ETKT:1:1", _ids, Now);

        stock.Retire(retired.Id, Now);
        var replacement = stock.Allocate(7, "ETKT:1:1", _ids, Now);

        Assert.Equal(StockNumberState.Retired, retired.State);
        Assert.Equal((2L, 3L), (replacement.Serial, stock.NextNumber));
    }

    [Theory]
    [InlineData(1L, 100L, true)]
    [InlineData(100L, 150L, true)]
    [InlineData(40L, 60L, true)]
    [InlineData(101L, 200L, false)]
    public void Overlap_is_detected_against_the_whole_range(long rangeFrom, long rangeTo, bool overlaps)
        => Assert.Equal(overlaps, Define(rangeFrom: 50, rangeTo: 100).Overlaps(rangeFrom, rangeTo));

    [Fact]
    public void Stock_of_another_kind_cannot_issue_tickets()
    {
        var stock = DocumentStock.Define(_ids.NewId(), 10, 5, AccountableDocumentKind.ElectronicMiscDocument, "0992", 10, DocumentStock.NoCheckDigitProfile, 1, 10);

        var exception = Assert.Throws<BusinessException>(() => stock.EnsureCanIssue(AccountableDocumentKind.ElectronicTicket, 1));

        Assert.Equal(2786, exception.Code);
    }

    private DocumentStock Define(long rangeFrom = 1, long rangeTo = 999, int serialWidth = 10, string prefix = "0991")
        => DocumentStock.Define(_ids.NewId(), 10, 5, AccountableDocumentKind.ElectronicTicket, prefix, serialWidth, DocumentStock.NoCheckDigitProfile, rangeFrom, rangeTo);
}
