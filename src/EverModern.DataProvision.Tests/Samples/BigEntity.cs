using EverModern.DataProvision.Abstractions;

namespace EverModern.DataProvision.Tests.Samples;

public abstract class TestBaseEntity : IEntity<long>
{
    public long Id { get; set; }
}

public class BigEntity : TestBaseEntity
{
    static long _lastId = 1;

    public static bool IdIsTemporary(long id)
        => id < 1;

    public ICollection<TestTableLine> OwnedLines { get; set; } = [];
}
