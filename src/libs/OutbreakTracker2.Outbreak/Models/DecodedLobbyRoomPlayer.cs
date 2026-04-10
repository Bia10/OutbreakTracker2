using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedLobbyRoomPlayer : IHasId
{
    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(IsEnabled))]
    public bool IsEnabled { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(NameId))]
    public byte NameId { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(NpcType))]
    public string NpcType { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterName))]
    public string CharacterName { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterHp))]
    public string CharacterHp { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(CharacterPower))]
    public string CharacterPower { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NpcName))]
    public string NpcName { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NpcHp))]
    public string NpcHp { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(NpcPower))]
    public string NpcPower { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(SlotIndex))]
    public byte SlotIndex { get; init; } = byte.MaxValue;
}
