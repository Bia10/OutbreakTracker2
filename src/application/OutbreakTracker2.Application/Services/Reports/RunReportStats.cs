using OutbreakTracker2.Application.Services.Reports.Events;

namespace OutbreakTracker2.Application.Services.Reports;

public sealed record RunReportStats(
    int TotalEnemyKills,
    int TotalDespawns,
    int TotalDamageTaken,
    double PeakVirusPercentage,
    int TotalDoorStateChanges,
    int TotalItemPickups,
    int TotalItemDrops,
    TimeSpan Duration,
    IReadOnlyDictionary<string, int> EnemyDamageContributedByPlayer,
    IReadOnlyDictionary<string, int> KillsContributedByPlayer
)
{
    public static RunReportStats From(RunReport report)
    {
        int kills = 0;
        int despawns = 0;
        int damageTaken = 0;
        double peakVirus = 0.0;
        int doorChanges = 0;
        int itemPickups = 0;
        int itemDrops = 0;
        Dictionary<string, int> damageByPlayer = [];
        Dictionary<string, int> killsByPlayer = [];

        foreach (RunEvent evt in report.Events)
        {
            switch (evt)
            {
                case EnemyKilledEvent kill:
                    kills++;
                    AccumulateWeightedKill(kill.ContributingPlayers, killsByPlayer);
                    break;
                case EnemyDamagedEvent dmg:
                    AccumulateWeightedDamage(dmg.ContributingPlayers, dmg.Damage, damageByPlayer);
                    break;
                case EnemyDespawnedEvent:
                    despawns++;
                    break;
                case PlayerHealthChangedEvent { IsDamage: true } hp:
                    damageTaken += hp.OldHealth - hp.NewHealth;
                    break;
                case PlayerVirusChangedEvent virus when virus.NewVirusPercentage > peakVirus:
                    peakVirus = virus.NewVirusPercentage;
                    break;
                case DoorStateChangedEvent:
                    doorChanges++;
                    break;
                case ItemPickedUpEvent:
                    itemPickups++;
                    break;
                case ItemDroppedEvent:
                    itemDrops++;
                    break;
            }
        }

        return new RunReportStats(
            kills,
            despawns,
            damageTaken,
            peakVirus,
            doorChanges,
            itemPickups,
            itemDrops,
            report.Duration,
            damageByPlayer,
            killsByPlayer
        );
    }

    // Distributes damage among contributors weighted by their Power stat.
    // If all Power values are zero (e.g. unknown) falls back to equal split.
    private static void AccumulateWeightedDamage(
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players,
        ushort totalDamage,
        Dictionary<string, int> accumulator
    )
    {
        if (players.Count == 0)
            return;

        float totalPower = 0f;
        foreach ((_, _, float p) in players)
            totalPower += p;

        foreach ((_, string name, float power) in players)
        {
            float share = totalPower > 0f ? power / totalPower : 1f / players.Count;
            int credited = (int)MathF.Round(totalDamage * share);
            accumulator[name] = accumulator.GetValueOrDefault(name) + credited;
        }
    }

    // A kill is credited to all contributors (each player in the room gets +1 kill share).
    // This mirrors most games' "assist = kill" convention since we cannot determine the finisher.
    private static void AccumulateWeightedKill(
        IReadOnlyList<(Ulid PlayerId, string PlayerName, float Power)> players,
        Dictionary<string, int> accumulator
    )
    {
        foreach ((_, string name, _) in players)
            accumulator[name] = accumulator.GetValueOrDefault(name) + 1;
    }
}
