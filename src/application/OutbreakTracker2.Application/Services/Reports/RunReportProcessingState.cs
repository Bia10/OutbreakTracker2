using System.Collections.Concurrent;
using OutbreakTracker2.Outbreak.Common;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportProcessingState
{
    private string _lastScenarioId = string.Empty;
    private DecodedInGameScenario? _lastScenario;
    private int _lastScenarioStatus = (int)ScenarioStatus.None;

    public ConcurrentDictionary<Ulid, DecodedInGamePlayer> ActivePlayers { get; } = new();

    /// <summary>
    /// Tracks all enabled players by their slot index, regardless of <c>IsInGame</c> status.
    /// Used to resolve item pickup/drop holder names, which reference slot indices and may point
    /// to NPC-controlled slots that have <c>IsEnabled=true</c> but <c>IsInGame=false</c>.
    /// </summary>
    public DecodedInGamePlayer?[] AllEnabledPlayersBySlot { get; } = new DecodedInGamePlayer?[GameConstants.MaxPlayers];

    public DecodedInGamePlayer?[] ActivePlayersBySlot { get; } = new DecodedInGamePlayer?[GameConstants.MaxPlayers];

    public DecodedItem[]? PreviousItems { get; set; }

    public string LastScenarioId
    {
        get => Volatile.Read(ref _lastScenarioId);
        set => Volatile.Write(ref _lastScenarioId, value);
    }

    public DecodedInGameScenario? LastScenario
    {
        get => Volatile.Read(ref _lastScenario);
        set => Volatile.Write(ref _lastScenario, value);
    }

    public ScenarioStatus LastScenarioStatus
    {
        get => (ScenarioStatus)Volatile.Read(ref _lastScenarioStatus);
        set => Volatile.Write(ref _lastScenarioStatus, (int)value);
    }
}
