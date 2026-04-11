using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public readonly record struct DecodedItem
{
    public DecodedItem() { }

    [JsonInclude]
    [JsonPropertyName(nameof(SlotIndex))]
    public byte SlotIndex { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public short Id { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(En))]
    public short En { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(TypeId))]
    public short TypeId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(TypeName))]
    public string TypeName { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Quantity))]
    public short Quantity { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PickedUp))]
    public short PickedUp { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Present))]
    public int Present { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Mix))]
    public byte Mix { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomId))]
    public byte RoomId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomName))]
    public string RoomName { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(PickedUpByName))]
    public string PickedUpByName { get; init; } = string.Empty;
}
