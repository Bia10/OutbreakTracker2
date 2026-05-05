using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Views.Map.Canvas;

[JsonSerializable(typeof(MapProjectionCalibrationDocument))]
[JsonSerializable(typeof(Dictionary<string, MapProjectionCalibration>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
internal sealed partial class MapProjectionCalibrationJsonContext : JsonSerializerContext;
