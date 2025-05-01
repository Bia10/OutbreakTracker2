using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedItem
{
    [JsonInclude]
    [JsonPropertyName(nameof(SlotIndex))]
    public byte SlotIndex { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public short Id { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(En))]
    public short En { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(TypeName))]
    public string TypeName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Quantity))]
    public short Quantity { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PickedUp))]
    public short PickedUp { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Present))]
    public int Present { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Mix))]
    public byte Mix { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomId))]
    public byte RoomId { get; set; }
}