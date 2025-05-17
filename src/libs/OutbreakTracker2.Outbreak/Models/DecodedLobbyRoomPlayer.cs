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
    [JsonPropertyName(nameof(NpcType))]
    public string NpcType { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterName))]
    public string CharacterName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterHp))]
    public string CharacterHp { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterPower))]
    public string CharacterPower { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NpcName))]
    public string NpcName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Npchp))]
    public string Npchp { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NpcPower))]
    public string NpcPower { get; set; } = string.Empty;
}