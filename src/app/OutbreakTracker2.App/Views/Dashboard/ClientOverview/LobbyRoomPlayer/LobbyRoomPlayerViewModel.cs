using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;
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

    public LobbyRoomPlayerViewModel() { }

    public LobbyRoomPlayerViewModel(DecodedLobbyRoomPlayer model)
    {
        Update(model);
    }

    public void Update(DecodedLobbyRoomPlayer model)
    {
        IsEnabled = model.IsEnabled;
        NameId = model.NameId;
        NpcType = model.NPCType;
        CharacterName = model.CharacterName;
        CharacterHp = model.CharacterHP;
        CharacterPower = model.CharacterPower;
        NpcName = model.NPCName;
        NpcHp = model.NPCHP;
        NpcPower = model.NPCPower;

        DisplayName = NpcType == "Main Characters" ? CharacterName : NpcName;
    }

    public override bool Equals(object? obj)
        => obj is LobbyRoomPlayerViewModel viewModel &&
           Id == viewModel.Id;

    public override int GetHashCode()
        => HashCode.Combine(Id);
}