using DestallMaterials.WheelProtection.DataStructures;

namespace DestallMaterials.Tests;

public class EqualityComparer
{
    [Test]
    public void TestWithDictionary()
    {
        var equalityComparer = ReferenceEqualityComparer.Instance;

        var dict = new HashSet<string>(equalityComparer);

        var key = "abc";

        dict.Add(key);

        var newKey = string.Concat(["a", "b", "c"]);

        var (hashCode1, hashCode2) = (equalityComparer.GetHashCode(key), equalityComparer.GetHashCode(newKey));

        Assert.AreEqual(key, newKey);
        Assert.IsFalse(ReferenceEquals(key, newKey));
        Assert.IsFalse(dict.Contains(newKey));
        Assert.AreNotEqual(hashCode1, hashCode2);

        newKey = string.Intern(newKey);
        (hashCode1, hashCode2) = (equalityComparer.GetHashCode(key), equalityComparer.GetHashCode(newKey));

        Assert.AreEqual(key, newKey);
        Assert.IsTrue(ReferenceEquals(key, newKey));
        Assert.IsTrue(dict.Contains(newKey));
        Assert.AreEqual(hashCode1, hashCode2);
    }

    [Test]
    public void GetHashCode_MustIgnoreOverriden()
    {
        var equalityComparer = ReferenceEqualityComparer.Instance;

        var item1 = new OverridingClass
        {
            Value = 1
        };

        var item2 = new OverridingClass
        {
            Value = 1
        };

        Assert.AreEqual(item1, item2);
        Assert.AreEqual(item1.GetHashCode(), item2.GetHashCode());

        Assert.AreNotEqual(equalityComparer.GetHashCode(item1), equalityComparer.GetHashCode(item2));
        Assert.IsFalse(equalityComparer.Equals(item1, item2));
    }

    class OverridingClass
    {
        public int Value { get; set; }
        public override int GetHashCode()
            => Value;

        public override bool Equals(object? obj)
            => obj is OverridingClass other && other.Value == Value;
    }
}
