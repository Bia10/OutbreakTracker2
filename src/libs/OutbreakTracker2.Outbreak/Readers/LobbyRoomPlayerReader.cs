using System.Text.Json;
using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Serialization;
using OutbreakTracker2.PCSX2;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomPlayerReader : ReaderBase
{
    public DecodedLobbyRoomPlayer[] DecodedLobbyRoomPlayers { get; }

    public LobbyRoomPlayerReader(GameClient gameClient, EEmemMemory eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedLobbyRoomPlayers = new DecodedLobbyRoomPlayer[Constants.MaxPlayers];
        for (int i = 0; i < Constants.MaxPlayers; i++)
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

    public byte GetPlayerCharNPCType(int characterId)
    {
        nint basePlayerAddress = GetLobbyRoomPlayerBasePointer(characterId);
        nint offset = CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.LobbyRoomPlayerNPCTypeOffset,
            GameFile.FileTwo => FileTwoPtrs.LobbyRoomPlayerNPCTypeOffset,
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

    public string GetCharacterNpcTypeString(int characterId)
        => GetEnumString(GetPlayerCharNPCType(characterId), CharacterNpcType.Unknown);

    public string GetCharacterNameString(byte characterBaseType)
        => GetEnumString(characterBaseType, CharacterBaseType.Unknown);

    public string GetCharacterHealthString(byte characterBaseType)
        => GetEnumString(characterBaseType, CharacterHealth.Unknown);

    public string GetCharacterPowerString(byte characterBaseType)
        => GetEnumString(characterBaseType, CharacterPower.Unknown);

    public string GetCharacterNpcNameString(byte characterBaseType)
        => GetEnumString(characterBaseType, CharacterNpcName.Unknown);

    public string GetCharacterNpcHealthString(byte characterBaseType)
        => GetEnumString(characterBaseType, CharacterNpcHealth.Unknown);

    public string GetCharacterNpcPowerString(byte characterBaseType)
        => GetEnumString(characterBaseType, CharacterNpcPower.Unknown);

    public void UpdateRoomPlayers(bool debug = false)
    {
        if (CurrentFile is GameFile.Unknown) return;

        long start = Environment.TickCount64;

        if (debug) Logger.LogDebug("Decoding lobby room players");

        for (var i = 0; i < Constants.MaxPlayers; i++)
        {
            DecodedLobbyRoomPlayers[i].IsEnabled = GetPlayerCharEnabled(i);
            if (!DecodedLobbyRoomPlayers[i].IsEnabled) continue;

            DecodedLobbyRoomPlayers[i].NameId = GetPlayerCharNameId(i);
            DecodedLobbyRoomPlayers[i].NPCType = GetCharacterNpcTypeString(i);

            switch (DecodedLobbyRoomPlayers[i].NPCType)
            {
                case "Main Characters":
                    DecodedLobbyRoomPlayers[i].CharacterName = GetCharacterNameString(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].CharacterHP = GetCharacterHealthString(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].CharacterPower = GetCharacterPowerString(DecodedLobbyRoomPlayers[i].NameId);
                    break;
                case "Other NPCs":
                    DecodedLobbyRoomPlayers[i].NPCName = GetCharacterNpcNameString(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].NPCHP = GetCharacterNpcHealthString(DecodedLobbyRoomPlayers[i].NameId);
                    DecodedLobbyRoomPlayers[i].NPCPower = GetCharacterNpcPowerString(DecodedLobbyRoomPlayers[i].NameId);
                    break;
                case "Unknown":
                    Logger.LogDebug("[{UpdateRoomPlayersName}] NPCType unknown: {NpcType} for character at index {I}.", nameof(UpdateRoomPlayers), DecodedLobbyRoomPlayers[i].NPCType, i);
                    break;
            }
        }

        long duration = Environment.TickCount64 - start;

        if (!debug) return;

        Logger.LogDebug("Decoded room players in {Duration}ms", duration);

        foreach (DecodedLobbyRoomPlayer player in DecodedLobbyRoomPlayers)
            Logger.LogDebug(JsonSerializer.Serialize(player, DecodedLobbyRoomPlayerJsonContext.Default.DecodedLobbyRoomPlayer));
    }
}
