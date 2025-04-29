namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedInGamePlayer
{
    public bool Enabled;

    public bool InGame;

    public string CharacterType = string.Empty;

    public byte NameId;

    public string CharacterName = string.Empty;

    public short CurrentHealth;

    public short MaximumHealth;

    public double HealthPercentage;

    public string Condition = string.Empty;

    public ushort BleedTime;

    public ushort AntiVirusTime;

    public ushort AntiVirusGTime;

    public ushort HerbTime;

    public int CurVirus;

    public int MaxVirus;

    public double VirusPercentage;

    public float CritBonus;

    public float Size;

    public float Power;

    public float Speed;

    public float PositionX;

    public float PositionY;

    public short RoomId;

    public string RoomName = string.Empty;

    public string Status = string.Empty;
    
    public byte[] Inventory = new byte[4];
    
    public byte SpecialItem;

    // TODO: this is actually 4 bytes (each byte one item)
    public byte SpecialInventory;

    // TODO: this is actually 4 bytes (each byte one item)
    public byte DeadInventory;

    // TODO: this is actually 4 bytes (each byte one item)
    public byte SpecialDeadInventory;

    public byte CindyBag;

    public byte EquippedItem;
}
