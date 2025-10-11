namespace DestallMaterials.Tests;

using global::DestallMaterials.WheelProtection.DataStructures.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

public class BorrowedListTests : IDisposable
{
    private BorrowedList<int> _list;

    public BorrowedListTests()
    {
        _list = new BorrowedList<int>();
    }

    public void Dispose()
    {
        _list?.Dispose();
    }

    [Fact]
    public void Constructor_Default_CreatesEmptyList()
    {
        // Arrange & Act
        using var list = new BorrowedList<int>();

        // Assert
        Assert.Equal(0, list.Count);
        Assert.False(list.IsReadOnly);
    }

    [Fact]
    public void Constructor_WithCapacity_CreatesListWithSpecifiedCapacity()
    {
        // Arrange & Act
        using var list = new BorrowedList<int>(10);

        // Assert
        Assert.Equal(0, list.Count);
    }

    [Fact]
    public void Constructor_WithNegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new BorrowedList<int>(-1));
    }

    [Fact]
    public void Constructor_WithCollection_PopulatesListCorrectly()
    {
        // Arrange
        var source = new[] { 1, 2, 3, 4, 5 };

        // Act
        using var list = new BorrowedList<int>(source);

        // Assert
        Assert.Equal(5, list.Count);
        Assert.Equal(source, list.ToArray());
    }

    [Fact]
    public void Constructor_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange, Act & Assert
        Assert.Throws<ArgumentNullException>(() => new BorrowedList<int>((IEnumerable<int>)null));
    }

    [Fact]
    public void Add_SingleElement_IncreasesCount()
    {
        // Act
        _list.Add(42);

        // Assert
        Assert.Equal(1, _list.Count);
        Assert.Equal(42, _list[0]);
    }

    [Fact]
    public void Add_MultipleElements_AddsInOrder()
    {
        // Arrange
        var values = new[] { 1, 2, 3, 4, 5 };

        // Act
        foreach (var value in values)
        {
            _list.Add(value);
        }

        // Assert
        Assert.Equal(5, _list.Count);
        for (int i = 0; i < values.Length; i++)
        {
            Assert.Equal(values[i], _list[i]);
        }
    }

    [Fact]
    public void Add_ManyElements_HandlesCapacityExpansion()
    {
        // Act - Add more elements than initial capacity
        for (int i = 0; i < 100; i++)
        {
            _list.Add(i);
        }

        // Assert
        Assert.Equal(100, _list.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i, _list[i]);
        }
    }

    [Fact]
    public void Indexer_Get_ValidIndex_ReturnsCorrectElement()
    {
        // Arrange
        _list.Add(10);
        _list.Add(20);
        _list.Add(30);

        // Act & Assert
        Assert.Equal(10, _list[0]);
        Assert.Equal(20, _list[1]);
        Assert.Equal(30, _list[2]);
    }

    [Fact]
    public void Indexer_Get_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _list.Add(42);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _list[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _list[1]);
    }

    [Fact]
    public void Indexer_Set_ValidIndex_UpdatesElement()
    {
        // Arrange
        _list.Add(10);
        _list.Add(20);

        // Act
        _list[1] = 99;

        // Assert
        Assert.Equal(99, _list[1]);
        Assert.Equal(10, _list[0]); // Other elements unchanged
    }

    [Fact]
    public void Indexer_Set_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _list.Add(42);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _list[-1] = 99);
        Assert.Throws<ArgumentOutOfRangeException>(() => _list[1] = 99);
    }

    [Fact]
    public void Clear_RemovesAllElements()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);
        _list.Add(3);

        // Act
        _list.Clear();

        // Assert
        Assert.Equal(0, _list.Count);
    }

    [Fact]
    public void Contains_ExistingElement_ReturnsTrue()
    {
        // Arrange
        _list.Add(42);
        _list.Add(99);

        // Act & Assert
        Assert.True(_list.Contains(42));
        Assert.True(_list.Contains(99));
    }

    [Fact]
    public void Contains_NonExistingElement_ReturnsFalse()
    {
        // Arrange
        _list.Add(42);

        // Act & Assert
        Assert.False(_list.Contains(99));
    }

    [Fact]
    public void CopyTo_ValidArray_CopiesElements()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);
        _list.Add(3);
        var array = new int[5];

        // Act
        _list.CopyTo(array, 1);

        // Assert
        Assert.Equal(new[] { 0, 1, 2, 3, 0 }, array);
    }

    [Fact]
    public void CopyTo_NullArray_ThrowsArgumentNullException()
    {
        // Arrange
        _list.Add(42);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _list.CopyTo(null, 0));
    }

    [Fact]
    public void CopyTo_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _list.Add(42);
        var array = new int[2];

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _list.CopyTo(array, -1));
    }

    [Fact]
    public void CopyTo_InsufficientSpace_ThrowsArgumentException()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);
        _list.Add(3);
        var array = new int[2];

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _list.CopyTo(array, 0));
    }

    [Fact]
    public void IndexOf_ExistingElement_ReturnsCorrectIndex()
    {
        // Arrange
        _list.Add(10);
        _list.Add(20);
        _list.Add(30);
        _list.Add(20); // Duplicate

        // Act & Assert
        Assert.Equal(1, _list.IndexOf(20)); // Returns first occurrence
        Assert.Equal(2, _list.IndexOf(30));
    }

    [Fact]
    public void IndexOf_NonExistingElement_ReturnsMinusOne()
    {
        // Arrange
        _list.Add(10);
        _list.Add(20);

        // Act & Assert
        Assert.Equal(-1, _list.IndexOf(99));
    }

    [Fact]
    public void Insert_AtBeginning_InsertsCorrectly()
    {
        // Arrange
        _list.Add(2);
        _list.Add(3);

        // Act
        _list.Insert(0, 1);

        // Assert
        Assert.Equal(3, _list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, _list.ToArray());
    }

    [Fact]
    public void Insert_AtMiddle_InsertsCorrectly()
    {
        // Arrange
        _list.Add(1);
        _list.Add(3);

        // Act
        _list.Insert(1, 2);

        // Assert
        Assert.Equal(3, _list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, _list.ToArray());
    }

    [Fact]
    public void Insert_AtEnd_InsertsCorrectly()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);

        // Act
        _list.Insert(2, 3);

        // Assert
        Assert.Equal(3, _list.Count);
        Assert.Equal(new[] { 1, 2, 3 }, _list.ToArray());
    }

    [Fact]
    public void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _list.Add(42);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _list.Insert(-1, 99));
        Assert.Throws<ArgumentOutOfRangeException>(() => _list.Insert(2, 99));
    }

    [Fact]
    public void Remove_ExistingElement_RemovesAndReturnsTrue()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);
        _list.Add(3);

        // Act
        bool result = _list.Remove(2);

        // Assert
        Assert.True(result);
        Assert.Equal(2, _list.Count);
        Assert.Equal(new[] { 1, 3 }, _list.ToArray());
    }

    [Fact]
    public void Remove_NonExistingElement_ReturnsFalse()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);

        // Act
        bool result = _list.Remove(99);

        // Assert
        Assert.False(result);
        Assert.Equal(2, _list.Count);
    }

    [Fact]
    public void RemoveAt_ValidIndex_RemovesElement()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);
        _list.Add(3);

        // Act
        _list.RemoveAt(1);

        // Assert
        Assert.Equal(2, _list.Count);
        Assert.Equal(new[] { 1, 3 }, _list.ToArray());
    }

    [Fact]
    public void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        _list.Add(42);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => _list.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => _list.RemoveAt(1));
    }

    [Fact]
    public void GetEnumerator_IteratesAllElements()
    {
        // Arrange
        var expected = new[] { 1, 2, 3, 4, 5 };
        foreach (var item in expected)
        {
            _list.Add(item);
        }

        // Act
        var result = _list.ToArray();

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Dispose_DisposesUnderlyingBuffers()
    {
        // Arrange
        _list.Add(1);
        _list.Add(2);

        // Act
        _list.Dispose();

        // Assert - All operations should throw ObjectDisposedException
        Assert.Throws<ObjectDisposedException>(() => _list.Add(3));
        Assert.Throws<ObjectDisposedException>(() => _list[0]);
        Assert.Throws<ObjectDisposedException>(() => _list.Count);
        Assert.Throws<ObjectDisposedException>(() => _list.Contains(1));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Act & Assert
        _list.Dispose();
        _list.Dispose(); // Should not throw
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void StressTest_AddAndRemoveMany_WorksCorrectly(int count)
    {
        using var list = new BorrowedList<int>();

        // Add elements
        for (int i = 0; i < count; i++)
        {
            list.Add(i);
        }
        Assert.Equal(count, list.Count);

        // Verify elements
        for (int i = 0; i < count; i++)
        {
            Assert.Equal(i, list[i]);
        }

        // Remove elements from end
        for (int i = count - 1; i >= 0; i--)
        {
            list.RemoveAt(i);
            Assert.Equal(i, list.Count);
        }
    }

    [Fact]
    public void UsingStatement_AutomaticallyDisposes()
    {
        BorrowedList<int> capturedList = null;

        // Act
        using (var list = new BorrowedList<int>())
        {
            list.Add(42);
            capturedList = list;
        } // Dispose called automatically

        // Assert
        Assert.Throws<ObjectDisposedException>(() => capturedList.Add(1));
    }
}
