namespace OutbreakTracker2.Outbreak.Models;

internal class DecodedLobbyRoomPlayer
{
    public bool IsEnabled { get; set; } = false;

    public byte NameId { get; set; } = 0x00;

    public string NPCType { get; set; } = string.Empty;

    public string CharacterName { get; set; } = string.Empty;

    public string CharacterHP { get; set; } = string.Empty;

    public string CharacterPower { get; set; } = string.Empty;

    public string NPCName { get; set; } = string.Empty;

    public string NPCHP { get; set; } = string.Empty;

    public string NPCPower { get; set; } = string.Empty;
}
