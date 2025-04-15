using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbyRoom
{
    public byte Status { get; set; } = 0x00;

    public string TimeLeft { get; set; } = string.Empty;

    public short MaxPlayer { get; set; } = Constants.MaxPlayers;

    public short CurPlayer { get; set; } = 0x0000;

    public string Difficulty { get; set; } = string.Empty;

    public string ScenarioName { get; set; } = string.Empty;
}