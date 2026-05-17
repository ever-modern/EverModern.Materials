using EverModern.DataProvision.Abstractions;
using LinqToDB.Async;

namespace EverModern.DataProvision.Tests;

public class DeferredQueryableTests : QueryableTestsBase
{
    [Fact]
    public async Task ToArrayAsync_UsesFactoryConnection()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> queryable = GetQueryable();

        var items = await queryable.ToArrayAsync();

        Assert.NotEqual(0, items.Length);
    }

    [Fact]
    public async Task Where_FiltersResults()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> queryable = GetQueryable();

        var items = await queryable
            .Where(x => x.Id == 1)
            .ToArrayAsync();

        Assert.Single(items);
        Assert.Equal(1, items[0].Id);
    }

    [Fact]
    public async Task Select_ProjectsResults()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> queryable = GetQueryable();

        var names = await queryable
            .Select(x => x.Name)
            .ToArrayAsync();

        Assert.Equal(3, names.Length);
        Assert.Contains("One", names);
        Assert.Contains("Two", names);
        Assert.Contains("Three", names);
    }

    [Fact]
    public async Task OrderBy_OrdersResults()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> queryable = GetQueryable();

        var items = await queryable
            .OrderByDescending(x => x.Id)
            .ToArrayAsync();

        Assert.Equal(3, items.Length);
        Assert.Equal(3, items[0].Id);
        Assert.Equal(2, items[1].Id);
        Assert.Equal(1, items[2].Id);
    }

    [Fact]
    public async Task OrderBy_ThenBy_OrdersResults()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> queryable = GetQueryable();

        var items = await queryable
            .OrderBy(x => x.Name.Length)
            .ThenBy(x => x.Id)
            .ToArrayAsync();

        Assert.Equal(3, items.Length);
        // "One" and "Two" both have length 3, sorted by Id; "Three" has length 5
        Assert.Equal("One", items[0].Name);
        Assert.Equal("Two", items[1].Name);
        Assert.Equal("Three", items[2].Name);
    }

    [Fact]
    public async Task GroupJoin_CorrelatesResults()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> outer = GetQueryable();
        IReadOnlyAsyncQueryable<DeferredQueryableItem> inner = GetQueryable();

        var results = await outer
            .GroupJoin(
                inner,
                o => o.Id,
                i => i.Id,
                (o, matches) => new { o.Id, MatchCount = matches.Count() })
            .ToArrayAsync();

        Assert.Equal(3, results.Length);
        Assert.All(results, r => Assert.Equal(1, r.MatchCount));
    }

    [Fact]
    public async Task LeftJoin_IncludesOuterRowsWithNoMatch()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> outer = GetQueryable();
        // inner has only Id=1
        IReadOnlyAsyncQueryable<DeferredQueryableItem> innerFull = GetQueryable();
        IReadOnlyAsyncQueryable<DeferredQueryableItem> inner = innerFull.Where(x => x.Id == 1);

        var results = await outer
            .LeftJoin(
                inner,
                o => o.Id,
                i => i.Id,
                (o, i) => new { OuterId = o.Id, InnerName = i == null ? null : i.Name })
            .ToArrayAsync();

        Assert.Equal(3, results.Length);
        var matched = results.Single(r => r.OuterId == 1);
        Assert.Equal("One", matched.InnerName);
        var unmatched = results.Where(r => r.OuterId != 1).ToArray();
        Assert.All(unmatched, r => Assert.Null(r.InnerName));
    }

    [Fact]
    public async Task Where_Then_Select_ChainWorks()
    {
        IReadOnlyAsyncQueryable<DeferredQueryableItem> queryable = GetQueryable();

        var names = await queryable
            .Where(x => x.Id > 1)
            .Select(x => x.Name)
            .ToListAsync();

        Assert.Equal(2, names.Count);
        Assert.Contains("Two", names);
        Assert.Contains("Three", names);
    }
}
