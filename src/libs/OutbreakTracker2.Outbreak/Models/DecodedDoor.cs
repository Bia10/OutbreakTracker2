using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedDoor
{
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; set; }

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