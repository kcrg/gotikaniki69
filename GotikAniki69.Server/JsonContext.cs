using System.Text.Json.Serialization;
using GotikAniki69.Server.Models;

namespace GotikAniki69.Server;

[JsonSerializable(typeof(Coordinates))]
[JsonSerializable(typeof(Response))]
[JsonSerializable(typeof(Payload))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class JsonContext : JsonSerializerContext;