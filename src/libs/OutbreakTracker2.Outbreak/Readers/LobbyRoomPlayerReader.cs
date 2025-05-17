using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2;
using System.Text.Json;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomPlayerReader : ReaderBase
{
    public DecodedLobbyRoomPlayer[] DecodedLobbyRoomPlayers { get; }

    public LobbyRoomPlayerReader(GameClient gameClient, IEEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedLobbyRoomPlayers = new DecodedLobbyRoomPlayer[GameConstants.MaxPlayers];
        for (int i = 0; i < GameConstants.MaxPlayers; i++)
            DecodedLobbyRoomPlayers[i] = new DecodedLobbyRoomPlayer();
    }

    public bool GetPlayerCharEnabled(int characterId)
    {
        nint basePlayerAddress = GetLobbyRoomPlayerBasePointer(characterId);
        nint offset = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.LobbyRoomPlayerEnabledOffset,
            GameFile.FileTwo => FileTwoPtrs.LobbyRoomPlayerEnabledOffset,
            _ => nint.Zero
        };

        return ReadValue<bool>(basePlayerAddress, [offset]);
    }

    public byte GetPlayerCharNpcType(int characterId)
    {
        nint basePlayerAddress = GetLobbyRoomPlayerBasePointer(characterId);
        nint offset = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.LobbyRoomPlayerNpcTypeOffset,
            GameFile.FileTwo => FileTwoPtrs.LobbyRoomPlayerNpcTypeOffset,
            _ => nint.Zero
        };

        return ReadValue<byte>(basePlayerAddress, [offset]);
    }

    public byte GetPlayerCharNameId(int characterId)
    {
        nint basePlayerAddress = GetLobbyRoomPlayerBasePointer(characterId);
        nint offset = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.LobbyRoomPlayerNameIdOffset,
            GameFile.FileTwo => FileTwoPtrs.LobbyRoomPlayerNameIdOffset,
            _ => nint.Zero
        };

        return ReadValue<byte>(basePlayerAddress, [offset]);
    }

    public static string GetCharacterNpcTypeName(byte characterType)
        => EnumUtility.GetEnumString(characterType, CharacterNpcType.Unknown);

    public static string GetCharacterName(byte characterBaseType)
        => EnumUtility.GetEnumString(characterBaseType, CharacterBaseType.Unknown);

    public static string GetCharacterHealthName(byte characterBaseType)
        => EnumUtility.GetEnumString(characterBaseType, CharacterHealth.Unknown);

    public static string GetCharacterPowerName(byte characterBaseType)
        => EnumUtility.GetEnumString(characterBaseType, CharacterPower.Unknown);

    public static string GetCharacterNpcName(byte characterBaseType)
        => EnumUtility.GetEnumString(characterBaseType, CharacterNpcName.Unknown);

    public static string GetCharacterNpcHealthName(byte characterBaseType)
        => EnumUtility.GetEnumString(characterBaseType, CharacterNpcHealth.Unknown);

    public static string GetCharacterNpcPowerName(byte characterBaseType)
        => EnumUtility.GetEnumString(characterBaseType, CharacterNpcPower.Unknown);

    public void UpdateRoomPlayers(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding lobby room players");

        for (int i = 0; i < GameConstants.MaxPlayers; i++)
        {
            DecodedLobbyRoomPlayers[i].IsEnabled = GetPlayerCharEnabled(i);
            if (!DecodedLobbyRoomPlayers[i].IsEnabled) continue;

            DecodedLobbyRoomPlayers[i].NameId = GetPlayerCharNameId(i);
            DecodedLobbyRoomPlayers[i].NpcType = GetCharacterNpcTypeName(GetPlayerCharNpcType(i));

            switch (DecodedLobbyRoomPlayers[i].NpcType)
            {
                case "Main Characters":
                    DecodedLobbyRoomPlayers[i].CharacterName = GetCharacterName(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].CharacterHp = GetCharacterHealthName(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].CharacterPower = GetCharacterPowerName(DecodedLobbyRoomPlayers[i].NameId);
                    break;
                case "Other NPCs":
                    DecodedLobbyRoomPlayers[i].NpcName = GetCharacterNpcName(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].Npchp = GetCharacterNpcHealthName(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].NpcPower = GetCharacterNpcPowerName(DecodedLobbyRoomPlayers[i].NameId);
                    break;
                case "Unknown":
                    Logger.LogDebug("[{UpdateRoomPlayersName}] NPCType unknown: {NpcType} for character at index {I}", nameof(UpdateRoomPlayers), DecodedLobbyRoomPlayers[i].NpcType, i);
                    break;
            }
        }

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded room players in {Duration}ms", duration);
        foreach (string jsonObject in DecodedLobbyRoomPlayers.Select(inGamePlayer
                     => JsonSerializer.Serialize(inGamePlayer, DecodedLobbyRoomPlayerJsonContext.Default.DecodedLobbyRoomPlayer)))
            Logger.LogDebug("Decoded room player: {JsonObject}", jsonObject);
    }
}
