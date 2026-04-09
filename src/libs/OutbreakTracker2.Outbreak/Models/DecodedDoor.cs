using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

[SuppressMessage(
    "Performance",
    "EPS01:Struct can be made readonly",
    Justification = "System.Text.Json source generation emits analyzer violations for the readonly form."
)]
public record struct DecodedDoor : IHasId
{
    public DecodedDoor()
    {
        Id = default;
        Hp = default;
        Flag = default;
        Status = string.Empty;
    }

    [JsonInclude]
    [JsonPropertyName(nameof(Id))]
    public Ulid Id { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Hp))]
    public ushort Hp { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Flag))]
    public ushort Flag { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public string Status { get; init; }
}
