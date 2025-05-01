using OutbreakTracker2.Outbreak.Common;
using System.Text.Json.Serialization;

namespace OutbreakTracker2.Outbreak.Models;

public sealed record DecodedScenario
{
    [JsonInclude]
    [JsonPropertyName(nameof(CurrentFile))]
    public byte CurrentFile { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(ScenarioName))]
    public string ScenarioName { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(FrameCounter))]
    public int FrameCounter { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Cleared))]
    public byte Cleared { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(WildThingsTime))]
    public short WildThingsTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(EscapeTime))]
    public short EscapeTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(FightTime))]
    public int FightTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(FightTime2))]
    public short FightTime2 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(GarageTime))]
    public int GarageTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(GasTime))]
    public int GasTime { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(GasFlag))]
    public int GasFlag { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(GasRandom))]
    public byte GasRandom { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(ItemRandom))]
    public byte ItemRandom { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(ItemRandom2))]
    public byte ItemRandom2 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PuzzleRandom))]
    public byte PuzzleRandom { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Coin))]
    public byte Coin { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(KilledZombie))]
    public byte KilledZombie { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PlayerCount))]
    public byte PlayerCount { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassDesperateTimes1))]
    public short PassDesperateTimes1 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassWildThings))]
    public byte PassWildThings { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassDesperateTimes2))]
    public byte PassDesperateTimes2 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassDesperateTimes3))]
    public byte PassDesperateTimes3 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass1))]
    public byte Pass1 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass2))]
    public byte Pass2 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass3))]
    public byte Pass3 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassUnderbelly1))]
    public short PassUnderbelly1 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassUnderbelly2))]
    public byte PassUnderbelly2 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(PassUnderbelly3))]
    public byte PassUnderbelly3 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass4))]
    public short Pass4 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass5))]
    public byte Pass5 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Pass6))]
    public byte Pass6 { get; set; }

    [JsonInclude]
    [JsonPropertyName(nameof(Difficulty))]
    public string Difficulty { get; set; } = string.Empty;

    [JsonInclude]
    [JsonPropertyName(nameof(Items))]
    public DecodedItem[] Items { get; set; } = new DecodedItem[GameConstants.MaxItems];
}