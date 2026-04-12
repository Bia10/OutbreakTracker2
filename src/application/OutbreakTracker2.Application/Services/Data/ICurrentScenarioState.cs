using OutbreakTracker2.Outbreak.Enums;

namespace OutbreakTracker2.Application.Services.Data;

/// <summary>
/// Narrow pull-based contract exposing only the scenario fields required by entity alert rules.
/// Inject this instead of <see cref="IDataSnapshot"/> when a rule only needs scenario name and status.
/// </summary>
public interface ICurrentScenarioState
{
    string ScenarioName { get; }
    ScenarioStatus Status { get; }
}
