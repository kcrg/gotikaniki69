using System.Buffers;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace GotikAniki69.Server.Serialization;

public static class JsonBytes
{
    public static byte[] Serialize<T>(T value, JsonTypeInfo<T> typeInfo)
    {
        var buffer = new ArrayBufferWriter<byte>(256);
        using var writer = new Utf8JsonWriter(buffer);
        JsonSerializer.Serialize(writer, value, typeInfo);
        writer.Flush();
        return buffer.WrittenSpan.ToArray();
    }
}
