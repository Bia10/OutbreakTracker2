using OutbreakTracker2.MemoryWatcherIntegration;
using OutbreakTracker2.Outbreak.Common;

namespace OutbreakTracker2.UnitTests;

public sealed class MemoryWatcherRegionCatalogTests
{
    private const int ObservedPickupBytes = (int)FileOnePtrs.PickupOffset + sizeof(short);
    private const int ObservedPlayerBytesFileOne = (int)FileOnePtrs.InventoryOffset + 9;
    private const int ObservedPlayerBytesFileTwo = (int)FileTwoPtrs.InventoryOffset + 9;
    private const int ObservedLobbyRoomPlayerBytesFileOne = (int)FileOnePtrs.LobbyRoomPlayerNpcTypeOffset + 1;
    private const int ObservedLobbyRoomPlayerBytesFileTwo = (int)FileTwoPtrs.LobbyRoomPlayerEnabledOffset + 1;
    private const int ObservedEnemyListEntryBytes = 0x46;
    private const int FileOneDoorCount = GameConstants.MaxDoors - 9;

    [Test]
    public async Task CreateRegions_UsesObservedSpanMathForHighTrafficTables()
    {
        Dictionary<string, OutbreakTrackerMemoryRegionDefinition> regions = CreateRegionMap();

        await AssertRegionAsync(regions, "ScenarioDiscSignatureFileOne", FileOnePtrs.DiscStart, 1);
        await AssertRegionAsync(regions, "ScenarioDiscSignatureFileTwo", FileTwoPtrs.DiscStart, 1);
        await AssertRegionAsync(
            regions,
            "ScenarioRandomsFileOne",
            FileOnePtrs.ItemRandom,
            checked((nuint)(FileOnePtrs.InGamePlayerNumber - FileOnePtrs.ItemRandom + 1))
        );
        await AssertRegionAsync(
            regions,
            "ScenarioRandomsFileTwo",
            FileTwoPtrs.ItemRandom,
            checked((nuint)(FileTwoPtrs.IngamePlayerNumber - FileTwoPtrs.ItemRandom + 1))
        );
        await AssertRegionAsync(
            regions,
            "ScenarioPickupTableFileOne",
            FileOnePtrs.PickupSpaceStart,
            ObservedArrayLength(FileOnePtrs.PickupStructSize, GameConstants.MaxItems - 1, ObservedPickupBytes)
        );
        await AssertRegionAsync(
            regions,
            "ScenarioPickupTableFileTwo",
            FileTwoPtrs.PickupSpaceStart,
            ObservedArrayLength(FileTwoPtrs.PickupStructSize, GameConstants.MaxItems - 1, ObservedPickupBytes)
        );
        await AssertRegionAsync(
            regions,
            "PlayerStructObservedSpansFileOne",
            FileOnePtrs.GetPlayerStartAddress(0),
            ObservedArrayLength(
                (int)(FileOnePtrs.GetPlayerStartAddress(1) - FileOnePtrs.GetPlayerStartAddress(0)),
                GameConstants.MaxPlayers,
                ObservedPlayerBytesFileOne
            )
        );
        await AssertRegionAsync(
            regions,
            "PlayerStructObservedSpansFileTwo",
            FileTwoPtrs.GetPlayerStartAddress(0),
            ObservedArrayLength(
                (int)(FileTwoPtrs.GetPlayerStartAddress(1) - FileTwoPtrs.GetPlayerStartAddress(0)),
                GameConstants.MaxPlayers,
                ObservedPlayerBytesFileTwo
            )
        );
        await AssertRegionAsync(
            regions,
            "EnemyListFileOne",
            FileOnePtrs.EnemyListOffset,
            ObservedArrayLength(FileOnePtrs.EnemyListEntrySize, GameConstants.MaxEnemies2, ObservedEnemyListEntryBytes)
        );
        await AssertRegionAsync(
            regions,
            "EnemyListFileTwo",
            FileTwoPtrs.EnemyListOffset,
            ObservedArrayLength(FileTwoPtrs.EnemyListEntrySize, GameConstants.MaxEnemies2, ObservedEnemyListEntryBytes)
        );
        await AssertRegionAsync(
            regions,
            "LobbyRoomPlayersObservedSpansFileOne",
            FileOnePtrs.GetLobbyRoomPlayerAddress(0),
            ObservedArrayLength(
                FileOnePtrs.LobbyRoomPlayerStructSize,
                GameConstants.MaxPlayers,
                ObservedLobbyRoomPlayerBytesFileOne
            )
        );
        await AssertRegionAsync(
            regions,
            "LobbyRoomPlayersObservedSpansFileTwo",
            FileTwoPtrs.GetLobbyRoomPlayerAddress(0),
            ObservedArrayLength(
                FileTwoPtrs.LobbyRoomPlayerStructSize,
                GameConstants.MaxPlayers,
                ObservedLobbyRoomPlayerBytesFileTwo
            )
        );
        await AssertRegionAsync(
            regions,
            "LobbyRoomPriorityFileTwo",
            FileTwoPtrs.RoomPriority,
            checked((nuint)(FileTwoPtrs.RoomPriorityEntrySize * FileTwoPtrs.RoomPriorityCount))
        );
    }

    [Test]
    public async Task CreateRegions_CoversCriticalReaderAddresses()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
            new OutbreakTrackerMemoryRegionCatalog().CreateRegions(nint.Zero);

