using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.Outbreak.Offsets;

internal static class InGameScenarioOffsets
{
    public static readonly (nint[] File1, nint[] File2) ScenarioId = ([FileOnePtrs.InGameScenarioId], [FileTwoPtrs.InGameScenarioId]);
    public static readonly (nint[] File1, nint[] File2) FrameCounter = ([FileOnePtrs.InGameFrameCounter], [FileTwoPtrs.InGameFrameCounter]);
    public static readonly (nint[] File1, nint[] File2) ScenarioStatus = ([FileOnePtrs.ScenarioStatus], [FileTwoPtrs.ScenarioStatus]);
    public static readonly (nint[] File1, nint[] File2) PlayerNumber = ([FileOnePtrs.InGamePlayerNumber], [FileTwoPtrs.IngamePlayerNumber]);
    public static readonly (nint[] File1, nint[] File2) Difficulty = ([FileOnePtrs.Difficulty], [FileTwoPtrs.Difficulty]);
    public static readonly (nint[] File1, nint[] File2) ItemRandom = ([FileOnePtrs.ItemRandom], [FileTwoPtrs.ItemRandom]);
    public static readonly (nint[] File1, nint[] File2) ItemRandom2 = ([FileOnePtrs.ItemRandom2], [FileTwoPtrs.ItemRandom2]);
    public static readonly (nint[] File1, nint[] File2) PuzzleRandom = ([FileOnePtrs.PuzzleRandom], [FileTwoPtrs.PuzzleRandom]);
    public static readonly (nint[] File1, nint[] File2) Pass4 = ([FileOnePtrs.Pass4], [FileTwoPtrs.Pass4]);

    public static readonly (nint[] File1, nint[] File2) WildThingsTime = ([], [FileTwoPtrs.WildThingsTime]);
    public static readonly (nint[] File1, nint[] File2) EscapeTime = ([], [FileTwoPtrs.EscapeTime]);
    public static readonly (nint[] File1, nint[] File2) DesperateTimesFightTime = ([], [FileTwoPtrs.DesperateTimesFightTime]);
    public static readonly (nint[] File1, nint[] File2) DesperateTimesFightTime2 = ([], [FileTwoPtrs.DesperateTimesFightTime2]);
    public static readonly (nint[] File1, nint[] File2) DesperateTimesGarageTime = ([], [FileTwoPtrs.DesperateTimesGarageTime]);
    public static readonly (nint[] File1, nint[] File2) DesperateTimesGasTime = ([], [FileTwoPtrs.DesperateTimesGasTime]);
    public static readonly (nint[] File1, nint[] File2) DesperateTimesGasFlag = ([], [FileTwoPtrs.DesperateTimesGasFlag]);
    public static readonly (nint[] File1, nint[] File2) DesperateTimesGasRandom = ([], [FileTwoPtrs.DesperateTimesGasRandom]);
    public static readonly (nint[] File1, nint[] File2) Coin = ([], [FileTwoPtrs.Coin]);
    public static readonly (nint[] File1, nint[] File2) KilledZombies = ([], [FileTwoPtrs.KilledZombie]);
    public static readonly (nint[] File1, nint[] File2) PassWildThings = ([], [FileTwoPtrs.PassWildThings]);
    public static readonly (nint[] File1, nint[] File2) PassDesperateTimes = ([], [FileTwoPtrs.PassDesperateTimes]);
    public static readonly (nint[] File1, nint[] File2) PassDesperateTimes2 = ([], [FileTwoPtrs.PassDesperateTimes2]);
    public static readonly (nint[] File1, nint[] File2) PassDesperateTimes3 = ([], [FileTwoPtrs.PassDesperateTimes3]);
    public static readonly (nint[] File1, nint[] File2) PassUnderBelly1 = ([], [FileTwoPtrs.PassUnderBelly1]);
    public static readonly (nint[] File1, nint[] File2) PassUnderBelly2 = ([], [FileTwoPtrs.PassUnderBelly2]);
    public static readonly (nint[] File1, nint[] File2) PassUnderBelly3 = ([], [FileTwoPtrs.PassUnderBelly3]);

    public static readonly (nint[] File1, nint[] File2) Pass1 = ([FileOnePtrs.Pass1], []);
    public static readonly (nint[] File1, nint[] File2) Pass2 = ([FileOnePtrs.Pass2], []);
    public static readonly (nint[] File1, nint[] File2) Pass3 = ([FileOnePtrs.Pass3], []);
    public static readonly (nint[] File1, nint[] File2) Pass5 = ([FileOnePtrs.Pass5], []);
    public static readonly (nint[] File1, nint[] File2) Pass6 = ([FileOnePtrs.Pass6], []);

    public static readonly (nint[] File1, nint[] File2) PickupSpaceStart = ([FileOnePtrs.PickupSpaceStart], [FileTwoPtrs.PickupSpaceStart]);
    public static readonly (int File1, int File2) PickupStructSize = new(FileOnePtrs.PickupStructSize, FileTwoPtrs.PickupStructSize);
    public static readonly (nint File1, nint File2) ItemRoomIdOffset = (FileOnePtrs.RoomIdOffset, FileTwoPtrs.RoomIdOffset);
    public static readonly (nint File1, nint File2) ItemNumberOffset = (FileOnePtrs.NumberOffset, FileTwoPtrs.NumberOffset);
    public static readonly (nint File1, nint File2) ItemIdOffset = (FileOnePtrs.IdOffset, FileTwoPtrs.IdOffset);
    public static readonly (nint File1, nint File2) ItemMixOffset = (FileOnePtrs.MixOffset, FileTwoPtrs.MixOffset);
    public static readonly (nint File1, nint File2) ItemPresentOffset = (FileOnePtrs.PresentOffset, FileTwoPtrs.PresentOffset);
    public static readonly (nint File1, nint File2) ItemPickupCountOffset = (FileOnePtrs.PickupCountOffset, FileTwoPtrs.PickupCountOffset);
    public static readonly (nint File1, nint File2) ItemPickupOffset = (FileOnePtrs.PickupOffset, FileTwoPtrs.PickupOffset);
}