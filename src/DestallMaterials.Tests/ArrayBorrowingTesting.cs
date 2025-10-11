using DestallMaterials.WheelProtection.DataStructures.Buffers;
using DestallMaterials.WheelProtection.Extensions.Ranges;
using DestallMaterials.WheelProtection.Linq;

namespace DestallMaterials.Tests;

public class ArrayBorrowingTesting
{
    [Test]
    public void ArrayBorrower_GetRange()
    {
        const int size = 100;

        int[] arr = (0..size)
            .AsSequence()
            .ToArray();

        var borrowedArray = new BorrowedArray<int>(arr, size);

        var combs = (0..size)
            .Select(n => (0..n)
                .Select(i => (arr[i..n], borrowedArray[i..n])));

        int i = 0;

        foreach (var comb1 in combs)
        {
            foreach (var comb2 in comb1)
            {
                i++;
                Assert.True(comb2.Item1.SequenceEqual(comb2.Item2));
            }
        }

        Assert.AreEqual((size - 1) * size / 2, i);
    }

    [Test]
    public void ArrayBorrower_GetRange_FromEndIndexing()
    {
        const int size = 100;

        int[] arr = (0..size)
            .AsSequence(size)
            .ToArray();

        var borrowedArray = new BorrowedArray<int>(arr, size);

        var combs = (0..size)
            .Select(n => (0..n)
                .Select(i =>
                {
                    var range = ^n..^i;

                    return (arr[range], borrowedArray[range]);
                }));

        int i = 0;

        foreach (var comb1 in combs)
        {
            foreach (var comb2 in comb1)
            {
                i++;
                Assert.True(comb2.Item1.SequenceEqual(comb2.Item2));
            }
        }

        Assert.AreEqual((size - 1) * size / 2, i);
    }

    [Test]
    public void ArrayBorrower_ComputeRentedArraySize()
    {
        var expectedResults = (
                    (128, 200, 256),
                    (20, 31, 40),
                    (15, 16, 30),
                    (20, 10, 20)
                );

        foreach (var act in expectedResults)
        {
            var actual = ArrayBorrower<object>.ComputeRentedArraySize(act.Item2, act.Item1);
            Assert.AreEqual(act.Item3, actual);
        }
    }

    [Test]
    public void Create_ThenBorrow()
    {
        const int borrowedSize = 200;

        var borrower = new ArrayBorrower<byte>()
        {
            Discriminator = 128
        };

        var arr = borrower.Borrow(borrowedSize);
        var arr1 = borrower.Borrow(borrowedSize);

        Span<byte> spanArr = arr;

        arr[0] = 45;

        Assert.AreNotEqual(arr1[0], arr[0]);

        arr.Free();

        var arr2 = borrower.Borrow(borrowedSize);

        Assert.AreEqual(45, arr2[0]);

        arr2.Free();
        arr1.Free();
    }

    [Test]
    public void OneArrayWriter_GetMemory()
    {
        int[] arr = (0, 0, 0, 0, 0, 0).ToArray();

        var writer = OneArrayWriter.Create(arr);

        var mem1 = writer.GetMemory(1);
        new int[] { 100 }.AsMemory().CopyTo(mem1);

        Assert.AreEqual(100, arr[0]);

        writer.Advance(1);
        var mem2 = writer.GetMemory(2);
        new int[] { 200, 300 }.CopyTo(mem2);

        Assert.AreEqual(200, arr[1]);
        Assert.AreEqual(300, arr[2]);

        writer.Advance(2);
        var mem3 = writer.GetMemory(3);
        new int[] { 400, 500, 600 }.CopyTo(mem3);

        Assert.AreEqual(400, arr[3]);
        Assert.AreEqual(500, arr[4]);
        Assert.AreEqual(600, arr[5]);

        writer.Advance(3);

        Assert.Throws<IndexOutOfRangeException>(() => writer.Advance(1));
    }

    [Test]
    public void OneArrayWriter_GetSpan()
    {
        int[] arr = (0, 0, 0, 0, 0, 0).ToArray();

        var writer = OneArrayWriter.Create(arr);

        var mem1 = writer.GetSpan(1);
        new int[] { 100 }.AsSpan().CopyTo(mem1);

        Assert.AreEqual(100, arr[0]);

        writer.Advance(1);
        var mem2 = writer.GetSpan(2);
        new int[] { 200, 300 }.CopyTo(mem2);

        Assert.AreEqual(200, arr[1]);
        Assert.AreEqual(300, arr[2]);

        writer.Advance(2);
        var mem3 = writer.GetSpan(3);
        new int[] { 400, 500, 600 }.CopyTo(mem3);

        Assert.AreEqual(400, arr[3]);
        Assert.AreEqual(500, arr[4]);
        Assert.AreEqual(600, arr[5]);

        writer.Advance(3);

        Assert.Throws<IndexOutOfRangeException>(() => writer.Advance(1));
    }

    [Test]
    public void ArrayPoolWriter_GetSpan()
    {
        var writer = new ArrayPoolWriter<int>();

        for (int i = 0; i < 1000; i++)
        {
            var mem1 = writer.GetSpan(1);
            new int[] { 100 }.AsSpan().CopyTo(mem1);

            writer.Advance(1);
            var mem2 = writer.GetSpan(2);
            new int[] { 200, 300 }.CopyTo(mem2);

            writer.Advance(2);
            var mem3 = writer.GetSpan(3);
            new int[] { 400, 500, 600 }.CopyTo(mem3);

            writer.Advance(3);

            using var arr = writer.ExtractResults()
                ?? throw new Exception();

            Assert.AreEqual(6, arr.Length);

            Assert.AreEqual(100, arr[0]);
            Assert.AreEqual(200, arr[1]);
            Assert.AreEqual(300, arr[2]);
            Assert.AreEqual(400, arr[3]);
            Assert.AreEqual(500, arr[4]);
            Assert.AreEqual(600, arr[5]);

            writer.Advance(3);
        }
    }
}