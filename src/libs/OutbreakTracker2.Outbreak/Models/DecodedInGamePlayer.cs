using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedInGamePlayer : IHasId
{
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(IsEnabled))]
    public bool IsEnabled { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(IsInGame))]
    public bool IsInGame { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Type))]
    public string Type { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public byte NameId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Name))]
    public string Name { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CurHealth))]
    public short CurHealth { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxHealth))]
    public short MaxHealth { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(HealthPercentage))]
    public double HealthPercentage { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Condition))]
    public string Condition { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(BleedTime))]
    public ushort BleedTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(AntiVirusTime))]
    public ushort AntiVirusTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(AntiVirusGTime))]
    public ushort AntiVirusGTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(HerbTime))]
    public ushort HerbTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(CurVirus))]
    public int CurVirus { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxVirus))]
    public int MaxVirus { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(VirusPercentage))]
    public double VirusPercentage { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(CritBonus))]
    public float CritBonus { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Size))]
    public float Size { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Power))]
    public float Power { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Speed))]
    public float Speed { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PositionX))]
    public float PositionX { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PositionY))]
    public float PositionY { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomId))]
    public short RoomId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomName))]
    public string RoomName { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Inventory))]
    public byte[] Inventory { get; init; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialItem))]
    public byte SpecialItem { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialInventory))]
    public byte[] SpecialInventory { get; init; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(DeadInventory))]
    public byte[] DeadInventory { get; init; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialDeadInventory))]
    public byte[] SpecialDeadInventory { get; init; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(EquippedItem))]
    public byte EquippedItem { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(SlotIndex))]
    public int SlotIndex { get; init; }
}
