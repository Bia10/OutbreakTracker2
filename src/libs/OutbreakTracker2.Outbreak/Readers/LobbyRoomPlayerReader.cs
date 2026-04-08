using Microsoft.Extensions.Logging;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Offsets;
using OutbreakTracker2.Outbreak.Utility;
using OutbreakTracker2.PCSX2.Client;
using OutbreakTracker2.PCSX2.EEmem;

namespace OutbreakTracker2.Outbreak.Readers;

public sealed class LobbyRoomPlayerReader : ReaderBase, ILobbyRoomPlayerReader
{
    public DecodedLobbyRoomPlayer[] DecodedLobbyRoomPlayers { get; private set; }

    public LobbyRoomPlayerReader(IGameClient gameClient, IEEmemAddressReader eememMemory, ILogger logger)
        : base(gameClient, eememMemory, logger)
    {
        DecodedLobbyRoomPlayers = new DecodedLobbyRoomPlayer[GameConstants.MaxPlayers];
        for (int i = 0; i < GameConstants.MaxPlayers; i++)
            DecodedLobbyRoomPlayers[i] = new DecodedLobbyRoomPlayer();
    }

    private nint GetLobbyRoomPlayerBaseAddress(int characterId)
    {
        return CurrentFile switch
        {
            GameFile.FileOne => FileOnePtrs.GetLobbyRoomPlayerAddress(characterId),
            GameFile.FileTwo => FileTwoPtrs.GetLobbyRoomPlayerAddress(characterId),
            _ => nint.Zero,
        };
    }

    private bool GetPlayerCharEnabled(nint basePlayerAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(LobbyRoomPlayerOffsets.PlayerEnabled);
        return ReadValue<bool>(basePlayerAddress, offsets);
    }

    private byte GetPlayerCharNpcType(nint basePlayerAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(LobbyRoomPlayerOffsets.PlayerNpcType);
        return ReadValue<byte>(basePlayerAddress, offsets);
    }

    private byte GetPlayerCharNameId(nint basePlayerAddress)
    {
        ReadOnlySpan<nint> offsets = GetFileSpecificOffsets(LobbyRoomPlayerOffsets.PlayerNameId);
        return ReadValue<byte>(basePlayerAddress, offsets);
    }

    private static string GetCharacterNpcTypeName(byte characterType) =>
        EnumUtility.GetEnumString(characterType, CharacterNpcType.Unknown);

    private static string GetCharacterName(byte characterBaseType) =>
        EnumUtility.GetEnumString(characterBaseType, CharacterBaseType.Unknown);

    private static string GetCharacterHealthName(byte characterBaseType) =>
        EnumUtility.GetEnumString(characterBaseType, CharacterHealth.Unknown);

    private static string GetCharacterPowerName(byte characterBaseType) =>
        EnumUtility.GetEnumString(characterBaseType, CharacterPower.Unknown);

    private static string GetCharacterNpcName(byte characterBaseType) =>
        EnumUtility.GetEnumString(characterBaseType, CharacterNpcName.Unknown);

    private static string GetCharacterNpcHealthName(byte characterBaseType) =>
        EnumUtility.GetEnumString(characterBaseType, CharacterNpcHealth.Unknown);

    private static string GetCharacterNpcPowerName(byte characterBaseType) =>
        EnumUtility.GetEnumString(characterBaseType, CharacterNpcPower.Unknown);

    public void UpdateRoomPlayers()
    {
        if (CurrentFile is GameFile.Unknown)
            return;

        DecodedLobbyRoomPlayer[] newDecodedLobbyRoomPlayers = new DecodedLobbyRoomPlayer[GameConstants.MaxPlayers];

        for (int i = 0; i < GameConstants.MaxPlayers; i++)
        {
            nint basePlayerAddress = GetLobbyRoomPlayerBaseAddress(i);
            if (basePlayerAddress == nint.Zero || !GetPlayerCharEnabled(basePlayerAddress))
            {
                newDecodedLobbyRoomPlayers[i] = new DecodedLobbyRoomPlayer();
                continue;
            }

            byte nameId = GetPlayerCharNameId(basePlayerAddress);
            byte playerCharNpcTypeId = GetPlayerCharNpcType(basePlayerAddress);
            string npcType = GetCharacterNpcTypeName(playerCharNpcTypeId);

            string characterName = string.Empty;
            string characterHp = string.Empty;
            string characterPower = string.Empty;
            string npcName = string.Empty;
            string npcHp = string.Empty;
            string npcPower = string.Empty;

            switch ((CharacterNpcType)playerCharNpcTypeId)
            {
                case CharacterNpcType.MainCharacters:
                    characterName = GetCharacterName(nameId);
                    characterHp = GetCharacterHealthName(nameId);
                    characterPower = GetCharacterPowerName(nameId);
                    break;
                case CharacterNpcType.OtherNpCs:
                    npcName = GetCharacterNpcName(nameId);
                    npcHp = GetCharacterNpcHealthName(nameId);
                    npcPower = GetCharacterNpcPowerName(nameId);
                    break;
                default:
                    Logger.LogDebug(
                        "[{UpdateRoomPlayersName}] NPCType unknown: {NpcType} for character at index {I}",
                        nameof(UpdateRoomPlayers),
                        npcType,
                        i
                    );
                    break;
            }

            newDecodedLobbyRoomPlayers[i] = new DecodedLobbyRoomPlayer
            {
                IsEnabled = true,
                NameId = nameId,
                NpcType = npcType,
                CharacterName = characterName,
                CharacterHp = characterHp,
                CharacterPower = characterPower,
                NpcName = npcName,
                NpcHp = npcHp,
                NpcPower = npcPower,
            };
        }

        DecodedLobbyRoomPlayers = newDecodedLobbyRoomPlayers;
    }
}
