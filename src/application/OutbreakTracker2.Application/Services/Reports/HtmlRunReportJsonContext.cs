using System.Text.Json.Serialization;

namespace OutbreakTracker2.Application.Services.Reports;

[JsonSerializable(typeof(HtmlRunReportWriter.HtmlPayload))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal sealed partial class HtmlRunReportJsonContext : JsonSerializerContext;
