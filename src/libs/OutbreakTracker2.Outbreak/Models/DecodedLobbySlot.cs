using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbySlot
{
    public short SlotNumber { get; set; } = 0x0000;

    public string Status { get; set; } = string.Empty;

    public string IsPassProtected { get; set; } = string.Empty;

    public short CurPlayers { get; set; } = 0x0000;

    public short MaxPlayers { get; set; } = Constants.MaxPlayers;

    public string ScenarioId { get; set; } = string.Empty;

    public string Version { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
}
