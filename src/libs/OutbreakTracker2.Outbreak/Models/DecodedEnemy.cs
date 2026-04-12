using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedEnemy : IHasId
{
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Enabled))]
    public byte Enabled { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(InGame))]
    public byte InGame { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(SlotId))]
    public short SlotId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomId))]
    public byte RoomId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(TypeId))]
    public byte TypeId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public byte NameId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Name))]
    public string Name { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CurHp))]
    public ushort CurHp { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxHp))]
    public ushort MaxHp { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(BossType))]
    public byte BossType { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public byte Status { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PositionX))]
    public float PositionX { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PositionY))]
    public float PositionY { get; init; }
}
