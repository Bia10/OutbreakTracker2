namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedEnemy
{
    public bool Enabled;

    public bool InGame;

    public short SlotId;

    public byte Flag;

    public short CurHp;

    public short MaxHp;

    public byte BossType;

    public short NameId;

    public string Name = string.Empty;

    public byte RoomId;

    public string RoomName = string.Empty;

    public byte TypeId;

    public byte Status;
}
