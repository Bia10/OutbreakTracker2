namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbyRoomPlayer
{
    public bool IsEnabled { get; set; }

    public byte NameId { get; set; }

    public string NPCType { get; set; } = string.Empty;

    public string CharacterName { get; set; } = string.Empty;

    public string CharacterHP { get; set; } = string.Empty;

    public string CharacterPower { get; set; } = string.Empty;

    public string NPCName { get; set; } = string.Empty;

    public string NPCHP { get; set; } = string.Empty;

    public string NPCPower { get; set; } = string.Empty;
}
