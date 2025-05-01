using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedInGamePlayer
{
    [JsonInclude]
    [JsonPropertyName(nameof(Enabled))]
    public bool Enabled { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(InGame))]
    public bool InGame { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterType))]
    public string CharacterType { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public byte NameId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterName))]
    public string CharacterName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CurrentHealth))]
    public short CurrentHealth { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaximumHealth))]
    public short MaximumHealth { get; set; }

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
    [JsonPropertyName(nameof(InventoryNamed))]
    public string[] InventoryNamed { get; set; }  =
    [
        "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|"
    ];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialItem))]
    public byte SpecialItem { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialItemNamed))]
    public string SpecialItemNamed { get; set; } = "Empty|[0x00](0)|";

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialInventory))]
    public byte[] SpecialInventory { get; set; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialInventoryNamed))]
    public string[] SpecialInventoryNamed { get; set; }  =
    [
        "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|"
    ];

    [JsonInclude]
    [JsonPropertyName(nameof(DeadInventory))]
    public byte[] DeadInventory { get; set; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(DeadInventoryNamed))]
    public string[] DeadInventoryNamed { get; set; }  =
    [
        "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|"
    ];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialDeadInventory))]
    public byte[] SpecialDeadInventory { get; set; } = new byte[4];

    [JsonInclude]
    [JsonPropertyName(nameof(SpecialDeadInventoryNamed))]
    public string[] SpecialDeadInventoryNamed { get; set; } =
    [
        "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|", "Empty|[0x00](0)|"
    ];

    [JsonInclude]
    [JsonPropertyName(nameof(EquippedItem))]
    public byte EquippedItem { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(EquippedItemNamed))]
    public string EquippedItemNamed { get; set; } = "Empty|[0x00](0)|";

}
