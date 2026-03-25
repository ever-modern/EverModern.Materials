namespace EverModern.WheelProtection.Extensions.Enumerables;

/// <summary>
/// Provides compression helpers for byte arrays.
/// </summary>
public static class ByteArrayExtensions
{
    /// <summary>
    /// Compresses a byte array using a stream compressor asynchronously.
    /// </summary>
    /// <param name="byteArray">The input array.</param>
    /// <param name="compressor">The compressor factory.</param>
    public static async Task<byte[]> CompressWithAsync(
        this byte[] byteArray,
        Func<Stream, Stream> compressor
    )
    {
        using var inputStream = new MemoryStream(byteArray);
        using var outputStream = new MemoryStream();

        using var compressionStream = compressor(outputStream);

        await inputStream.CopyToAsync(compressionStream);
        await compressionStream.FlushAsync();

        var result = outputStream.ToArray();

        return result;
    }

    /// <summary>
    /// Compresses a byte array using a stream compressor.
    /// </summary>
    /// <param name="byteArray">The input array.</param>
    /// <param name="compressor">The compressor factory.</param>
    public static byte[] CompressWith(this byte[] byteArray, Func<Stream, Stream> compressor)
    {
        using var inputStream = new MemoryStream(byteArray);
        using var outputStream = new MemoryStream();

        using var compressionStream = compressor(outputStream);

        inputStream.CopyTo(compressionStream);
        compressionStream.Flush();

        var result = outputStream.ToArray();

        return result;
    }

    /// <summary>
    /// Decompresses a byte array using a stream decompressor asynchronously.
    /// </summary>
    /// <param name="byteArray">The input array.</param>
    /// <param name="decompressor">The decompressor factory.</param>
    public static async Task<byte[]> DecompressWithAsync(
        this byte[] byteArray,
        Func<Stream, Stream> decompressor
    )
    {
        using var inputStream = new MemoryStream(byteArray);
        using var decompressionStream = decompressor(inputStream);

        using var outputStream = new MemoryStream();

        await decompressionStream.CopyToAsync(outputStream);

        outputStream.Position = 0;

        var result = outputStream.ToArray();

        return result;
    }

    /// <summary>
    /// Decompresses a byte array using a stream decompressor.
    /// </summary>
    /// <param name="byteArray">The input array.</param>
    /// <param name="decompressor">The decompressor factory.</param>
    public static byte[] DecompressWith(this byte[] byteArray, Func<Stream, Stream> decompressor)
    {
        using var inputStream = new MemoryStream(byteArray);
        using var decompressionStream = decompressor(inputStream);

        using var outputStream = new MemoryStream();

        decompressionStream.CopyTo(outputStream);

        outputStream.Position = 0;

        var result = outputStream.ToArray();

        return result;
    }
}
