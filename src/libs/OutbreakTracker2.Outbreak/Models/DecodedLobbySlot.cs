using System.Text.Json.Serialization;
using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbySlot
{
    [JsonInclude]
    [JsonPropertyName(nameof(SlotNumber))]
    public short SlotNumber { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(IsPassProtected))]
    public string IsPassProtected { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CurPlayers))]
    public short CurPlayers { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(MaxPlayers))]
    public short MaxPlayers { get; set; } = Constants.MaxPlayers;

    [JsonInclude]
    [JsonPropertyName(nameof(ScenarioId))]
    public string ScenarioId { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Version))]
    public string Version { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Title))]
    public string Title { get; set; } = string.Empty;
}
