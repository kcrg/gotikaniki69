using System.Text.Json.Serialization;
using GotikAniki69.Server.Models;

namespace GotikAniki69.Server.Serialization;

[JsonSerializable(typeof(CoordinatesModel))]
[JsonSerializable(typeof(ResponseModel))]
[JsonSerializable(typeof(PayloadModel))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class JsonContext : JsonSerializerContext;