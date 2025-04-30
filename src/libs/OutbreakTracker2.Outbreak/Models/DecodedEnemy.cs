using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedEnemy
{
    [JsonInclude]
    [JsonPropertyName(nameof(Enabled))]
    public bool Enabled;

    [JsonInclude]
    [JsonPropertyName(nameof(InGame))]
    public bool InGame;

    [JsonInclude]
    [JsonPropertyName(nameof(SlotId))]
    public short SlotId;

    [JsonInclude]
    [JsonPropertyName(nameof(Flag))]
    public byte Flag;

    [JsonInclude]
    [JsonPropertyName(nameof(CurHp))]
    public short CurHp;

    [JsonInclude]
    [JsonPropertyName(nameof(MaxHp))]
    public short MaxHp;

    [JsonInclude]
    [JsonPropertyName(nameof(BossType))]
    public byte BossType;

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public short NameId;

    [JsonInclude]
    [JsonPropertyName(nameof(Name))]
    public string Name = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(RoomId))]
    public byte RoomId;

    [JsonInclude]
    [JsonPropertyName(nameof(RoomName))]
    public string RoomName = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(TypeId))]
    public byte TypeId;

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public byte Status;
}
