namespace OutbreakTracker2.Outbreak.Models;

public record DecodedScenario
{
    public byte CurrentFile;

    public string ScenarioName = string.Empty;

    public int FrameCounter;

    public byte Cleared;

    public short WildThingsTime;

    public short EscapeTime;

    public int FightTime;

    public short FightTime2;

    public int GarageTime;

    public int GasTime;

    public int GasFlag;

    public byte GasRandom;

    public byte ItemRandom;

    public byte ItemRandom2;

    public byte PuzzleRandom;

    public byte Coin;

    public byte KilledZombie;

    public byte PlayerCount;

    public short PassDesperateTimes1;

    public byte PassWildThings;

    public byte PassDesperateTimes2;

    public byte PassDesperateTimes3;

    public byte Pass1;

    public byte Pass2;

    public byte Pass3;

    public short PassUnderbelly1;

    public byte PassUnderbelly2;

    public byte PassUnderbelly3;

    public short Pass4;

    public byte Pass5;

    public byte Pass6;

    public string Difficulty = string.Empty;
}
