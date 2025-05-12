using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedEnemy
{
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public string Id { get; set; } = Guid.CreateVersion7().ToString();

    [JsonInclude]
    [JsonPropertyName(nameof(Enabled))]
    public byte Enabled;

    [JsonInclude]
    [JsonPropertyName(nameof(InGame))]
    public byte InGame;

    [JsonInclude]
    [JsonPropertyName(nameof(SlotId))]
    public short SlotId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomId))]
    public byte RoomId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(TypeId))]
    public byte TypeId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public byte NameId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CurHp))]
    public short CurHp { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxHp))]
    public short MaxHp { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(BossType))]
    public byte BossType { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public byte Status { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomName))]
    public string RoomName { get; set; } = string.Empty;
}
