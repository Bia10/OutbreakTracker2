using System.Text.Json.Serialization;
using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedInGameScenario
{
    [JsonInclude]
    [JsonPropertyName(nameof(CurrentFile))]
    public byte CurrentFile { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(ScenarioName))]
    public string ScenarioName { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(FrameCounter))]
    public int FrameCounter { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Status))]
    public byte Status { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(WildThingsTime))]
    public short WildThingsTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(EscapeTime))]
    public short EscapeTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(FightTime))]
    public int FightTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(FightTime2))]
    public short FightTime2 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(GarageTime))]
    public int GarageTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(GasTime))]
    public int GasTime { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(GasFlag))]
    public int GasFlag { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(GasRandom))]
    public byte GasRandom { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(ItemRandom))]
    public byte ItemRandom { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(ItemRandom2))]
    public byte ItemRandom2 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PuzzleRandom))]
    public byte PuzzleRandom { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Coin))]
    public byte Coin { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(KilledZombie))]
    public byte KilledZombie { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PlayerCount))]
    public byte PlayerCount { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassDesperateTimes1))]
    public short PassDesperateTimes1 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassWildThings))]
    public byte PassWildThings { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassDesperateTimes2))]
    public byte PassDesperateTimes2 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassDesperateTimes3))]
    public byte PassDesperateTimes3 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass1))]
    public byte Pass1 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass2))]
    public byte Pass2 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass3))]
    public byte Pass3 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassUnderbelly1))]
    public short PassUnderbelly1 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassUnderbelly2))]
    public byte PassUnderbelly2 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassUnderbelly3))]
    public byte PassUnderbelly3 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass4))]
    public short Pass4 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass5))]
    public byte Pass5 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass6))]
    public byte Pass6 { get; init; }

    [JsonInclude]
    [JsonPropertyName(nameof(Difficulty))]
    public string Difficulty { get; init; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Items))]
    public DecodedItem[] Items { get; init; } = new DecodedItem[GameConstants.MaxItems];
}
