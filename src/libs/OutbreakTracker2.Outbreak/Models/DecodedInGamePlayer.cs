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

    /// <summary>
    /// Inventory snapshot with value semantics so diffing is based on slot contents, not buffer identity.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName(nameof(Inventory))]
    public InventorySnapshot Inventory { get; init; } = InventorySnapshot.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialItem))]
    public byte SpecialItem { get; init; }

    /// <summary>
    /// Special-inventory snapshot with value semantics.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName(nameof(SpecialInventory))]
    public InventorySnapshot SpecialInventory { get; init; } = InventorySnapshot.Empty;

    /// <summary>
    /// Downed-inventory snapshot with value semantics.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName(nameof(DeadInventory))]
    public InventorySnapshot DeadInventory { get; init; } = InventorySnapshot.Empty;

    /// <summary>
    /// Downed special-inventory snapshot with value semantics.
    /// </summary>
    [JsonInclude]
    [JsonPropertyName(nameof(SpecialDeadInventory))]
    public InventorySnapshot SpecialDeadInventory { get; init; } = InventorySnapshot.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(EquippedItem))]
    public byte EquippedItem { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(SlotIndex))]
    public int SlotIndex { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(LoadingStatus))]
    public byte LoadingStatus { get; init; }
}
