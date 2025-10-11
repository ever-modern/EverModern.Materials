using System.Buffers;

namespace DestallMaterials.WheelProtection.DataStructures.Buffers;

public readonly struct ArrayBorrower<T>
{
    public ArrayBorrower()
    {
    }

    public required int Discriminator { get; init; }

    public ArrayPool<T> Source { get; init; } = ArrayPool<T>.Shared;

    public BorrowedArray<T> Borrow(int size)
        => new(Source.Rent(ComputeRentedArraySize(size, Discriminator)), size);

    public static int ComputeRentedArraySize(int size, int discriminator) =>
        (size + discriminator - 1) / discriminator * discriminator;
}
