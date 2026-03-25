using EverModern.DataProvision.Abstractions;

namespace EverModern.DataProvision.Tests.Samples;

public class TestTableLine : TestBaseEntity, IOwnedEntity<long, BigEntity>
{
    public long OwnerId { get; set; }
}
