using System.Buffers;

namespace EverModern.WheelProtection.DataStructures.Buffers;

/// <summary>
/// Borrows arrays from a pool with a configurable size discriminator.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public readonly struct ArrayBorrower<T>
{
    /// <summary>
    /// Initializes a new borrower.
    /// </summary>
    public ArrayBorrower()
    {
    }

    /// <summary>
    /// Gets the size discriminator used to round rented array sizes.
    /// </summary>
    public required int Discriminator { get; init; }

    /// <summary>
    /// Gets the array pool source.
    /// </summary>
    public ArrayPool<T> Source { get; init; } = ArrayPool<T>.Shared;

    /// <summary>
    /// Borrows an array with the specified size.
    /// </summary>
    /// <param name="size">The requested size.</param>
    public BorrowedArray<T> Borrow(int size)
        => new(Source.Rent(ComputeRentedArraySize(size, Discriminator)), size);

    /// <summary>
    /// Computes the actual rented array size based on the discriminator.
    /// </summary>
    /// <param name="size">The requested size.</param>
    /// <param name="discriminator">The size discriminator.</param>
    public static int ComputeRentedArraySize(int size, int discriminator) =>
        (size + discriminator - 1) / discriminator * discriminator;
}
