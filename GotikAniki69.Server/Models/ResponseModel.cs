using System.Text.Json.Serialization;

namespace GotikAniki69.Server.Models;

public class ResponseModel
{
    [JsonPropertyName("type")]
    public required string Type { get; set; }

    [JsonPropertyName("payload")]
    public PayloadModel? Payload { get; set; }
}

public class PayloadModel
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("nick")]
    public string? NickName { get; set; }

    [JsonPropertyName("skinId")]
    public string? SkinId
    {
        get; set;
    }

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }
}