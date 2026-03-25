using System.Text.Json;

namespace EverModern.WheelProtection.DataStructures.Serialization;

/// <summary>
/// Provides JSON serialization helpers for tuples.
/// </summary>
public static class TuplesSerialization
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        AllowTrailingCommas = true,
    };

    /// <summary>
    /// Serializes a tuple to a stream.
    /// </summary>
    /// <typeparam name="T1">The first item type.</typeparam>
    /// <typeparam name="T2">The second item type.</typeparam>
    /// <param name="items">The tuple items.</param>
    /// <param name="stream">The destination stream.</param>
    public static void SerializeTuple<T1, T2>((T1, T2) items, Stream stream)
    {
        stream.WriteByte((byte)'[');
        JsonSerializer.Serialize(stream, items.Item1, _serializerOptions);
        stream.WriteByte((byte)',');
        JsonSerializer.Serialize(stream, items.Item2, _serializerOptions);
        stream.WriteByte((byte)']');
    }


    /// <summary>
    /// Deserializes a tuple from a stream.
    /// </summary>
    /// <typeparam name="T1">The first item type.</typeparam>
    /// <typeparam name="T2">The second item type.</typeparam>
    /// <param name="stream">The source stream.</param>
    public static (T1, T2) DeserializeTuple<T1, T2>(Stream stream)
    {
        SkipFor(stream, '[');
        
        var firstItem = JsonSerializer.Deserialize<T1>(stream, _serializerOptions);
        SkipFor(stream, ',');
        var secondItems = JsonSerializer.Deserialize<T2>(stream);
        SkipFor(stream, ']');

        return (firstItem, secondItems);
    }

    static void SkipFor(Stream stream, char symbol)
    {
        var currentByte = stream.ReadByte();
        while (currentByte != symbol)
        {
            if (currentByte != ' ' && currentByte != '\t' && currentByte != '\n' && currentByte != '\r')
            {
                throw new InvalidOperationException($"Unexpected symbol encountered {(char)currentByte}");
            }
            currentByte = stream.ReadByte();
        }
    }

}