        await AssertCoveredAsync(
            regions,
            FileOnePtrs.GetPlayerStartAddress(0) + FileOnePtrs.InventoryOffset + 8,
            1,
            OutbreakTrackerMemoryDomains.InGamePlayers
        );
        await AssertCoveredAsync(
            regions,
            FileTwoPtrs.GetPlayerStartAddress(0) + FileTwoPtrs.InventoryOffset + 8,
            1,
            OutbreakTrackerMemoryDomains.InGamePlayers
        );
        await AssertCoveredAsync(
            regions,
            FileOnePtrs.EnemyListOffset + ((GameConstants.MaxEnemies2 - 1) * FileOnePtrs.EnemyListEntrySize) + 0x45,
            1,
            OutbreakTrackerMemoryDomains.Enemies
        );
        await AssertCoveredAsync(
            regions,
            FileTwoPtrs.EnemyListOffset + ((GameConstants.MaxEnemies2 - 1) * FileTwoPtrs.EnemyListEntrySize) + 0x45,
            1,
            OutbreakTrackerMemoryDomains.Enemies
        );
        await AssertCoveredAsync(
            regions,
            FileOnePtrs.DeadInventoryStart + ((GameConstants.MaxPlayers * 8) - 1),
            1,
            OutbreakTrackerMemoryDomains.InGamePlayers
        );
        await AssertCoveredAsync(
            regions,
            FileTwoPtrs.VirusMaxStart + ((GameConstants.MaxCharacterData - 1) * sizeof(int)),
            sizeof(int),
            OutbreakTrackerMemoryDomains.InGamePlayers
        );
        await AssertCoveredAsync(regions, FileTwoPtrs.WTGateHp, sizeof(ushort), OutbreakTrackerMemoryDomains.Scenario);
        await AssertCoveredAsync(
            regions,
            FileOnePtrs.GetDoorHealthAddress(FileOneDoorCount - 1),
            sizeof(ushort),
            OutbreakTrackerMemoryDomains.Doors
        );
        await AssertCoveredAsync(
            regions,
            FileTwoPtrs.GetDoorFlagAddress(GameConstants.MaxDoors - 1),
            sizeof(ushort),
            OutbreakTrackerMemoryDomains.Doors
        );
        await AssertCoveredAsync(
            regions,
            FileOnePtrs.BaseLobbySlot
                + ((GameConstants.MaxLobbySlots - 1) * LobbySlotStructOffsets.StructSize)
                + LobbySlotStructOffsets.Title,
            1,
            OutbreakTrackerMemoryDomains.LobbySlots
        );
        await AssertCoveredAsync(
            regions,
            FileTwoPtrs.GetLobbyRoomPlayerAddress(GameConstants.MaxPlayers - 1)
                + FileTwoPtrs.LobbyRoomPlayerEnabledOffset,
            1,
            OutbreakTrackerMemoryDomains.LobbyRoomPlayers
        );
        await AssertCoveredAsync(
            regions,
            FileTwoPtrs.RoomPriority + ((FileTwoPtrs.RoomPriorityCount - 1) * FileTwoPtrs.RoomPriorityEntrySize),
            1,
            OutbreakTrackerMemoryDomains.LobbyRoom
        );
    }

    [Test]
    public async Task CreateRegions_KeepsWatchedBytesWithinTightBudget()
    {
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions =
            new OutbreakTrackerMemoryRegionCatalog().CreateRegions(nint.Zero);
        ulong totalBytes = regions.Aggregate(0UL, static (sum, region) => sum + region.ByteLength);

        await Assert.That(totalBytes).IsLessThan(120_000UL);
        await Assert.That(regions.Any(static region => region.Name == "ScenarioDispatchAndRandoms")).IsFalse();
        await Assert.That(regions.Any(static region => region.Name == "LobbyRoomConfig")).IsFalse();
        await Assert.That(regions.Any(static region => region.Name == "LobbyRoomLiveState")).IsFalse();
    }

    private static Dictionary<string, OutbreakTrackerMemoryRegionDefinition> CreateRegionMap() =>
        new OutbreakTrackerMemoryRegionCatalog().CreateRegions(nint.Zero).ToDictionary(static region => region.Name);

    private static nuint ObservedArrayLength(int strideBytes, int elementCount, int observedBytesPerElement) =>
        checked((nuint)(((long)(elementCount - 1) * strideBytes) + observedBytesPerElement));

    private static async Task AssertRegionAsync(
        IReadOnlyDictionary<string, OutbreakTrackerMemoryRegionDefinition> regions,
        string name,
        nint expectedBaseAddress,
        nuint expectedByteLength
    )
    {
        await Assert.That(regions.ContainsKey(name)).IsTrue();
        OutbreakTrackerMemoryRegionDefinition region = regions[name];
        await Assert.That(region.BaseAddress).IsEqualTo(expectedBaseAddress);
        await Assert.That(region.ByteLength).IsEqualTo(expectedByteLength);
    }

    private static async Task AssertCoveredAsync(
        IReadOnlyList<OutbreakTrackerMemoryRegionDefinition> regions,
        nint address,
        int byteLength,
        OutbreakTrackerMemoryDomains expectedDomain
    )
    {
        bool covered = regions.Any(region =>
            (region.Domains & expectedDomain) != OutbreakTrackerMemoryDomains.None
            && Contains(region, address, byteLength)
        );

        await Assert.That(covered).IsTrue();
    }

    private static bool Contains(OutbreakTrackerMemoryRegionDefinition region, nint address, int byteLength)
    {
        long regionStart = region.BaseAddress;
        long regionEndExclusive = regionStart + (long)region.ByteLength;
        long rangeStart = address;
        long rangeEndExclusive = rangeStart + byteLength;

        return rangeStart >= regionStart && rangeEndExclusive <= regionEndExclusive;
    }
}
