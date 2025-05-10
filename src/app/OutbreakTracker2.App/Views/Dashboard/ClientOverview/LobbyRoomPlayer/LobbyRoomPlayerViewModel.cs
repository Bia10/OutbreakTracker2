using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.App.Views.Dashboard.ClientOverview.LobbyRoomPlayer;

public partial class LobbyRoomPlayerViewModel : ObservableObject
{
    private DecodedLobbyRoomPlayer _model;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private byte _nameId;

    [ObservableProperty]
    private string _nPCType = string.Empty;

    [ObservableProperty]
    private string _characterName = string.Empty;

    [ObservableProperty]
    private string _characterHP = string.Empty;

    [ObservableProperty]
    private string _characterPower = string.Empty;

    [ObservableProperty]
    private string _nPCName = string.Empty;

    [ObservableProperty]
    private string _nPCHP = string.Empty;

    [ObservableProperty]
    private string _nPCPower = string.Empty;

    public bool IsMainCharacter => NPCType == "Main Characters";
    public bool IsOtherNPC => NPCType == "Other NPCs";
    public byte UniquePlayerId => NameId;

    public LobbyRoomPlayerViewModel(DecodedLobbyRoomPlayer model)
    {
        _model = model;
        Update(model);
    }

    public void Update(DecodedLobbyRoomPlayer model)
    {
        if (!model.IsEnabled)
            return;

        _model = model;

        IsEnabled = model.IsEnabled;
        NameId = model.NameId;      
        NPCType = model.NPCType;
        CharacterName = model.CharacterName;
        CharacterHP = model.CharacterHP;
        CharacterPower = model.CharacterPower;
        NPCName = model.NPCName; 
        NPCHP = model.NPCHP;
        NPCPower = model.NPCPower;

        DisplayName = NPCType == "Main Characters" ? CharacterName : NPCName;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        return Equals((LobbyRoomPlayerViewModel)obj);
    }

    protected bool Equals(LobbyRoomPlayerViewModel other)
        => UniquePlayerId == other.UniquePlayerId;

    public override int GetHashCode()
        => UniquePlayerId.GetHashCode();
}