using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbyRoomPlayer
{
    [JsonInclude]
    [JsonPropertyName(nameof(IsEnabled))]
    public bool IsEnabled { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public byte NameId { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(NPCType))]
    public string NPCType { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterName))]
    public string CharacterName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterHP))]
    public string CharacterHP { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterPower))]
    public string CharacterPower { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NPCName))]
    public string NPCName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NPCHP))]
    public string NPCHP { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NPCPower))]
    public string NPCPower { get; set; } = string.Empty;
}