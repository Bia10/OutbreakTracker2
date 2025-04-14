namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbyRoom
{
    public byte Status { get; set; }

    public string TimeLeft { get; set; } = string.Empty;

    public short MaxPlayer { get; set; } = Constants.MaxPlayers;

    public short CurPlayer { get; set; }

    public string Difficulty { get; set; } = string.Empty;

    public string ScenarioName { get; set; } = string.Empty;
}