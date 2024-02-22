using System.Text.Json.Serialization;
using GotikAniki69.Server.Models;

namespace GotikAniki69.Server;

[JsonSerializable(typeof(CoordinatesModel))]
[JsonSerializable(typeof(ResponseModel))]
[JsonSerializable(typeof(PayloadModel))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class JsonContext : JsonSerializerContext;