using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedInGamePlayer
{
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(IsEnabled))]
    public bool IsEnabled { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(IsInGame))]
    public bool IsInGame { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Type))]
    public string Type { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public byte NameId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Name))]
    public string Name { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CurHealth))]
    public short CurHealth { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxHealth))]
    public short MaxHealth { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(HealthPercentage))]
    public double HealthPercentage { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Condition))]
    public string Condition { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(BleedTime))]
    public ushort BleedTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(AntiVirusTime))]
    public ushort AntiVirusTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(AntiVirusGTime))]
    public ushort AntiVirusGTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(HerbTime))]
    public ushort HerbTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(CurVirus))]
    public int CurVirus { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxVirus))]
    public int MaxVirus { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(VirusPercentage))]
    public double VirusPercentage { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(CritBonus))]
    public float CritBonus { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Size))]
    public float Size { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Power))]
    public float Power { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Speed))]
    public float Speed { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PositionX))]
    public float PositionX { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PositionY))]
    public float PositionY { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomId))]
    public short RoomId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(RoomName))]
    public string RoomName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Inventory))]
    public byte[] Inventory { get; set; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialItem))]
    public byte SpecialItem { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialInventory))]
    public byte[] SpecialInventory { get; set; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(DeadInventory))]
    public byte[] DeadInventory { get; set; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialDeadInventory))]
    public byte[] SpecialDeadInventory { get; set; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(EquippedItem))]
    public byte EquippedItem { get; set; }
}
