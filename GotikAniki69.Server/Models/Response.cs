using System.Text.Json.Serialization;

namespace GotikAniki69.Server.Models;

public class Response
{
    [JsonPropertyName("type")]
    public required string Type
    {
        get; set;
    }

    [JsonPropertyName("payload")]
    public required Payload Payload
    {
        get; set;
    }
}
public class Payload
{
    [JsonPropertyName("x")]
    public double X
    {
        get; set;
    }

    [JsonPropertyName("y")]
    public double Y
    {
        get; set;
    }

    [JsonPropertyName("index")]
    public int Index
    {
        get; set;
    }
}