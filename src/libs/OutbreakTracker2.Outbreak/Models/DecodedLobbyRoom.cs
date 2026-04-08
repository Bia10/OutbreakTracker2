using System.Text.Json.Serialization;
using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbyRoom
{
    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(TimeLeft))]
    public string TimeLeft { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(MaxPlayer))]
    public short MaxPlayer { get; init; } = GameConstants.MaxPlayers;

    [JsonInclude]
    [JsonPropertyName(nameof(CurPlayer))]
    public short CurPlayer { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Difficulty))]
    public string Difficulty { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(ScenarioName))]
    public string ScenarioName { get; init; } = string.Empty;
}
