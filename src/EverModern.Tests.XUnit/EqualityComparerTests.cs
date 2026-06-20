namespace EverModern.Tests.XUnit;

public class EqualityComparerTests
{
    [Fact]
    public void TestWithDictionary()
    {
        var equalityComparer = ReferenceEqualityComparer.Instance;

        var dict = new HashSet<string>(equalityComparer);

        var key = "abc";

        dict.Add(key);

        var newKey = string.Concat(["a", "b", "c"]);

        var (hashCode1, hashCode2) = (equalityComparer.GetHashCode(key), equalityComparer.GetHashCode(newKey));

        Assert.Equal(key, newKey);
        Assert.False(ReferenceEquals(key, newKey));
        Assert.False(dict.Contains(newKey));
        Assert.NotEqual(hashCode1, hashCode2);

        newKey = string.Intern(newKey);
        (hashCode1, hashCode2) = (equalityComparer.GetHashCode(key), equalityComparer.GetHashCode(newKey));

        Assert.Equal(key, newKey);
        Assert.True(ReferenceEquals(key, newKey));
        Assert.True(dict.Contains(newKey));
        Assert.Equal(hashCode1, hashCode2);
    }

    [Fact]
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

        Assert.Equal(item1, item2);
        Assert.Equal(item1.GetHashCode(), item2.GetHashCode());

        Assert.NotEqual(equalityComparer.GetHashCode(item1), equalityComparer.GetHashCode(item2));
        Assert.False(equalityComparer.Equals(item1, item2));
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
