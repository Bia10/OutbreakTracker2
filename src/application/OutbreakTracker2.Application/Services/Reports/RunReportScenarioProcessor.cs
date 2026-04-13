using OutbreakTracker2.Application.Services.Reports.Events;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Application.Services.Reports;

internal sealed class RunReportScenarioProcessor : IRunReportScenarioProcessor
{
    private static readonly IReadOnlySet<ScenarioStatus> MonitoredStatuses = new HashSet<ScenarioStatus>
    {
        ScenarioStatus.Unknown8,
        ScenarioStatus.Unknown9,
        ScenarioStatus.Unknown10,
        ScenarioStatus.Unknown11,
    };

    public void Process(DecodedInGameScenario scenario, RunReportProcessingContext context)
    {
        RunReportProcessingState state = context.State;
        state.LastScenario = scenario;

        ScenarioStatus previousStatus = state.LastScenarioStatus;
        state.LastScenarioStatus = scenario.Status;

        if (previousStatus != state.LastScenarioStatus)
        {
            if (MonitoredStatuses.Contains(state.LastScenarioStatus))
                context.Emit(
                    new ScenarioStatusChangedEvent(context.GetCurrentTime(), previousStatus, state.LastScenarioStatus)
                );

            if (state.LastScenarioStatus == ScenarioStatus.InGame && state.ActivePlayers.Count > 0)
                context.AutoStartSession();

            if (state.LastScenarioStatus is ScenarioStatus.GameFinished or ScenarioStatus.RankScreen)
            {
                DateTimeOffset finishTime = context.GetCurrentTime();
                foreach (DecodedInGamePlayer player in state.ActivePlayers.Values)
                    context.Emit(
                        new PlayerLeftEvent(
                            finishTime,
                            player.Id,
                            player.Name,
                            player.CurHealth,
                            player.VirusPercentage
                        )
                    );

                state.ActivePlayers.Clear();
                context.AutoStopSession();
            }

            if (state.LastScenarioStatus == ScenarioStatus.None && context.IsSessionRunning())
            {
                state.ActivePlayers.Clear();
                context.AutoStopSession();
            }
        }

        DecodedItem[] currentItems = scenario.Items;
        DecodedItem[]? previousItems = state.PreviousItems;
        state.PreviousItems = currentItems;

        if (previousItems is null || state.LastScenarioStatus != ScenarioStatus.InGame)
            return;

        DateTimeOffset now = context.GetCurrentTime();
        int count = Math.Min(previousItems.Length, currentItems.Length);

        for (int i = 0; i < count; i++)
        {
            DecodedItem previousItem = previousItems[i];
            DecodedItem currentItem = currentItems[i];

            if (string.IsNullOrEmpty(previousItem.TypeName) || string.IsNullOrEmpty(currentItem.TypeName))
                continue;

            if (previousItem.PickedUp == 0 && currentItem.PickedUp > 0)
            {
                string holderName = context.ResolvePickupHolderName(currentItem.PickedUp);
                context.Emit(
                    new ItemPickedUpEvent(
                        now,
                        currentItem.TypeName,
                        currentItem.SlotIndex,
                        currentItem.RoomId,
                        holderName
                    )
                );
            }
            else if (previousItem.PickedUp > 0 && currentItem.PickedUp == 0)
            {
                string previousHolderName = context.ResolvePickupHolderName(previousItem.PickedUp);
                context.Emit(
                    new ItemDroppedEvent(
                        now,
                        currentItem.TypeName,
                        currentItem.SlotIndex,
                        currentItem.RoomId,
                        previousHolderName
                    )
                );
            }

            if (previousItem.Quantity != currentItem.Quantity)
                context.Emit(
                    new ItemQuantityChangedEvent(
                        now,
                        currentItem.TypeName,
                        currentItem.SlotIndex,
                        currentItem.RoomId,
                        previousItem.Quantity,
                        currentItem.Quantity
                    )
                );
        }
    }
}
