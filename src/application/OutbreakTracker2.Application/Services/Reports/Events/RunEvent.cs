using System.Text;
using OutbreakTracker2.Outbreak.Enums;
using OutbreakTracker2.Outbreak.Utility;

namespace OutbreakTracker2.Application.Services.Reports.Events;

public abstract record RunEvent(DateTimeOffset OccurredAt)
{
    public int ScenarioFrame { get; init; }

    public abstract string Describe(Scenario scenario);

    protected static string Invariant(FormattableString value) => FormattableString.Invariant(value);

    protected static string RoomName(Scenario scenario, int roomId) => scenario.GetRoomName(roomId);

    protected static string FormatContributions(IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players)
    {
        if (players.Count == 0)
            return string.Empty;

        StringBuilder builder = new(" — by ");
        for (int index = 0; index < players.Count; index++)
        {
            if (index > 0)
                builder.Append(", ");

            builder.Append("**").Append(players[index].PlayerName).Append("**");
        }

        return builder.ToString();
    }
}
