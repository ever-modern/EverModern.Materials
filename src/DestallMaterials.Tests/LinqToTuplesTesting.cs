using DestallMaterials.WheelProtection.Linq;
using TupleType = (int, int, int, int, int);

namespace DestallMaterials.Tests;

public class LinqToTuplesTesting
{
    static TupleType _items = (3, 10, 20, 50, 51);

    [Test]
    public void Methods_MustYieldSameResults_AsClassicLinq()
    {
        List<(Func<int[], object>, Func<TupleType, object>)> checks = [
                (items => items.Any(i => i > 100), _items => _items.Any(i => i > 100)),
                (items => items.Any(i => i > 49), items => items.Any(i => i > 49)),
                (items => items.All(i => i > 3), items => items.All(i => i > 3)),
                (items => items.All(i => i > 1), items => items.All(i => i > 1)),
                (items => items.Sum(), items => items.Sum()),
                (items => items.Sum(i => i % 3), items => items.Sum(i => i % 3)),
                (items => items.Contains(items[2]), items => items.Contains(items.At(2))),
                (items => items.Count(i => i % 2 == 0), items => items.Count(i => i % 2 == 0)),
                (items => items.First(i => i % 4 + i % 5 == 0), items => items.First(i => i % 4 + i % 5 == 0))
            ];

        var itemsArray = _items.AsEnumerable().ToArray();

        foreach (var check in checks)
        {
            var (arrayCheck, tupleCheck) = check;

            Assert.AreEqual(arrayCheck(itemsArray), tupleCheck(_items));
        }

    }
}