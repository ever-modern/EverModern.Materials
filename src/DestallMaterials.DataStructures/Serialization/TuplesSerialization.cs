using System.Text.Json;

namespace DestallMaterials.WheelProtection.DataStructures.Serialization;

public static class TuplesSerialization
{
    static readonly JsonSerializerOptions _serializerOptions = new()
    {
        AllowTrailingCommas = true,
    };

    public static void SerializeTuple<T1, T2>((T1, T2) items, Stream stream)
    {
        stream.WriteByte((byte)'[');
        JsonSerializer.Serialize(stream, items.Item1, _serializerOptions);
        stream.WriteByte((byte)',');
        JsonSerializer.Serialize(stream, items.Item2, _serializerOptions);
        stream.WriteByte((byte)']');
    }


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
