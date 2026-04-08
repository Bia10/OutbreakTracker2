using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Readers;

public interface IInGameScenarioReader : IDisposable
{
    DecodedInGameScenario DecodedScenario { get; }

    bool IsInScenario();

    void UpdateScenario();
}
