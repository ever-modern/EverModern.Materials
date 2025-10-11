namespace DestallMaterials.WheelProtection.Extensions.Compression;

public static class ByteArrayExtensions
{
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
