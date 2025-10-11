namespace DestallMaterials.EnlightenedDataProvision.Tests.Samples;

public class TestTableLine : TestBaseEntity, IOwnedEntity<long, BigEntity>
{
    public long OwnerId { get; set; }
}
