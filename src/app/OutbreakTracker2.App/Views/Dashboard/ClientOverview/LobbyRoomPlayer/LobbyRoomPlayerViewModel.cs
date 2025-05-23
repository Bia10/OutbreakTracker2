using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.App.Views.Common;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;
using System;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoomPlayer;

public partial class LobbyRoomPlayerViewModel : ObservableObject
{
    [ObservableProperty]
    private Ulid _id = Ulid.NewUlid();

    [ObservableProperty]
    private byte _nameId;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _npcType = string.Empty;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _characterHp = string.Empty;

    [ObservableProperty]
    private string _characterPower = string.Empty;

    [ObservableProperty]
    private string _npcName = string.Empty;

    [ObservableProperty]
    private string _npcHp = string.Empty;

    [ObservableProperty]
    private string _npcPower = string.Empty;

    public bool IsMainCharacter => NpcType == "Main Characters";
    public bool IsOtherNpc => NpcType == "Other NPCs";
    public byte DataPlayerId => NameId;
    public Ulid ViewModelId => Id;

    public CharacterBustViewModel PlayerBustViewModel { get; }

    public LobbyRoomPlayerViewModel(DecodedLobbyRoomPlayer model, CharacterBustViewModel characterBustViewModel)
    {
        PlayerBustViewModel = characterBustViewModel;

        Update(model);
    }

    public void Update(DecodedLobbyRoomPlayer model)
    {
        IsEnabled = model.IsEnabled;
        NameId = model.NameId;
        NpcType = model.NpcType;
        CharacterName = model.CharacterName;
        CharacterHp = model.CharacterHp;
        CharacterPower = model.CharacterPower;
        NpcName = model.NpcName;
        NpcHp = model.Npchp;
        NpcPower = model.NpcPower;

        DisplayName = NpcType == "Main Characters" ? CharacterName : NpcName;

        if (EnumUtility.TryParseByValueOrMember(DisplayName, out CharacterBaseType charType))
            _ = PlayerBustViewModel.UpdateBustAsync(charType);
    }

    public override bool Equals(object? obj)
        => obj is LobbyRoomPlayerViewModel viewModel &&
           Id == viewModel.Id;

    public override int GetHashCode()
        => HashCode.Combine(Id);
}