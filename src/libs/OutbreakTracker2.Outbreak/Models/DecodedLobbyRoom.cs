using OutbreakTracker2.Outbreak.Common;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbyRoom
{
    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public byte Status { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(TimeLeft))]
    public string TimeLeft { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(MaxPlayer))]
    public short MaxPlayer { get; set; } = GameConstants.MaxPlayers;

    [JsonInclude]
    [JsonPropertyName(nameof(CurPlayer))]
    public short CurPlayer { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Difficulty))]
    public string Difficulty { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(ScenarioName))]
    public string ScenarioName { get; set; } = string.Empty;
}