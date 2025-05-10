using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedDoor
{
    // Generate a unique Id ist not possible to create one from properties since they duplicate
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public string Id { get; set; } = Guid.CreateVersion7().ToString();

    [JsonInclude]
    [JsonPropertyName(nameof(Hp))]
    public ushort Hp { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Flag))]
    public ushort Flag { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; set; } = string.Empty;
}