using OutbreakTracker2.Outbreak.Models;

namespace OutbreakTracker2.Outbreak.Readers;

public interface IEnemiesReader : IDisposable
{
    DecodedEnemy[] DecodedEnemies2 { get; }

    void UpdateEnemies2();
}
