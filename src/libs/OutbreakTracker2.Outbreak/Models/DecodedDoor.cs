using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public readonly record struct DecodedDoor : IHasId
{
    public DecodedDoor()
    {
        Id = default;
        SlotId = default;
        Hp = default;
        Flag = default;
        Status = string.Empty;
    }

    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(SlotId))]
    public int SlotId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Hp))]
    public ushort Hp { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Flag))]
    public ushort Flag { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; init; }
}
