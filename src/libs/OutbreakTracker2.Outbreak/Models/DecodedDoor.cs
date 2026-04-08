using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedDoor : IHasId
{
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Hp))]
    public ushort Hp { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Flag))]
    public ushort Flag { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; init; } = string.Empty;
}
