using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Application.Services.Tracking;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportDoorDiffProcessor : IRunReportCollectionDiffProcessor<DecodedDoor>
{
    public void Process(CollectionDiff<DecodedDoor> diff, RunReportProcessingContext context)
    {
        if (context.State.LastScenarioStatus != ScenarioStatus.InGame)
            return;

        DateTimeOffset now = context.GetCurrentTime();

        foreach (EntityChange<DecodedDoor> change in diff.Changed)
        {
            if (!string.Equals(change.Previous.Status, change.Current.Status, StringComparison.Ordinal))
                context.Emit(
                    new DoorStateChangedEvent(
                        now,
                        change.Current.Id,
                        change.Current.SlotId,
                        change.Previous.Status,
                        change.Current.Status
                    )
                );

            if (change.Previous.Hp != change.Current.Hp)
                context.Emit(
                    new DoorDamagedEvent(
                        now,
                        change.Current.Id,
                        change.Current.SlotId,
                        change.Previous.Hp,
                        change.Current.Hp
                    )
                );

            if (change.Previous.Flag != change.Current.Flag)
                context.Emit(
                    new DoorFlagChangedEvent(
                        now,
                        change.Current.Id,
                        change.Current.SlotId,
                        change.Previous.Flag,
                        change.Current.Flag
                    )
                );
        }
    }
}
