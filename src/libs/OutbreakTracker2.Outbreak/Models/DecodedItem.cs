using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedItem
{
    [JsonInclude]
    [JsonPropertyName(nameof(Number))]
    public byte Number { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public short Id { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(En))]
    public short En { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Type))]
    public short Type { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Count))]
    public short Count { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pick))]
    public short Pick { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Present))]
    public int Present { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Mix))]
    public byte Mix { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomID))]
    public byte RoomID { get; set; }
}