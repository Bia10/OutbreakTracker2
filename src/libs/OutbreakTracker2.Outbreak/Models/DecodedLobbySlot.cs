using System.Text.Json.Serialization;
using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbySlot
{
    [JsonInclude]
    [JsonPropertyName(nameof(SlotNumber))]
    public short SlotNumber { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(IsPassProtected))]
    public bool IsPassProtected { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(CurPlayers))]
    public short CurPlayers { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxPlayers))]
    public short MaxPlayers { get; init; } = GameConstants.MaxPlayers;

    [JsonInclude]
    [JsonPropertyName(nameof(ScenarioId))]
    public string ScenarioId { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Version))]
    public string Version { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Title))]
    public string Title { get; init; } = string.Empty;
}
