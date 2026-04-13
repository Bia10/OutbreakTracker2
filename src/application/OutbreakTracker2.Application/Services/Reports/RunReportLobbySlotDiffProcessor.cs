using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportLobbySlotDiffProcessor : IRunReportCollectionDiffProcessor<DecodedLobbySlot>
{
    public void Process(CollectionDiff<DecodedLobbySlot> diff, RunReportProcessingContext context)
    {
        foreach (DecodedLobbySlot slot in diff.Added)
            if (!string.IsNullOrEmpty(slot.ScenarioId))
                context.State.LastScenarioId = slot.ScenarioId;

        foreach (EntityChange<DecodedLobbySlot> change in diff.Changed)
            if (!string.IsNullOrEmpty(change.Current.ScenarioId))
                context.State.LastScenarioId = change.Current.ScenarioId;
    }
}
