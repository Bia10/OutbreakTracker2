using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedInGamePlayer : IEquatable<DecodedInGamePlayer>
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

    public bool Equals(DecodedInGamePlayer? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        if (Enabled != other.Enabled) return false;
        if (InGame != other.InGame) return false;
        if (CharacterType != other.CharacterType) return false;
        if (NameId != other.NameId) return false;
        if (CharacterName != other.CharacterName) return false;
        if (CurrentHealth != other.CurrentHealth) return false;
        if (MaximumHealth != other.MaximumHealth) return false;
        if (!HealthPercentage.Equals(other.HealthPercentage)) return false;
        if (Condition != other.Condition) return false;
        if (BleedTime != other.BleedTime) return false;
        if (AntiVirusTime != other.AntiVirusTime) return false;
        if (AntiVirusGTime != other.AntiVirusGTime) return false;
        if (HerbTime != other.HerbTime) return false;
        if (CurVirus != other.CurVirus) return false;
        if (MaxVirus != other.MaxVirus) return false;
        if (!VirusPercentage.Equals(other.VirusPercentage)) return false;
        if (!CritBonus.Equals(other.CritBonus)) return false;
        if (!Size.Equals(other.Size)) return false;
        if (!Power.Equals(other.Power)) return false;
        if (!Speed.Equals(other.Speed)) return false;
        if (!PositionX.Equals(other.PositionX)) return false;
        if (!PositionY.Equals(other.PositionY)) return false;
        if (RoomId != other.RoomId) return false;
        if (RoomName != other.RoomName) return false;
        if (Status != other.Status) return false;
        if (SpecialItem != other.SpecialItem) return false;
        if (EquippedItem != other.EquippedItem) return false;
        if (!Inventory.SequenceEqual(other.Inventory)) return false;
        if (!SpecialInventory.SequenceEqual(other.SpecialInventory)) return false;
        if (!DeadInventory.SequenceEqual(other.DeadInventory)) return false;
        if (!SpecialDeadInventory.SequenceEqual(other.SpecialDeadInventory)) return false;

        return true;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Enabled);
        hash.Add(InGame);
        hash.Add(CharacterType);
        hash.Add(NameId);
        hash.Add(CharacterName);
        hash.Add(CurrentHealth);
        hash.Add(MaximumHealth);
        hash.Add(HealthPercentage);
        hash.Add(Condition);
        hash.Add(BleedTime);
        hash.Add(AntiVirusTime);
        hash.Add(AntiVirusGTime);
        hash.Add(HerbTime);
        hash.Add(CurVirus);
        hash.Add(MaxVirus);
        hash.Add(VirusPercentage);
        hash.Add(CritBonus);
        hash.Add(Size);
        hash.Add(Power);
        hash.Add(Speed);
        hash.Add(PositionX);
        hash.Add(PositionY);
        hash.Add(RoomId);
        hash.Add(RoomName);
        hash.Add(Status);
        hash.Add((Inventory[0], Inventory[1], Inventory[2], Inventory[3]));
        hash.Add((SpecialInventory[0], SpecialInventory[1], SpecialInventory[2], SpecialInventory[3]));
        hash.Add((DeadInventory[0], DeadInventory[1], DeadInventory[2], DeadInventory[3]));
        hash.Add((SpecialDeadInventory[0], SpecialDeadInventory[1], SpecialDeadInventory[2], SpecialDeadInventory[3]));
        hash.Add(SpecialItem);
        hash.Add(EquippedItem);
        return hash.ToHashCode();
    }
}
