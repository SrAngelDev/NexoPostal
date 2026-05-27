using FluentAssertions;
using Nexopostal.Shared.Dtos.Common;
using Xunit;

namespace Nexopostal.Tests.Shared;

public class PagedResultTests
{
    [Fact]
    public void TotalPages_PageSizeCero_DevuelveCero()
    {
        new PagedResult<int> { PageSize = 0, TotalCount = 100 }.TotalPages.Should().Be(0);
    }

    [Theory]
    [InlineData(100, 10, 10)]
    [InlineData(101, 10, 11)]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    public void TotalPages_CalculoCorrecto(int total, int size, int expected)
    {
        new PagedResult<string> { PageSize = size, TotalCount = total }.TotalPages.Should().Be(expected);
    }

    [Theory]
    [InlineData(1, 10, 100, true, false)]   // primera
    [InlineData(5, 10, 100, true, true)]    // intermedia
    [InlineData(10, 10, 100, false, true)]  // última
    [InlineData(1, 10, 5, false, false)]    // una sola
    public void HasNextAndPrevious(int page, int size, int total, bool hasNext, bool hasPrev)
    {
        var p = new PagedResult<int> { Page = page, PageSize = size, TotalCount = total };
        p.HasNext.Should().Be(hasNext);
        p.HasPrevious.Should().Be(hasPrev);
    }

    [Fact]
    public void Items_DefaultVacio()
    {
        new PagedResult<string>().Items.Should().BeEmpty();
    }

    [Fact]
    public void PageFilter_DefaultValues()
    {
        var f = new PageFilter();
        f.Page.Should().Be(0);
        f.Size.Should().Be(10);
        f.SortBy.Should().BeNull();
        f.Direction.Should().Be("asc");
    }
}
