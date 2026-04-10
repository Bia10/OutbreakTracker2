using CommunityToolkit.Mvvm.ComponentModel;
using OutbreakTracker2.Application.Views.Common.Character;
using OutbreakTracker2.Outbreak.Enums.Character;
using OutbreakTracker2.Outbreak.Models;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Views.Dashboard.ClientOverview.LobbyRoomPlayer;

public sealed partial class LobbyRoomPlayerViewModel : ObservableObject
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
    [NotifyPropertyChangedFor(nameof(IsEnemyFactionNpc))]
    private string _npcName = string.Empty;

    [ObservableProperty]
    private string _npcHp = string.Empty;

    [ObservableProperty]
    private string _npcPower = string.Empty;

    [ObservableProperty]
    private bool _isRoomMaster;

    private static readonly HashSet<string> EnemyFactionNpcs = ["Mr. Red", "Bob", "Tony"];

    public bool IsMainCharacter => string.Equals(NpcType, "Main Characters", StringComparison.Ordinal);
    public bool IsOtherNpc => string.Equals(NpcType, "Other NPCs", StringComparison.Ordinal);
    public bool IsEnemyFactionNpc => !string.IsNullOrEmpty(NpcName) && EnemyFactionNpcs.Contains(NpcName);
    public byte DataPlayerId => NameId;
    public Ulid ViewModelId => Id;

    public CharacterBustViewModel PlayerBustViewModel { get; }

    public LobbyRoomPlayerViewModel(DecodedLobbyRoomPlayer model, CharacterBustViewModel characterBustViewModel)
    {
        PlayerBustViewModel = characterBustViewModel;

        Update(model, 255);
    }

    public void Update(DecodedLobbyRoomPlayer model, byte roomMasterId)
    {
        IsEnabled = model.IsEnabled;
        NameId = model.NameId;
        NpcType = model.NpcType;
        CharacterName = model.CharacterName;
        CharacterHp = model.CharacterHp;
        CharacterPower = model.CharacterPower;
        NpcName = model.NpcName;
        NpcHp = model.NpcHp;
        NpcPower = model.NpcPower;

        DisplayName = string.Equals(NpcType, "Main Characters", StringComparison.Ordinal) ? CharacterName : NpcName;
        IsRoomMaster = roomMasterId <= 3 && model.SlotIndex == roomMasterId;

        if (EnumUtility.TryParseByValueOrMember(DisplayName, out CharacterBaseType charType))
            _ = PlayerBustViewModel.UpdateBustAsync(charType);
    }

    public override bool Equals(object? obj) => obj is LobbyRoomPlayerViewModel viewModel && Id == viewModel.Id;

    public override int GetHashCode() => HashCode.Combine(Id);
}
