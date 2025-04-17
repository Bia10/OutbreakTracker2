using System.Text.Json;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Outbreak.Readers;

public class EnemiesReader : ReaderBase
{
    public EnemiesReader(GameClient gameClient, EEmemMemory eememMemory) : base(gameClient, eememMemory)
    {
    }

    public byte GetNameId(int enemyId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetEnemyAddress(enemyId) + FileOnePtrs.EnemyNameIdOffset),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetEnemyAddress(enemyId) + FileTwoPtrs.EnemyNameIdOffset),
        _ => 0xFF
    };

    public byte GetType(int enemyId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetEnemyAddress(enemyId) + FileOnePtrs.EnemyTypeOffset),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetEnemyAddress(enemyId) + FileTwoPtrs.EnemyTypeOffset),
        _ => 0xFF
    };

    public byte GetEnabled(int enemyId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetEnemyAddress(enemyId) + FileOnePtrs.EnemyEnabled),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetEnemyAddress(enemyId) + FileTwoPtrs.EnemyEnabled),
        _ => 0xFF
    };

    public byte GetInGame(int enemyId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<byte>(FileOnePtrs.GetEnemyAddress(enemyId) + FileOnePtrs.EnemyInGame),
        GameFile.FileTwo => ReadValue<byte>(FileTwoPtrs.GetEnemyAddress(enemyId) + FileTwoPtrs.EnemyInGame),
        _ => 0xFF
    };

    public short GetCurHealth(int enemyId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetEnemyAddress(enemyId) + FileOnePtrs.EnemyHpOffset),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetEnemyAddress(enemyId) + FileTwoPtrs.EnemyHpOffset),
        _ => short.MaxValue
    };

    public short GetMaxHealth(int enemyId) => CurrentFile switch
    {
        GameFile.FileOne => ReadValue<short>(FileOnePtrs.GetEnemyAddress(enemyId) + FileOnePtrs.EnemyMaxHpOffset),
        GameFile.FileTwo => ReadValue<short>(FileTwoPtrs.GetEnemyAddress(enemyId) + FileTwoPtrs.EnemyMaxHpOffset),
        _ => short.MaxValue
    };

    public DecodedEnemy[] DecodedEnemies2 { get; } = new DecodedEnemy[Constants.MaxEnemies2];

    public void UpdateEnemies2(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Console.WriteLine("Decoding enemies2");

        for (var i = 0; i < Constants.MaxEnemies2; i++)
        {
            int curMobOffset = 0x60 * i;

                switch (CurrentFile)
                {
                    case GameFile.FileOne:
                        DecodedEnemies2[i].SlotId = ReadValue<byte>(FileOnePtrs.EnemyListOffset + curMobOffset + 0x1);
                        DecodedEnemies2[i].TypeId = ReadValue<byte>(FileOnePtrs.EnemyListOffset + curMobOffset + 0x2);
                        DecodedEnemies2[i].NameId = ReadValue<short>(FileOnePtrs.EnemyListOffset + curMobOffset + 0x3);
                        DecodedEnemies2[i].CurHp = ReadValue<short>(FileOnePtrs.EnemyListOffset + curMobOffset + 0x1C);
                        DecodedEnemies2[i].MaxHp = ReadValue<short>(FileOnePtrs.EnemyListOffset + curMobOffset + 0x1E);
                        DecodedEnemies2[i].RoomId = ReadValue<byte>(FileOnePtrs.EnemyListOffset + curMobOffset + 0x22);
                        DecodedEnemies2[i].Status = ReadValue<byte>(FileOnePtrs.EnemyListOffset + curMobOffset + 0x45);
                        break;

                    case GameFile.FileTwo:
                        DecodedEnemies2[i].SlotId = ReadValue<byte>(FileTwoPtrs.EnemyListOffset + curMobOffset + 0x1);
                        DecodedEnemies2[i].TypeId = ReadValue<byte>(FileTwoPtrs.EnemyListOffset + curMobOffset + 0x2);
                        DecodedEnemies2[i].NameId = ReadValue<short>(FileTwoPtrs.EnemyListOffset + curMobOffset + 0x3);
                        DecodedEnemies2[i].CurHp = ReadValue<short>(FileTwoPtrs.EnemyListOffset + curMobOffset + 0x1C);
                        DecodedEnemies2[i].MaxHp = ReadValue<short>(FileTwoPtrs.EnemyListOffset + curMobOffset + 0x1E);
                        DecodedEnemies2[i].RoomId = ReadValue<byte>(FileTwoPtrs.EnemyListOffset + curMobOffset + 0x22);
                        DecodedEnemies2[i].Status = ReadValue<byte>(FileTwoPtrs.EnemyListOffset + curMobOffset + 0x45);
                        break;

                    default: throw new ArgumentOutOfRangeException();
                }

                DecodedEnemies2[i].Name = GetEnumString(DecodedEnemies2[i].TypeId, EnemyType.Unknown);

                // TODO: Add logic to get the room name based on current scenario
                // DecodedEnemies2[i].RoomName = ;
        }

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Console.WriteLine($"Decoded enemies2 in {duration}ms");

        foreach (DecodedEnemy? enemy in DecodedEnemies2)
            Console.WriteLine(JsonSerializer.Serialize(enemy, DecodedScenarioJsonContext.Default.DecodedScenario));
    }
}
